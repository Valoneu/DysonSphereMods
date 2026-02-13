using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CloserStations
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class CloserStationsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.CloserStations";
        public const string NAME = "CloserStations";
        public const string VERSION = "1.0.0";

        public static BepInEx.Configuration.ConfigEntry<float> DistanceMultiplier;

        private void Awake()
        {
            DistanceMultiplier = Config.Bind("General", "DistanceMultiplier", 0.75f, "Multiplier for the minimum distance between logistics stations. Default is 0.75.");

            Log.Init(Logger);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(CloserStationsPatch));
            Logger.LogInfo($"{NAME} v{VERSION} loaded!");
        }

        public static class CloserStationsPatch
        {
            [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Click_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                float mult = DistanceMultiplier.Value;
                float sqrMult = mult * mult;

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4)
                    {
                        float val = (float)codes[i].operand;
                        if (val == 225f || val == 625f || val == 841f || val == 14297f)
                        {
                            codes[i].operand = val * sqrMult;
                        }
                    }
                }

                return codes;
            }

            [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Blueprint_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                float mult = DistanceMultiplier.Value;
                float sqrMult = mult * mult;

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4)
                    {
                        float val = (float)codes[i].operand;
                        if (val == 225f || val == 625f || val == 841f || val == 14297f)
                        {
                            codes[i].operand = val * sqrMult;
                        }
                    }
                }

                return codes;
            }
        }
    }
}
