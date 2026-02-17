using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace FarZoom
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class FarZoomPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.FarZoom";
        public const string MOD_NAME = "FarZoom";
        public const string MOD_VERSION = "1.0.0";

        public static BepInEx.Configuration.ConfigEntry<float> ZoomMultiplier;
        public static BepInEx.Configuration.ConfigEntry<float> ZoomSpeedMultiplier;
        
        // Internal state for FOV, changed via Shift + Scroll
        public static float CurrentFovMultiplier = 1f;

        private void Awake()
        {
            ZoomMultiplier = Config.Bind("General", "ZoomMultiplier", 2f, "Multiplier for the maximum zoom distance in Mech/Build mode. Default is 2.0 (2x vanilla).");
            ZoomSpeedMultiplier = Config.Bind("General", "ZoomSpeedMultiplier", 1f, "Multiplier for the zoom speed in Mech/Build mode. Default is 1.0 (vanilla speed).");

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(FarZoomPatch));
            Logger.LogInfo($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }

        public static class FarZoomPatch
        {
            // --- Input Handling ---

            [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Logic))]
            [HarmonyPrefix]
            public static void GameCamera_Logic_Prefix()
            {
                // If Shift is held and there's scroll input, adjust FOV
                if (VFInput.shift && Mathf.Abs(VFInput.mouseWheel) > 0.0001f)
                {
                    // Scroll up (positive) = increase FOV, Scroll down (negative) = decrease FOV
                    // We use a 10% step
                    float step = VFInput.mouseWheel > 0 ? 1.1f : 0.90909f;
                    CurrentFovMultiplier *= step;
                    
                    // Clamp to reasonable limits (e.g., 0.1x to 5x)
                    CurrentFovMultiplier = Mathf.Clamp(CurrentFovMultiplier, 0.1f, 5f);
                    
                    // Consume the scroll input so it doesn't also zoom the camera
                    AccessTools.PropertySetter(typeof(VFInput), nameof(VFInput.mouseWheel)).Invoke(null, new object[] { 0f });
                }
            }

            // --- FOV Patches ---

            [HarmonyPatch(typeof(RTSPoser), nameof(RTSPoser.Calculate))]
            [HarmonyPrefix]
            public static void RTSPoser_Calculate_Prefix(RTSPoser __instance)
            {
                __instance.normalFov = 60f * CurrentFovMultiplier;
                // Maximum zoom distance patch
                __instance.distMax = 57f * ZoomMultiplier.Value;
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

            // --- RTS Zoom Speed Patch ---

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
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FarZoomPlugin), nameof(ZoomSpeedMultiplier))));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BepInEx.Configuration.ConfigEntry<float>), "Value")));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                        patched = true;
                        break;
                    }
                }
                if (!patched) Log.Warning("RTSPoser_Calculate_Transpiler failed to find Clamp01");
                return codes;
            }

            // --- GameCamera Logic - Fix collision poser limits for RTS mode ---

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
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FarZoomPlugin), nameof(ZoomMultiplier))));
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BepInEx.Configuration.ConfigEntry<float>), "Value")));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                        i += 3;
                        patched = true;
                    }
                }
                if (!patched) Log.Warning("GameCamera_Logic_Transpiler failed to find 58f");
                return codes;
            }
        }
    }
}
