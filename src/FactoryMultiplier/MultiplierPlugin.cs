using BepInEx;
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

            if (CustomKeyBindSystem.GetKeyBind("ToggleOverclock").keyValue || testKeyInvoked)
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
            for (int i = 1; i < traffic.pathCursor; i++)
            {
                var path = traffic.pathPool[i];
                if (path != null && path.id == i && path.belts.Count > 0)
                {
                    // Get the protoId from the first belt on the path to determine its tier
                    int firstBeltId = path.belts[0];
                    ItemProto beltProto = LDB.items.Select(factory.entityPool[traffic.beltPool[firstBeltId].entityId].protoId);

                    for (int j = 0; j < path.chunkCount; j++)
                    {
                        // The original speed is the base speed of the belt item
                        int originalSpeed = beltProto.prefabDesc.beltSpeed;

                        // Apply our multiplier
                        path.chunks[j * 3 + 2] = originalSpeed * beltMultiplier;
                    }
                }
            }
        }


        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleOverclock"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 214,
                    key = new CombineKey((int)KeyCode.KeypadMinus, 0, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleOverclock",
                    canOverride = true
                });
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("KEYToggleOverClock", "Enable/disable factory OverClock");
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