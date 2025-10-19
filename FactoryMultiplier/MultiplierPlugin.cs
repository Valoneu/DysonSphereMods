using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using static FactoryMultiplier.Util.Log;
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

        private void Awake()
        {
            // Plugin startup logic
            InitConfig(this.Config);
            InitKeyBinds();
            logger = Logger;
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(PowerConsumptionPatcher));
            _harmony.PatchAll(typeof(PowerGenerationPatcher));
            _harmony.PatchAll(typeof(MultiplierPlugin));
            _harmony.PatchAll(typeof(AssemblerPatcher));
            _harmony.PatchAll(typeof(FactoryUI));
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
                    logger.LogWarning($"reverting multipliers");
                    UIRealtimeTip.Popup($"Reverting factory to normal");
                }
                else
                {
                    logger.LogWarning($"applying multipliers");
                    UIRealtimeTip.Popup($"Applying multipliers to factory");
                }
            }
        }

        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleOverclock"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 214,
                    key = new CombineKey((int)KeyCode.LeftShift, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleOverclock",
                    canOverride = true
                });
            ProtoRegistry.RegisterString("KEYToggleOverClock", "Enable/disable factory OverClock");
        }

        internal void OnDestroy()
        {
            _harmony.UnpatchSelf();
            FactoryUI.DestroyUI();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIEjectorWindow), nameof(UIEjectorWindow._OnOpen))]
        private static void UIEjectorWindow_Open_Postfix(UIEjectorWindow __instance)
        {
            if (!enableAssemblerPopupLogMessage.Value)
                return;
            var ejector = __instance.factorySystem.ejectorPool[__instance.ejectorId];
            logger.LogInfo($"opened ejector {ejector.chargeSpend} {ejector.coldSpend} {JsonUtility.ToJson(ejector)}");
            var powerConsumerComponent = __instance.powerSystem.consumerPool[ejector.pcId];
            logger.LogInfo($"ejector consumption {JsonUtility.ToJson(powerConsumerComponent)}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAssemblerWindow), "_OnOpen")]
        private static void UIAssembler_Open_Postfix(UIAssemblerWindow __instance)
        {
            if (!enableAssemblerPopupLogMessage.Value)
                return;
            var ac = __instance.factorySystem.assemblerPool[__instance.assemblerId];
            logger.LogInfo($"opened assembler {ac.speed} {JsonUtility.ToJson(ac)}");
            var powerConsumerComponent = __instance.powerSystem.consumerPool[ac.pcId];
            logger.LogInfo($"assembler consumption {JsonUtility.ToJson(powerConsumerComponent)}");
        }
    }
}