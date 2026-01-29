using BepInEx;
using HarmonyLib;
using System.Linq;
using xiaoye97;
using BepInEx.Configuration;

namespace MaxLVLIncrease
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class plugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.MaxLVLIncrease";
        public const string NAME = "MaxLVLIncrease";
        public const string VERSION = "1.0.4";

        static int MaxLevelValue = 50000;

        public void Awake()
        {
            Harmony harmony = new Harmony(GUID);
            LDBTool.EditDataAction += TechLevelIncrease; ;
            harmony.PatchAll(typeof(plugin));

            MaxLevelValue = Config.Bind<int>("General", "MaxLevelValue", 50000, "Sets the max level of infinite vanilla tech (10k levels default in vanilla, 50k with mod )").Value;
        }
        bool didLevelIncrease = false;
        void TechLevelIncrease(Proto proto)
        {
            if (didLevelIncrease) return;
            foreach (var tech in LDB.techs.dataArray.Where(t => t.MaxLevel >= 1000))
            {
                tech.MaxLevel = MaxLevelValue;
            }
            didLevelIncrease = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void MaxLevel(GameHistoryData __instance)
        {
            TechProto[] dataArray = LDB.techs.dataArray;
            for (int i = 0; i < dataArray.Length; i++)
            {
                TechState techState = __instance.techStates[dataArray[i].ID];
                if (techState.maxLevel >= 10000)
                {
                    techState.maxLevel = MaxLevelValue;
                    techState.unlocked = false;
                    techState.hashUploaded = 0;
                }
                __instance.techStates[dataArray[i].ID] = techState;

            }
        }
    }
}
