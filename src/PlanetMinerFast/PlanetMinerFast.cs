using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using UnityEngine;
namespace PlanetMinerFast
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class PlanetMinerFastPlugin : BaseUnityPlugin
    {
        public const long ENERGY_COST = 20000000L; 
        private static Harmony _harmony;
        private static PlanetVeinCache[] _veinCaches = new PlanetVeinCache[1024];
        private static bool _weaverChecked = false;
        private static FastInvokeHandler _weaverGetPlanetHandler;
        private static FastInvokeHandler _weaverGetStatusHandler;
        private static object _weaverRunningEnum;
        private void Awake()
        {
            Log.Init(Logger);
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            TickManager.Patch(_harmony);
            TickManager.OnSlowTick += OnSlowTick;
            _harmony.PatchAll(typeof(PlanetMinerFastPlugin));
            Log.Info($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} Loaded - Optimized Mining Active");
        }
        private void OnDestroy()
        {
            TickManager.OnSlowTick -= OnSlowTick;
            _harmony?.UnpatchSelf();
        }
        private class PlanetVeinCache
        {
            public Dictionary<int, List<int>> ItemToVeinIndices = new Dictionary<int, List<int>>();
            public Dictionary<int, float> MinedFractions = new Dictionary<int, float>();
            public List<int> ActiveOres = new List<int>();
            public int[] PendingProductRegister = new int[12000];
            public long LastScanTick = -1;
            public double CostFrac = 0; 
            public bool IsDirty = true;
        }
        private static PlanetVeinCache GetCache(PlanetFactory factory)
        {
            if (factory == null || factory.index >= _veinCaches.Length) return new PlanetVeinCache();
            var cache = _veinCaches[factory.index];
            if (cache == null)
            {
                cache = new PlanetVeinCache();
                _veinCaches[factory.index] = cache;
            }
            if (cache.IsDirty || GameMain.gameTick - cache.LastScanTick > 3600)
            {
                cache.ItemToVeinIndices.Clear();
                var veinPool = factory.veinPool;
                int count = 0;
                for (int i = 1; i < factory.veinCursor; i++)
                {
                    if (veinPool[i].id == i && veinPool[i].amount > 0 && veinPool[i].productId > 0)
                    {
                        int itemId = veinPool[i].productId;
                        if (!cache.ItemToVeinIndices.TryGetValue(itemId, out var list))
                        {
                            list = new List<int>();
                            cache.ItemToVeinIndices[itemId] = list;
                        }
                        list.Add(i);
                        count++;
                    }
                }
                cache.ActiveOres = cache.ItemToVeinIndices.Keys.ToList();
                cache.IsDirty = false;
                cache.LastScanTick = GameMain.gameTick;
                Log.Debug($"Rebuilt vein cache for planet {factory.planetId}: found {count} active veins.");
            }
            return cache;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemoveVeinWithComponents))]
        private static void PlanetFactory_RemoveVein_Postfix(PlanetFactory __instance)
        {
            if (__instance != null && __instance.index < _veinCaches.Length && _veinCaches[__instance.index] != null)
            {
                _veinCaches[__instance.index].IsDirty = true; 
            }
        }
        private void OnSlowTick()
        {
            if (GameMain.data == null || GameMain.data.factories == null) return;
            foreach (var factory in GameMain.data.factories)
            {
                if (factory == null) continue;
                if (IsWeaverOptimizing(factory.planet)) continue;
                ProcessFactory(factory);
            }
        }
        private void ProcessFactory(PlanetFactory factory)
        {
            var history = GameMain.history;
            float miningSpeedScale = history.miningSpeedScale;
            if (miningSpeedScale <= 0.0001f) return;
            var cache = GetCache(factory);
            var transport = factory.transport;
            var factoryProductionStat = GameMain.statistics.production.factoryStatPool[factory.index];
            int[] productRegister = factoryProductionStat?.productRegister;
            float miningCostRate = history.miningCostRate;
            var veinPool = factory.veinPool;
            foreach (var sc in transport.stationPool)
            {
                if (sc == null || sc.id == 0 || sc.storage == null) continue;
                if (sc.energy < sc.energyMax * 0.2f && !sc.isCollector && !sc.isVeinCollector)
                {
                    GenerateEnergyFromAnySlot(sc);
                }
                for (int i = 0; i < sc.storage.Length; i++)
                {
                    if ((int)sc.storage[i].localLogic != 2) continue;
                    int itemId = sc.storage[i].itemId;
                    if (itemId <= 0 || sc.storage[i].count >= sc.storage[i].max) continue;
                    long currentEnergyCost = (sc.isCollector || sc.isVeinCollector) ? ENERGY_COST / 10 : ENERGY_COST;
                    if (sc.energy < currentEnergyCost) continue;
                    float minedAmount = 0;
                    float timeFactor = 1.0f; 
                    if (itemId == factory.planet.waterItemId)
                    {
                        minedAmount = 100f * miningSpeedScale * timeFactor;
                    }
                    else if (cache.ItemToVeinIndices.TryGetValue(itemId, out var indices))
                    {
                        bool isOil = LDB.veins.GetVeinTypeByItemId(itemId) == EVeinType.Oil;
                        float amountPerVein = 1f * miningSpeedScale * timeFactor;
                        foreach (int veinIdx in indices)
                        {
                            if (veinPool[veinIdx].id == 0 || veinPool[veinIdx].amount <= 0) continue;
                            if (isOil) 
                            {
                                minedAmount += (veinPool[veinIdx].amount / 6000f) * miningSpeedScale * timeFactor;
                            }
                            else if (TryMineVein(veinPool, veinIdx, miningCostRate, amountPerVein, factory, cache)) 
                            {
                                minedAmount += amountPerVein;
                            }
                        }
                    }
                    if (minedAmount > 0.001f)
                    {
                        cache.MinedFractions.TryGetValue(itemId, out float fraction);
                        fraction += minedAmount;
                        int finalAdded = (int)fraction;
                        if (finalAdded > 0)
                        {
                            fraction -= finalAdded;
                            sc.storage[i].count += finalAdded;
                            if (productRegister != null)
                            {
                                EVeinType veinType = LDB.veins.GetVeinTypeByItemId(itemId);
                                factory.AddMiningFlagUnsafe(veinType);
                                factory.AddVeinMiningFlagUnsafe(veinType);
                                if (itemId < cache.PendingProductRegister.Length)
                                {
                                    System.Threading.Interlocked.Add(ref cache.PendingProductRegister[itemId], finalAdded);
                                }
                            }
                            sc.energy -= currentEnergyCost;
                        }
                        cache.MinedFractions[itemId] = fraction;
                    }
                }
            }
        }
        private static bool TryMineVein(VeinData[] veinPool, int index, float miningRate, float minedAmount, PlanetFactory factory, PlanetVeinCache cache)
        {
            if (veinPool[index].id == 0 || veinPool[index].amount <= 0) return false;
            if (miningRate > 0.00001f)
            {
                cache.CostFrac += miningRate * minedAmount;
                int amountToConsume = (int)cache.CostFrac;
                if (amountToConsume > 0)
                {
                    cache.CostFrac -= amountToConsume;
                    if (amountToConsume > veinPool[index].amount) amountToConsume = veinPool[index].amount;
                    veinPool[index].amount -= amountToConsume;
                    factory.veinGroups[veinPool[index].groupIndex].amount -= amountToConsume;
                    factory.veinAnimPool[index].time = veinPool[index].amount >= 20000 ? 0.0f : (float)(1.0 - (double)veinPool[index].amount * 4.9999998736893758E-05);
                    if (veinPool[index].amount <= 0) 
                    {
                        int type = (int)veinPool[index].type;
                        int groupIndex = (int)veinPool[index].groupIndex;
                        Vector3 pos = veinPool[index].pos;
                        factory.RemoveVeinWithComponents(index);
                        factory.RecalculateVeinGroup(groupIndex);
                        factory.NotifyVeinExhausted(type, groupIndex, pos);
                    }
                }
            }
            return true;
        }
        private static void GenerateEnergyFromAnySlot(StationComponent sc)
        {
            for (int i = 0; i < sc.storage.Length; i++)
            {
                var store = sc.storage[i];
                if (store.itemId <= 0 || store.count <= 0) continue;
                var proto = LDB.items.Select(store.itemId);
                if (proto == null || proto.HeatValue <= 0) continue;
                int toConsume = Math.Min(10, (int)((sc.energyMax - sc.energy) / proto.HeatValue));
                if (toConsume <= 0) toConsume = 1;
                if (toConsume > store.count) toConsume = store.count;
                float incBonus = 1.0f + (float)store.inc / store.count * 0.1f; 
                sc.energy += (long)(toConsume * proto.HeatValue * incBonus);
                sc.storage[i].count -= toConsume;
                if (sc.storage[i].count <= 0) { sc.storage[i].itemId = 0; sc.storage[i].inc = 0; }
                else sc.storage[i].inc = (int)(store.inc * (float)(store.count - toConsume) / store.count);
                if (sc.energy >= sc.energyMax * 0.9f) break;
            }
        }
        private static bool IsWeaverOptimizing(PlanetData planet)
        {
            if (planet == null) return false;
            if (!_weaverChecked)
            {
                try {
                    var weaverType = Type.GetType("Weaver.Optimizations.IOptimizedPlanet, DSP_Weaver");
                    if (weaverType != null) {
                        var getMethod = Type.GetType("Weaver.Optimizations.OptimizedStarCluster, DSP_Weaver")?.GetMethod("GetOptimizedPlanet", BindingFlags.Static | BindingFlags.Public);
                        if (getMethod != null) _weaverGetPlanetHandler = MethodInvoker.GetHandler(getMethod);
                        var statusProp = weaverType.GetProperty("Status");
                        if (statusProp != null) _weaverGetStatusHandler = MethodInvoker.GetHandler(statusProp.GetGetMethod());
                        var enumType = Type.GetType("Weaver.Optimizations.OptimizedPlanetStatus, DSP_Weaver");
                        if (enumType != null) _weaverRunningEnum = Enum.Parse(enumType, "Running");
                    }
                } catch { }
                _weaverChecked = true;
            }
            if (_weaverGetPlanetHandler == null || _weaverGetStatusHandler == null) return false;
            try {
                var optPlanet = _weaverGetPlanetHandler(null, planet);
                if (optPlanet != null) return _weaverGetStatusHandler(optPlanet).Equals(_weaverRunningEnum);
            } catch { }
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactoryProductionStat), nameof(FactoryProductionStat.PrepareTick))]
        private static void FactoryProductionStat_PrepareTick_Postfix(FactoryProductionStat __instance)
        {
            if (__instance == null || __instance.productRegister == null || GameMain.statistics?.production?.factoryStatPool == null) return;
            var statPool = GameMain.statistics.production.factoryStatPool;
            for (int factoryIndex = 0; factoryIndex < statPool.Length; factoryIndex++)
            {
                if (statPool[factoryIndex] == __instance)
                {
                    if (factoryIndex >= _veinCaches.Length) break;
                    var cache = _veinCaches[factoryIndex];
                    if (cache == null) break;
                    bool hasPending = false;
                    for (int o = 0; o < cache.ActiveOres.Count; o++)
                    {
                        int i = cache.ActiveOres[o];
                        if (i < cache.PendingProductRegister.Length && cache.PendingProductRegister[i] > 0)
                        {
                            hasPending = true;
                            break;
                        }
                    }
                    if (!hasPending) continue;
                    for (int o = 0; o < cache.ActiveOres.Count; o++)
                    {
                        int i = cache.ActiveOres[o];
                        if (i >= cache.PendingProductRegister.Length) continue;
                        int pending = System.Threading.Interlocked.Exchange(ref cache.PendingProductRegister[i], 0);
                        if (pending > 0 && i < __instance.productRegister.Length)
                        {
                            __instance.productRegister[i] += pending;
                        }
                    }
                    break;
                }
            }
        }
    }
}
