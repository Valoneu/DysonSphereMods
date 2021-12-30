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
        public const string MOD_VERSION = "1.1.2";

        public static float HashrateScale = 1.0f;

        public void Awake()
        {
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(Patch));

            AcceptableValueRange<float> range = new AcceptableValueRange<float>(0.01f, 100f);
         
            ConfigDescription HashrateScaleDesc = new ConfigDescription(
                "multiplies the hashrate for technologies by the value", range
                );

            HashrateScale = (float)range.Clamp(Config.Bind<float>("General", "HashrateScale", 1f, HashrateScaleDesc).Value);
        }
    }

    public class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void ChangeTechCost(GameHistoryData __instance)
        {
            TechProto[] dataArray = LDB.techs.dataArray;
            for (int i = 0; i < dataArray.Length; i++)
            {
                TechState techState = __instance.techStates[dataArray[i].ID];
                if (techState.hashUploaded >= techState.hashNeeded)
                {
                    techState.hashUploaded = techState.hashNeeded;
                }
                __instance.techStates[dataArray[i].ID] = techState;
                
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), "GetHashNeeded")]
        public static void Modify(TechProto __instance, int levelRequest, ref long __result)
        {
            if (__instance.MaxLevel >= 0)
            {
                __result = (long)((double)__result * (double)TechHashReduce.HashrateScale + 0.5);
            }
        }
    }
}