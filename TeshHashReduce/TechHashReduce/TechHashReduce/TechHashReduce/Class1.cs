using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace TechHashReduce
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]

    public class TechHashReduce : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.TechHashReduce";
        public const string MOD_NAME = "TechHashReduce";
        public const string MOD_VERSION = "1.0.1";

        static float HashrateScale = 1.0f;

        internal void Awake()
        {
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(Patch));

            HashrateScale = Config.Bind<float>("General", "HashrateScale", 1f, "multiplies the hashrate for technologies by the value").Value;


        }

        public class Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameHistoryData), "Import")]
            public static void ChangeTechCost(GameHistoryData __instance)
            {

                TechProto[] dataArray = LDB.techs.dataArray;
                for (int j = 0; j < dataArray.Length; j++)
                {
                    TechState state = __instance.techStates[dataArray[j].ID];
                    long cost = dataArray[j].GetHashNeeded(state.curLevel);
                    state.hashNeeded = cost;
                    __instance.techStates[dataArray[j].ID] = state;
                }


            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(TechProto), "GetHashNeeded")]
            public static void Modify(TechProto __instance, int levelRequest, ref long __result)
            {
                
                if (__instance.Level != __instance.MaxLevel) {
                    __result = (long)Mathf.RoundToInt(__result * HashrateScale);
                }
              
            }

        }

    }
}