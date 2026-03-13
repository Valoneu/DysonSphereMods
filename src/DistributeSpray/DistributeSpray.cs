using System;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
namespace DistributeSpray
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class DistributeSprayPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.DistributeSpray";
        public const string NAME = "DistributeSpray";
        public const string VERSION = "1.0.2";
        public static ConfigEntry<bool> ModEnabled;
        private void Awake()
        {
            Log.Init(Logger);
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable/disable automatic spraying.");
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GameBeginPatch));
            MethodInfo insertUint = AccessTools.Method(typeof(PlanetFactory), "InsertInto", new Type[] { typeof(uint), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType() });
            if (insertUint != null) harmony.Patch(insertUint, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Postfix)));
            MethodInfo insertInt = AccessTools.Method(typeof(PlanetFactory), "InsertInto", new Type[] { typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType() });
            if (insertInt != null) harmony.Patch(insertInt, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Postfix)));
            MethodInfo insertStorage = AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.InsertIntoStorage), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int).MakeByRefType(), typeof(bool) });
            if (insertStorage != null) harmony.Patch(insertStorage, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertIntoStorage_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertIntoStorage_Postfix)));
            MethodInfo addItem = AccessTools.Method(typeof(StationComponent), nameof(StationComponent.AddItem), new Type[] { typeof(int), typeof(int), typeof(int) });
            if (addItem != null) harmony.Patch(addItem, prefix: new HarmonyMethod(typeof(StationPatches), nameof(StationPatches.AddItem_Prefix)), postfix: new HarmonyMethod(typeof(StationPatches), nameof(StationPatches.AddItem_Postfix)));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
    }
    public static class StationPatches
    {
        public static void AddItem_Prefix(StationComponent __instance, int itemId, int count, ref int inc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value || count <= 0 || itemId <= 0 || GameMain.data == null) return;
            PlanetFactory factory = null;
            if (GameMain.data.factories != null)
            {
                foreach (var f in GameMain.data.factories)
                {
                    if (f != null && f.planetId == __instance.planetId) { factory = f; break; }
                }
            }
            if (factory == null) return;
            var s = SprayLogic.GetStatus(factory.index);
            if (s == null || s.incLevel <= 0) return;
            int deficit = count * s.incLevel - inc;
            if (deficit <= 0) return;
            __state = true; inc += deficit;
        }
        public static void AddItem_Postfix(StationComponent __instance, int __result, bool __state)
        {
            if (__state && __result > 0) 
            {
                PlanetFactory factory = null;
                if (GameMain.data != null && GameMain.data.factories != null)
                {
                    foreach (var f in GameMain.data.factories)
                    {
                        if (f != null && f.planetId == __instance.planetId) { factory = f; break; }
                    }
                }
                if (factory != null)
                {
                    var status = SprayLogic.GetStatus(factory.index);
                    if (status != null) Interlocked.Add(ref status.incDebt, __result);
                }
            }
        }
    }
    public class SprayStatus { public int incLevel; public volatile int incDebt; public int bestProlIdx; }
    [HarmonyPatch]
    public static class GameBeginPatch
    {
        private static bool _subscribed = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void OnGameBegin()
        {
            try {
                SprayLogic.InitStatusArray(GameMain.data.factories.Length);
                if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= SprayLogic.OnFactoryFrameEnd; _subscribed = false; }
                GameMain.logic.onFactoryFrameEnd += SprayLogic.OnFactoryFrameEnd; _subscribed = true;
            } catch { }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            try { if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= SprayLogic.OnFactoryFrameEnd; _subscribed = false; } } catch { }
        }
    }
    public static class SprayLogic
    {
        private static SprayStatus[] _statusArr;
        public static readonly int[] ProlIds = { 1143, 1142, 1141 };
        public static readonly int[] ProlInc = { 4, 2, 1 };
        public static readonly int[] ProlSpray = { 60, 24, 12 };
        public static void InitStatusArray(int size) { _statusArr = new SprayStatus[size]; for (int i = 0; i < size; i++) _statusArr[i] = new SprayStatus(); }
        public static SprayStatus GetStatus(int fi) => (_statusArr != null && fi >= 0 && fi < _statusArr.Length) ? _statusArr[fi] : null;
        public static void OnFactoryFrameEnd()
        {
            try {
                if (!DistributeSprayPlugin.ModEnabled.Value || GameMain.data == null || _statusArr == null) return;
                NexusLogic.Init();
                var data = GameMain.data;
                var doneStars = new System.Collections.Generic.HashSet<int>();
                for (int fi = 0; fi < data.factoryCount; fi++) {
                    var factory = data.factories[fi];
                    if (factory?.transport == null || fi >= _statusArr.Length) continue;
                    var status = _statusArr[fi];
                    bool fullScan = GameMain.instance.timei % 60 == 0;
                    if (fullScan)
                    {
                        int bestLvl = 0, bestIdx = -1;
                        for (int si = 1; si < factory.transport.stationCursor; si++) {
                            var st = factory.transport.stationPool[si];
                            if (st == null || st.id != si || st.isCollector) continue;
                            for (int sl = 0; sl < st.storage.Length; sl++) {
                                int itemId = st.storage[sl].itemId;
                                int count = st.storage[sl].count;
                                if (itemId <= 0 || count <= 0) continue;
                                for (int pi = 0; pi < ProlIds.Length; pi++) if (itemId == ProlIds[pi] && ProlInc[pi] > bestLvl) { bestLvl = ProlInc[pi]; bestIdx = pi; }
                                if (status.incLevel > 0)
                                {
                                    int targetInc = count * status.incLevel;
                                    if (st.storage[sl].inc < targetInc)
                                    {
                                        int diff = targetInc - st.storage[sl].inc;
                                        int itemsToSpray = (diff + status.incLevel - 1) / status.incLevel;
                                        st.storage[sl].inc = targetInc;
                                        Interlocked.Add(ref status.incDebt, itemsToSpray);
                                    }
                                }
                            }
                        }
                        status.incLevel = bestLvl; status.bestProlIdx = bestIdx;
                    }
                    if (status.incLevel > 0)
                    {
                        int starIdx = factory.planetId / 100 - 1;
                        if (doneStars.Add(starIdx))
                        {
                            int NexusDebt = 0;
                            NexusLogic.SprayNexus(starIdx, status.incLevel, ref NexusDebt);
                            if (NexusDebt > 0) Interlocked.Add(ref status.incDebt, NexusDebt);
                        }
                    }
                    if (status.incDebt > 0 && status.bestProlIdx >= 0) {
                        int debt = status.incDebt, targetId = ProlIds[status.bestProlIdx], sprayPer = ProlSpray[status.bestProlIdx];
                        int[] consumeReg = GameMain.statistics?.production?.factoryStatPool?[factory.index]?.consumeRegister;
                        for (int si = 1; si < factory.transport.stationCursor && status.incDebt > 0; si++) {
                            var st = factory.transport.stationPool[si];
                            if (st == null || st.id != si || st.isCollector) continue;
                            for (int sl = 0; sl < st.storage.Length && status.incDebt > 0; sl++) {
                                if (st.storage[sl].itemId != targetId || st.storage[sl].count <= 0) continue;
                                int take = Math.Min((status.incDebt + sprayPer - 1) / sprayPer, st.storage[sl].count);
                                st.storage[sl].count -= take;
                                Interlocked.Add(ref status.incDebt, -take * sprayPer);
                                if (consumeReg != null) { lock (consumeReg) consumeReg[targetId] += take; }
                            }
                        }
                    }
                }
            } catch { }
        }
    }
    public static class NexusLogic
    {
        private static Type _starAssemblyType;
        private static FieldInfo _productStorageField;
        private static FieldInfo _productStorageIncField;
        private static bool _initialized = false;
        public static void Init() {
            if (_initialized) return;
            _starAssemblyType = AccessTools.TypeByName("MoreMegaStructure.StarAssembly");
            if (_starAssemblyType != null) {
                _productStorageField = AccessTools.Field(_starAssemblyType, "productStorage");
                _productStorageIncField = AccessTools.Field(_starAssemblyType, "productStorageInc");
            }
            _initialized = true;
        }
        public static void SprayNexus(int starIndex, int incLevel, ref int incDebt) {
            if (_starAssemblyType == null || _productStorageField == null || _productStorageIncField == null) return;
            var storage = _productStorageField.GetValue(null) as System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, int>>;
            var storageInc = _productStorageIncField.GetValue(null) as System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, int>>;
            if (storage != null && storage.TryGetValue(starIndex, out var items)) {
                if (!storageInc.TryGetValue(starIndex, out var incs)) return;
                foreach (var itemId in items.Keys.System_Collections_Generic_ICollection_int_ToArray()) {
                    int count = items[itemId];
                    if (itemId <= 0 || count <= 0) continue;
                    int targetInc = count * incLevel;
                    incs.TryGetValue(itemId, out int currentInc);
                    if (currentInc < targetInc) {
                        int diff = targetInc - currentInc;
                        incs[itemId] = targetInc;
                        incDebt += (diff + incLevel - 1) / incLevel;
                    }
                }
            }
        }
    }
    internal static class Extensions {
        public static T[] System_Collections_Generic_ICollection_int_ToArray<T>(this System.Collections.Generic.ICollection<T> collection) {
            T[] array = new T[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }
    }
    public static class InsertPatches
    {
        public static void InsertInto_Prefix(PlanetFactory __instance, byte itemCount, ref byte itemInc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value) return;
            var status = SprayLogic.GetStatus(__instance.index);
            if (status == null || status.incLevel <= 0) return;
            int deficit = itemCount * status.incLevel - itemInc;
            if (deficit <= 0) return;
            deficit = Math.Min(deficit, 255 - itemInc);
            __state = true; itemInc += (byte)deficit;
        }
        public static void InsertInto_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result > 0) { var status = SprayLogic.GetStatus(__instance.index); if (status != null) Interlocked.Add(ref status.incDebt, __result); }
        }
        public static void InsertIntoStorage_Prefix(PlanetFactory __instance, int count, ref int inc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value) return;
            var status = SprayLogic.GetStatus(__instance.index);
            if (status == null || status.incLevel <= 0) return;
            int deficit = count * status.incLevel - inc;
            if (deficit <= 0) return;
            __state = true; inc += deficit;
        }
        public static void InsertIntoStorage_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result > 0) { var status = SprayLogic.GetStatus(__instance.index); if (status != null) Interlocked.Add(ref status.incDebt, __result); }
        }
    }
}
