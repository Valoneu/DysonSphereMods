using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using DysonSphereMods.Shared;
namespace FarZoom
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class FarZoomPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ZoomMultiplier;
        public static ConfigEntry<float> ZoomSpeedMultiplier;
        private Harmony _harmony;
        private void Awake()
        {
            Log.Init(Logger);
            ZoomMultiplier = Config.Bind("General", "ZoomMultiplier", 2f, "Multiplier for the maximum zoom distance in Mech/Build mode. Default is 2.0 (2x vanilla).");
            ZoomSpeedMultiplier = Config.Bind("General", "ZoomSpeedMultiplier", 1f, "Multiplier for the zoom speed in Mech/Build mode. Default is 1.0 (vanilla speed).");
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(FarZoomPatcher));
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
    public static class FarZoomPatcher
    {
        public static float CurrentFovMultiplier = 1f;
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Logic))]
        [HarmonyPrefix]
        public static void GameCamera_Logic_Prefix()
        {
            if (VFInput.shift && Mathf.Abs(VFInput.mouseWheel) > 0.0001f)
            {
                float step = VFInput.mouseWheel > 0 ? 1.1f : 0.90909f;
                CurrentFovMultiplier *= step;
                CurrentFovMultiplier = Mathf.Clamp(CurrentFovMultiplier, 0.1f, 5f);
            }
        }
        [HarmonyPatch(typeof(RTSPoser), nameof(RTSPoser.Calculate))]
        [HarmonyPrefix]
        public static void RTSPoser_Calculate_Prefix(RTSPoser __instance)
        {
            __instance.normalFov = 60f * CurrentFovMultiplier;
            __instance.distMax = 57f * FarZoomPlugin.ZoomMultiplier.Value;
        }
        [HarmonyPatch(typeof(PlanetPoser), nameof(PlanetPoser.Calculate))]
        [HarmonyPrefix]
        public static void PlanetPoser_Calculate_Prefix(PlanetPoser __instance)
        {
            __instance.normalFov = 60f * CurrentFovMultiplier;
        }
        [HarmonyPatch(typeof(GraticulePoser), nameof(GraticulePoser.Calculate))]
        [HarmonyPrefix]
        public static void GraticulePoser_Calculate_Prefix(GraticulePoser __instance)
        {
            __instance.normalFov = 40f * CurrentFovMultiplier;
        }
        [HarmonyPatch(typeof(RTSPoser), nameof(RTSPoser.Calculate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RTSPoser_Calculate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("Clamp01"))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FarZoomPlugin), nameof(FarZoomPlugin.ZoomSpeedMultiplier))));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), "Value")));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    patched = true;
                    break;
                }
            }
            if (!patched) Log.Warning("RTSPoser_Calculate_Transpiler failed to find Clamp01");
            return codes;
        }
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Logic))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GameCamera_Logic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 58f)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FarZoomPlugin), nameof(FarZoomPlugin.ZoomMultiplier))));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), "Value")));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    i += 3;
                    patched = true;
                }
            }
            if (!patched) Log.Warning("GameCamera_Logic_Transpiler failed to find 58f");
            return codes;
        }
        [HarmonyPatch(typeof(RTSPoser), nameof(RTSPoser.Calculate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RTSPoser_BlockZoom_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            for (int i = 0; i < codes.Count - 5; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("disableDist"))
                {
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Brtrue_S, codes[i + 1].operand));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))));
                    patched = true;
                    break;
                }
            }
            if (!patched) Log.Warning("RTSPoser_BlockZoom_Transpiler failed");
            return codes;
        }
        [HarmonyPatch(typeof(PlanetPoser), nameof(PlanetPoser.Calculate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PlanetPoser_BlockZoom_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            for (int i = 0; i < codes.Count - 2; i++)
            {
                if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand.ToString().Contains("inFullscreenGUI"))
                {
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Brtrue_S, codes[i + 1].operand));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFInput), nameof(VFInput.shift))));
                    patched = true;
                    break;
                }
            }
            if (!patched) Log.Warning("PlanetPoser_BlockZoom_Transpiler failed");
            return codes;
        }
    }
}