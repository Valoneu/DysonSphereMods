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
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
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
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(PowerConsumptionPatcher));
            _harmony.PatchAll(typeof(PowerGenerationPatcher));
            _harmony.PatchAll(typeof(MultiplierPlugin));
            _harmony.PatchAll(typeof(AssemblerPatcher));
            _harmony.PatchAll(typeof(TurretPatcher));
            Logger.LogInfo($"Plugin: {PluginInfo.PLUGIN_GUID} {PluginInfo.PLUGIN_VERSION} is loaded!");
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
        }
    }
}