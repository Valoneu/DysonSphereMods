using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;


namespace LessVesselPower
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class LessVesselPowerPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.LessVesselPower";
        public const string MOD_NAME = "LessVesselPower";
        public const string MOD_VERSION = "1.0.5";

        private static float _vesselEnergyScale = 0.25f;

        private void Awake()
        {
            _vesselEnergyScale = Config.Bind("General", "VesselEnergyScale", 0.25f, "Multiplies the power needed for logistic vessels by the set amount").Value;

            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StationPatch));

            Logger.LogInfo($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }

        [HarmonyPatch(typeof(StationComponent))]
        public static class StationPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(StationComponent.CalcTripEnergyCost))]
            public static void CalcTripEnergyCost_Postfix(ref long __result)
            {
                __result = (long)(__result * _vesselEnergyScale);
            }
        }
    }
}