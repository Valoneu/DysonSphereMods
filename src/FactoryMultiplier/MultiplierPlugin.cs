using BepInEx;
using BepInEx.Configuration;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;
using DysonSphereMods.Shared;
using static FactoryMultiplier.Util.PluginConfig;

namespace FactoryMultiplier
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class MultiplierPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        public static int BattlefieldAnalysisBaseProtoId = 3009;

        private void Awake()
        {
            InitConfig(this.Config);
            InitKeyBinds();
            Log.Init(Logger);
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(PowerConsumptionPatcher));
            _harmony.PatchAll(typeof(PowerGenerationPatcher));
            _harmony.PatchAll(typeof(MultiplierPlugin));
            _harmony.PatchAll(typeof(AssemblerPatcher));
            _harmony.PatchAll(typeof(TurretPatcher));
            Logger.LogInfo($"Plugin: {MyPluginInfo.PLUGIN_GUID} {MyPluginInfo.PLUGIN_VERSION} is loaded!");
        }

        private void Update()
        {
            var testKeyInvoked = keyTestMode.Value && VFInput.alt && Input.GetKeyDown("1");
            var keyBind = CustomKeyBindSystem.GetKeyBind("ToggleOverclock");

            if (keyBind.keyValue || testKeyInvoked)
            {
                multiplierEnabled.Value = !multiplierEnabled.Value;
                if (!multiplierEnabled.Value)
                {
                    Log.Warning($"reverting multipliers");
                    UIRealtimeTip.Popup($"Reverting factory to normal");
                }
                else
                {
                    Log.Warning($"applying multipliers. Config: Asm={assembleMultiplier.Value}, Mine={miningMultiplier.Value}, Smelt={smeltMultiplier.Value}, Lab={labMultiplier}, Ejector={ejectorMultiplier}, Sorter={inserterMultiplier}");
                    UIRealtimeTip.Popup($"Applying multipliers to factory");
                }

                RefreshAllBeltsInGame();
                RefreshAllStationsInGame();
            }
        }

        private void RefreshAllBeltsInGame()
        {
            if (GameMain.data?.factories == null) return;
            Log.Info("Refreshing all belts in game...");
            foreach (var factory in GameMain.data.factories)
            {
                if (factory?.cargoTraffic != null)
                {
                    RefreshBeltsForFactory(factory);
                }
            }
        }

        private static System.Collections.Generic.Dictionary<int, int> _originalStationMaxItemCount = new System.Collections.Generic.Dictionary<int, int>();

        public void ApplyStationMultipliers()
        {
            if (LDB.items == null) return;
            
            int multi = stationStorageMultiplier;
            foreach (var item in LDB.items.dataArray)
            {
                if (item != null && item.prefabDesc != null && item.prefabDesc.isStation)
                {
                    if (!_originalStationMaxItemCount.ContainsKey(item.ID))
                    {
                        _originalStationMaxItemCount[item.ID] = item.prefabDesc.stationMaxItemCount;
                    }
                    item.prefabDesc.stationMaxItemCount = _originalStationMaxItemCount[item.ID] * multi;
                }
            }
        }

        public void RefreshAllStationsInGame()
        {
            ApplyStationMultipliers();
            if (GameMain.data?.factories == null) return;

            Log.Info("Refreshing all stations in game...");
            foreach (var factory in GameMain.data.factories)
            {
                if (factory?.transport?.stationPool != null)
                {
                    foreach (var station in factory.transport.stationPool)
                    {
                        if (station == null || station.id <= 0 || station.entityId <= 0) continue;
                        
                        int protoId = factory.entityPool[station.entityId].protoId;
                        var item = LDB.items.Select(protoId);
                        if (item == null || item.prefabDesc == null) continue;

                        int newMax = item.prefabDesc.stationMaxItemCount;
                        // Addition storage from tech is handled separately by the game, 
                        // but station.storage[i].max is the total limit.
                        // We should probably only update if it was at the previous limit or if we are increasing it.
                        
                        if (station.storage != null)
                        {
                            for (int i = 0; i < station.storage.Length; i++)
                            {
                                if (station.storage[i].itemId > 0)
                                {
                                    // Set to the new multiplied base + tech bonus (game handles tech bonus in GetAdditionStorage)
                                    // For simplicity, we match what UIStationStorage does.
                                    int techBonus = 0;
                                    if (GameMain.history != null)
                                    {
                                        techBonus = !station.isCollector ? (!station.isVeinCollector ? (!station.isStellar ? GameMain.history.localStationExtraStorage : GameMain.history.remoteStationExtraStorage) : GameMain.history.localStationExtraStorage) : GameMain.history.localStationExtraStorage;
                                    }
                                    station.storage[i].max = newMax + techBonus;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RefreshBeltsForFactory(PlanetFactory factory)
        {
            var traffic = factory.cargoTraffic;
            int multi = beltMultiplier;
            
            // 1. Update all individual belts in the pool for station throughput and UI
            for (int i = 1; i < traffic.beltCursor; i++)
            {
                if (traffic.beltPool[i].id == i)
                {
                    int entityId = traffic.beltPool[i].entityId;
                    int protoId = factory.entityPool[entityId].protoId;
                    ItemProto beltProto = LDB.items.Select(protoId);
                    if (beltProto != null)
                    {
                        int s = beltProto.prefabDesc.beltSpeed * multi;
                        if (s > 10) s = 10; // Cap at 10 (60 items/s) to prevent CargoPath.Update crash
                        traffic.beltPool[i].speed = s;
                    }
                }
            }

            // 2. Update all path chunks to match the belt speeds they cover
            for (int i = 1; i < traffic.pathCursor; i++)
            {
                var path = traffic.pathPool[i];
                if (path != null && path.id == i && path.chunks != null)
                {
                    for (int j = 0; j < path.chunkCount; j++)
                    {
                        if (j * 3 + 2 >= path.chunks.Length) break;

                        int begin = path.chunks[j * 3];
                        int speed = 1;
                        
                        // Find the belt that covers this chunk's start index (robust search)
                        if (path.belts != null)
                        {
                            int maxSegIndex = -1;
                            foreach (int bId in path.belts)
                            {
                                if (bId > 0 && bId < traffic.beltCursor)
                                {
                                    ref var belt = ref traffic.beltPool[bId];
                                    if (belt.id == bId && belt.segIndex <= begin && belt.segIndex > maxSegIndex)
                                    {
                                        maxSegIndex = belt.segIndex;
                                        speed = belt.speed;
                                    }
                                }
                            }
                        }
                        path.chunks[j * 3 + 2] = speed;
                    }
                }
            }
        }


        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleOverclock"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1214, // Increased ID to avoid potential vanilla/mod conflicts
                    key = new CombineKey((int)toggleOverclockKey.Value.MainKey, 0, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleOverclock",
                    canOverride = true
                });
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("KEYToggleOverclock", "Enable/disable factory OverClock");
#pragma warning restore CS0618
        }

        internal void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        private static void GameBegin_Postfix()
        {
            var pluginInstance = (MultiplierPlugin)FindObjectOfType(typeof(MultiplierPlugin));
            pluginInstance?.RefreshAllBeltsInGame();
            pluginInstance?.RefreshAllStationsInGame();
        }
    }
}