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
    public class TechHashReducePlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.TechHashReduce";
        public const string MOD_NAME = "TechHashReduce";
        public const string MOD_VERSION = "1.1.2";

        public static float HashrateScale = 1.0f;

        private void Awake()
        {
            var range = new AcceptableValueRange<float>(0.01f, 100f);
            var hashrateScaleDesc = new ConfigDescription("Multiplies the hashrate for technologies by the value", range);
            HashrateScale = Config.Bind("General", "HashrateScale", 1f, hashrateScaleDesc).Value;

            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(TechPatch));

            Logger.LogInfo($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }
    }

    public static class TechPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.Import))]
        public static void GameHistoryData_Import_Postfix(GameHistoryData __instance)
        {
            var techs = LDB.techs.dataArray;
            for (int i = 0; i < techs.Length; i++)
            {
                var tech = techs[i];
                var techState = __instance.techStates[tech.ID];
                if (techState.hashUploaded >= techState.hashNeeded)
                {
                    techState.hashUploaded = techState.hashNeeded;
                }
                __instance.techStates[tech.ID] = techState;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), nameof(TechProto.GetHashNeeded))]
        public static void GetHashNeeded_Postfix(TechProto __instance, int levelRequest, ref long __result)
        {
            if (__instance.MaxLevel >= 0)
            {
                __result = (long)(__result * TechHashReducePlugin.HashrateScale + 0.5);
            }
        }
    }
}