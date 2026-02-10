using BepInEx;
using HarmonyLib;
using System.Linq;
using xiaoye97;
using BepInEx.Configuration;

namespace MaxLVLIncrease
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MaxLVLIncreasePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.MaxLVLIncrease";
        public const string NAME = "MaxLVLIncrease";
        public const string VERSION = "1.0.4";

        private static int _maxLevelValue = 50000;

        public void Awake()
        {
            _maxLevelValue = Config.Bind("General", "MaxLevelValue", 50000, "Sets the max level of infinite vanilla tech (10k levels default in vanilla, 50k with mod)").Value;

            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(MaxLVLIncreasePlugin));

            LDBTool.EditDataAction += TechLevelIncrease;

            Logger.LogInfo($"{NAME} v{VERSION} loaded!");
        }

        private bool _didLevelIncrease;

        private void TechLevelIncrease(Proto proto)
        {
            if (_didLevelIncrease) return;
            
            foreach (var tech in LDB.techs.dataArray.Where(t => t.MaxLevel >= 1000))
            {
                tech.MaxLevel = _maxLevelValue;
            }
            _didLevelIncrease = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.Import))]
        public static void GameHistoryData_Import_Postfix(GameHistoryData __instance)
        {
            var techs = LDB.techs.dataArray;
            for (int i = 0; i < techs.Length; i++)
            {
                var tech = techs[i];
                var techState = __instance.techStates[tech.ID];
                if (techState.maxLevel >= 10000)
                {
                    techState.maxLevel = _maxLevelValue;
                    techState.unlocked = false;
                    techState.hashUploaded = 0;
                }
                __instance.techStates[tech.ID] = techState;
            }
        }
    }
}
