using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;

namespace PlanetMinerFast
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.Valoneu.Shared", BepInDependency.DependencyFlags.HardDependency)]
    public class PlanetMinerFastPlugin : BaseUnityPlugin
    {
        public const long ENERGY_COST = 20000000L; // 20 MJ

        private static Harmony _harmony;
        
        // Cache to store veins per planet to avoid scanning every frame
        private static readonly ConditionalWeakTable<PlanetFactory, PlanetVeinCache> _veinCaches = new ConditionalWeakTable<PlanetFactory, PlanetVeinCache>();
        
        // Weaver cache
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
            public long LastScanTick = -1;
            public double CostFrac = 0; 
            public bool IsDirty = true;
        }

        private static PlanetVeinCache GetCache(PlanetFactory factory)
        {
            if (!_veinCaches.TryGetValue(factory, out var cache))
            {
                cache = new PlanetVeinCache();
                _veinCaches.Add(factory, cache);
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
            if (_veinCaches.TryGetValue(__instance, out var cache))
            {
                cache.IsDirty = true; 
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

                if (sc.energy < sc.energyMax * 0.2f)
                {
                    GenerateEnergyFromAnySlot(sc);
                }

                for (int i = 0; i < sc.storage.Length; i++)
                {
                    if ((int)sc.storage[i].localLogic != 2) continue;
                    
                    int itemId = sc.storage[i].itemId;
                    if (itemId <= 0 || sc.storage[i].count >= sc.storage[i].max) continue;

                    float minedAmount = 0;
                    float timeFactor = 1.0f; // OnSlowTick is every 1s

                    if (itemId == factory.planet.waterItemId)
                    {
                        if (sc.energy >= ENERGY_COST) minedAmount = 100f * miningSpeedScale * timeFactor;
                    }
                    else if (cache.ItemToVeinIndices.TryGetValue(itemId, out var indices))
                    {
                        if (sc.energy >= ENERGY_COST)
                        {
                            bool isOil = LDB.veins.GetVeinTypeByItemId(itemId) == EVeinType.Oil;
                            foreach (int veinIdx in indices)
                            {
                                if (veinPool[veinIdx].id == 0 || veinPool[veinIdx].amount <= 0) continue;
                                if (isOil) minedAmount += (veinPool[veinIdx].amount / 6000f) * miningSpeedScale * timeFactor;
                                else if (TryMineVein(veinPool, veinIdx, miningCostRate, factory, cache)) minedAmount += 1f * miningSpeedScale * timeFactor;
                            }
                        }
                    }

                    if (minedAmount > 0.001f)
                    {
                        int finalAdded = (int)minedAmount;
                        if (finalAdded > 0)
                        {
                            sc.storage[i].count += finalAdded;
                            if (productRegister != null) productRegister[itemId] += finalAdded;
                            sc.energy -= ENERGY_COST;
                        }
                    }
                }
            }
        }

        private static bool TryMineVein(VeinData[] veinPool, int index, float miningRate, PlanetFactory factory, PlanetVeinCache cache)
        {
            if (veinPool[index].id == 0 || veinPool[index].amount <= 0) return false;
            bool consumeVein = false;
            if (miningRate > 0.00001f)
            {
                cache.CostFrac += miningRate;
                if (cache.CostFrac >= 1.0) { consumeVein = true; cache.CostFrac -= 1.0; }
            }
            if (consumeVein)
            {
                veinPool[index].amount--;
                factory.veinGroups[veinPool[index].groupIndex].amount--;
                if (veinPool[index].amount <= 0) factory.RemoveVeinWithComponents(index); 
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
    }
}
