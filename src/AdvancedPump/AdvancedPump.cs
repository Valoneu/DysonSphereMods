using System;
using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using UnityEngine;
namespace AdvancedPump
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class AdvancedPumpPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.AdvancedPump";
        public const string NAME = "AdvancedPump";
        public const string VERSION = "1.0.0";
        private void Awake()
        {
            Log.Init(Logger);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(PlacementPatch));
            harmony.PatchAll(typeof(GameBeginPatch));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
    }
    [HarmonyPatch]
    public static class PlacementPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        public static void CheckBuildConditions_Postfix(BuildTool_Click __instance, ref bool __result)
        {
            if (__instance.buildPreviews == null || __instance.planet == null || __instance.planet.waterItemId <= 0) return;
            for (int i = 0; i < __instance.buildPreviews.Count; i++) {
                BuildPreview bp = __instance.buildPreviews[i];
                if (bp.desc != null && bp.desc.isVeinCollector && bp.condition == EBuildCondition.NeedResource) {
                    bp.condition = EBuildCondition.Ok; __result = true;
                }
            }
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
                if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= MinerConvertLogic.OnFactoryFrameEnd; _subscribed = false; }
                GameMain.logic.onFactoryFrameEnd += MinerConvertLogic.OnFactoryFrameEnd; _subscribed = true;
            } catch { }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            try { if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= MinerConvertLogic.OnFactoryFrameEnd; _subscribed = false; } } catch { }
        }
    }
    public static class MinerConvertLogic
    {
        public static void OnFactoryFrameEnd()
        {
            try {
                var data = GameMain.data;
                if (data == null || GameMain.gameTick % 30 != 0) return;
                for (int fi = 0; fi < data.factoryCount; fi++) {
                    var factory = data.factories[fi];
                    if (factory?.planet == null || factory.planet.waterItemId <= 0 || factory.transport == null || factory.factorySystem == null) continue;
                    var minerPool = factory.factorySystem.minerPool;
                    for (int si = 1; si < factory.transport.stationCursor; si++) {
                        var station = factory.transport.stationPool[si];
                        if (station == null || station.id != si || !station.isVeinCollector || station.minerId <= 0 || station.minerId >= minerPool.Length) continue;
                        ref MinerComponent miner = ref minerPool[station.minerId];
                        if (miner.id != station.minerId || miner.veinCount > 0) continue;
                        if (miner.type != EMinerType.Water || miner.speed < 150000)
                        {
                            miner.type = EMinerType.Water;
                            miner.speed = 150000;
                            if (station.collectionIds != null && station.collectionIds.Length > 0) station.collectionIds[0] = factory.planet.waterItemId;
                            if (station.storage != null && station.storage.Length > 0) station.storage[0].itemId = factory.planet.waterItemId;
                            if (factory.entitySignPool != null && station.minerId < factory.entitySignPool.Length)
                            {
                                factory.entitySignPool[station.minerId].iconId0 = 0;
                                factory.entitySignPool[station.minerId].iconType = 0;
                            }
                        }
                    }
                }
            } catch { }
        }
    }
}
