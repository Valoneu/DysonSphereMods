using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using DysonSphereMods.Shared;
namespace CloserStations
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class CloserStationsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.CloserStations";
        public const string NAME = "CloserStations";
        public const string VERSION = "1.1.0";
        public static ConfigEntry<float> DistanceMultiplier;
        private void Awake()
        {
            Log.Init(Logger);
            InitConfig(Config);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(CloserStationsPatcher));
            Log.Info($"{NAME} v{VERSION} initialized successfully!");
        }
        private void InitConfig(ConfigFile confFile)
        {
            DistanceMultiplier = confFile.Bind("General", "DistanceMultiplier", 0.75f, "Multiplier for the minimum distance between logistics stations. Default is 0.75.");
        }
    }
    public static class CloserStationsPatcher
    {
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Click_Transpiler(IEnumerable<CodeInstruction> instructions)
            => CommonTranspiler(instructions);
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Blueprint_Transpiler(IEnumerable<CodeInstruction> instructions)
            => CommonTranspiler(instructions);
        private static IEnumerable<CodeInstruction> CommonTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            float mult = CloserStationsPlugin.DistanceMultiplier.Value;
            float sqrMult = mult * mult;
            int replaceCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4)
                {
                    float val = (float)codes[i].operand;
                    if (val == 225f || val == 625f || val == 841f || val == 14297f)
                    {
                        codes[i].operand = val * sqrMult;
                        replaceCount++;
                    }
                }
            }
            Log.Debug($"Replaced {replaceCount} magic distances in transpiler.");
            return codes;
        }
    }
}