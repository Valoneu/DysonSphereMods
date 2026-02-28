using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
using System.Diagnostics;
namespace TechHashReduce
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class TechHashReducePlugin : BaseUnityPlugin
    {
        public static float HashrateScale = 1.0f;
        private void Awake()
        {
            Log.Init(Logger);
            var range = new AcceptableValueRange<float>(0.01f, 100f);
            var hashrateScaleDesc = new ConfigDescription("Multiplies the hashrate for technologies by the value", range);
            var configEntry = Config.Bind("General", "HashrateScale", 1f, hashrateScaleDesc);
            HashrateScale = (float)range.Clamp(configEntry.Value);
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(Patch));
            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded with verbose debug logging!");
        }
    }
    public class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void ChangeTechCost(GameHistoryData __instance)
        {
            var dataArray = LDB.techs.dataArray;
            for (int i = 0; i < dataArray.Length; i++)
            {
                var tech = dataArray[i];
                if (tech != null && __instance.techStates.ContainsKey(tech.ID))
                {
                    var techState = __instance.techStates[tech.ID];
                    techState.hashNeeded = tech.GetHashNeeded(techState.curLevel);
                    if (techState.hashUploaded >= techState.hashNeeded)
                    {
                        Log.Warning($"[TechHashReduce Debug] Import check: Tech {tech.ID} loaded with hashUploaded ({techState.hashUploaded}) >= hashNeeded ({techState.hashNeeded}). Clamping uploaded.");
                        techState.hashUploaded = techState.hashNeeded;
                    }
                    __instance.techStates[tech.ID] = techState;
                }
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), "GetHashNeeded")]
        public static void Modify(TechProto __instance, int levelRequest, ref long __result)
        {
            if (__instance.MaxLevel >= 0)
            {
                long originalResult = __result;
                __result = (long)((double)__result * (double)TechHashReducePlugin.HashrateScale + 0.5);
                if (__result <= 0) 
                {
                    Log.Warning($"[TechHashReduce Debug] Tech {__instance.ID} Lvl {levelRequest}: Calculated cost was {__result} (original: {originalResult}). Forcing to 1.");
                    __result = 1;
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTech")]
        public static void UnlockTechPrefix(int techId, GameHistoryData __instance)
        {
            var techState = __instance.TechState(techId);
            Log.Info($"[TechHashReduce Debug] UnlockTech called for Tech {techId}. Current Level: {techState.curLevel}, Max Level: {techState.maxLevel}, Hash: {techState.hashUploaded}/{techState.hashNeeded}");
            Log.Info($"[TechHashReduce Debug] StackTrace: \n{new StackTrace()}");
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechUnlimited")]
        public static void UnlockTechUnlimitedPrefix(int techId, GameHistoryData __instance)
        {
            var techState = __instance.TechState(techId);
            Log.Info($"[TechHashReduce Debug] UnlockTechUnlimited called for Tech {techId}. Current Level: {techState.curLevel}, Max Level: {techState.maxLevel}, Hash: {techState.hashUploaded}/{techState.hashNeeded}");
            Log.Info($"[TechHashReduce Debug] StackTrace: \n{new StackTrace()}");
        }
    }
}
