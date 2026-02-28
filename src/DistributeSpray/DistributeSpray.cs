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
        public const string VERSION = "1.0.0";
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
            Log.Info($"{NAME} v{VERSION} loaded!");
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
                var data = GameMain.data;
                for (int fi = 0; fi < data.factoryCount; fi++) {
                    var factory = data.factories[fi];
                    if (factory?.transport == null || fi >= _statusArr.Length) continue;
                    var status = _statusArr[fi];
                    int bestLvl = 0, bestIdx = -1;
                    for (int si = 1; si < factory.transport.stationCursor; si++) {
                        var st = factory.transport.stationPool[si];
                        if (st == null || st.id != si || st.isCollector) continue;
                        for (int sl = 0; sl < st.storage.Length; sl++) {
                            if (st.storage[sl].count <= 0) continue;
                            for (int pi = 0; pi < ProlIds.Length; pi++) if (st.storage[sl].itemId == ProlIds[pi] && ProlInc[pi] > bestLvl) { bestLvl = ProlInc[pi]; bestIdx = pi; }
                        }
                    }
                    status.incLevel = bestLvl; status.bestProlIdx = bestIdx;
                    if (status.incDebt > 0 && bestIdx >= 0) {
                        int debt = status.incDebt, targetId = ProlIds[bestIdx], sprayPer = ProlSpray[bestIdx];
                        int[] consumeReg = GameMain.statistics?.production?.factoryStatPool?[factory.index]?.consumeRegister;
                        for (int si = 1; si < factory.transport.stationCursor && debt > 0; si++) {
                            var st = factory.transport.stationPool[si];
                            if (st == null || st.id != si || st.isCollector) continue;
                            for (int sl = 0; sl < st.storage.Length && debt > 0; sl++) {
                                if (st.storage[sl].itemId != targetId || st.storage[sl].count <= 0) continue;
                                int take = Math.Min((debt + sprayPer - 1) / sprayPer, st.storage[sl].count);
                                st.storage[sl].count -= take; debt -= take * sprayPer;
                                if (consumeReg != null) { lock (consumeReg) consumeReg[targetId] += take; }
                            }
                        }
                        status.incDebt = Math.Max(0, debt);
                    }
                }
            } catch { }
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
            __state = true; itemInc += (byte)deficit;
        }
        public static void InsertInto_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result > 0) { var status = SprayLogic.GetStatus(__instance.index); if (status != null) Interlocked.Add(ref status.incDebt, __result); }
        }
    }
}
