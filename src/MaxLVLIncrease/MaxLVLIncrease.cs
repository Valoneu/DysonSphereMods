using BepInEx;
using HarmonyLib;
using System.Linq;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
namespace MaxLVLIncrease
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class MaxLVLIncreasePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.MaxLVLIncrease";
        public const string NAME = "MaxLVLIncrease";
        public const string VERSION = "1.1.0";
        private static ConfigEntry<int> _maxLevelConfig;
        public void Awake()
        {
            Log.Init(Logger);
            _maxLevelConfig = Config.Bind("General", "MaxLevelValue", 50000, "Sets the max level of infinite vanilla tech (10k levels default in vanilla, 50k with mod)");
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(MaxLVLIncreasePlugin));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            int maxLevelValue = _maxLevelConfig.Value;
            int count = 0;
            if (LDB.techs != null)
            {
                foreach (var tech in LDB.techs.dataArray)
                {
                    if (tech != null && tech.MaxLevel >= 1000)
                    {
                        tech.MaxLevel = maxLevelValue;
                        count++;
                    }
                }
            }
            Log.Info($"Increased MaxLevel for {count} infinite technologies to {maxLevelValue}.");
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.Import))]
        public static void GameHistoryData_Import_Postfix(GameHistoryData __instance)
        {
            if (LDB.techs == null) return;
            int maxLevelValue = _maxLevelConfig.Value;
            var techs = LDB.techs.dataArray;
            for (int i = 0; i < techs.Length; i++)
            {
                var tech = techs[i];
                if (tech == null) continue;
                var techState = __instance.techStates[tech.ID];
                if (techState.maxLevel >= 10000)
                {
                    techState.maxLevel = maxLevelValue;
                    techState.unlocked = false;
                    techState.hashUploaded = 0;
                }
                __instance.techStates[tech.ID] = techState;
            }
        }
    }
}