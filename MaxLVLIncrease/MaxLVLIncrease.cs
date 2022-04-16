using BepInEx;
using HarmonyLib;
using System.Linq;
using xiaoye97;

namespace MaxLVLIncrease
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class plugin : BaseUnityPlugin 
    {
        public const string GUID = "com.Valoneu.MaxLVLIncrease";
        public const string NAME = "MaxLVLIncrease";
        public const string VERSION = "1.0.0";

        private void Awake()
        {
            Harmony harmony = new Harmony(GUID);
            LDBTool.EditDataAction += TechLevelIncrease;
        }
        bool didLevelIncrease = false;
        void TechLevelIncrease(Proto proto)
        {
            if (didLevelIncrease) return;
            foreach (var tech in LDB.techs.dataArray.Where(t => t.MaxLevel >= 1000))
            {
                tech.MaxLevel = 50000;
            }
            didLevelIncrease = true;
        }
    }
}
