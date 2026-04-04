using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
namespace DistributeWarpers
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class DistributeWarpersPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.DistributeWarpers";
        public const string NAME = "DistributeWarpers";
        public const string VERSION = "1.1.1";
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<int> TargetWarperCount;
        public static ConfigEntry<int> CheckInterval;
        private void Awake()
        {
            Log.Init(Logger);
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable/disable automatic warper distribution");
            TargetWarperCount = Config.Bind("General", "TargetCount", 50, "How many warpers each station should try to maintain (max 50)");
            CheckInterval = Config.Bind("General", "CheckInterval", 120, "How often (in ticks) to check and distribute");
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GameBeginPatch));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
    }
    [HarmonyPatch]
    public static class GameBeginPatch
    {
        private static bool _subscribed = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void OnGameBegin()
        {
            try {
                if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= WarperLogic.OnFactoryFrameEnd; _subscribed = false; }
                GameMain.logic.onFactoryFrameEnd += WarperLogic.OnFactoryFrameEnd; _subscribed = true;
            } catch { }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            try { if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= WarperLogic.OnFactoryFrameEnd; _subscribed = false; } } catch { }
        }
    }
    public static class WarperLogic
    {
        private const int WARPER_ID = 1210;
        public static void OnFactoryFrameEnd()
        {
            try {
                if (!DistributeWarpersPlugin.ModEnabled.Value || GameMain.data == null || GameMain.gameTick % (long)DistributeWarpersPlugin.CheckInterval.Value != 0) return;
                var data = GameMain.data;
                for (int fi = 0; fi < data.factoryCount; fi++) {
                    var factory = data.factories[fi];
                    if (factory?.transport == null) continue;
                    int totalAvailable = 0;
                    for (int i = 1; i < factory.transport.stationCursor; i++) {
                        var st = factory.transport.stationPool[i];
                        if (st == null || st.id != i || st.isCollector) continue;
                        for (int j = 0; j < st.storage.Length; j++) if (st.storage[j].itemId == WARPER_ID && st.storage[j].count > 0) totalAvailable += st.storage[j].count;
                    }
                    if (totalAvailable <= 0) continue;
                    for (int i = 1; i < factory.transport.stationCursor; i++) {
                        var st = factory.transport.stationPool[i];
                        if (st == null || st.id != i || st.isCollector || !st.isStellar) continue;
                        int needed = Math.Min(DistributeWarpersPlugin.TargetWarperCount.Value, 50) - st.warperCount;
                        if (needed <= 0) continue;
                        int toAdd = Math.Min(needed, totalAvailable);
                        int removed = TakeWarpersFromDepot(factory.transport, toAdd);
                        if (removed > 0) {
                            st.warperCount += removed; totalAvailable -= removed;
                        }
                        if (totalAvailable <= 0) break;
                    }
                }
            } catch { }
        }
        private static int TakeWarpersFromDepot(PlanetTransport transp, int count)
        {
            int rem = count;
            for (int i = 1; i < transp.stationCursor; i++) {
                var st = transp.stationPool[i];
                if (st == null || st.id != i || st.isCollector) continue;
                for (int j = 0; j < st.storage.Length; j++) if (st.storage[j].itemId == WARPER_ID && st.storage[j].count > 0) {
                    int take = Math.Min(rem, st.storage[j].count); st.storage[j].count -= take; rem -= take;
                    if (rem <= 0) return count;
                }
            }
            return count - rem;
        }
    }
}
