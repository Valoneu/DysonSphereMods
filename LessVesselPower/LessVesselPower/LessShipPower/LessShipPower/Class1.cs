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

    public class LessVesselPower : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.LessVesselPower";
        public const string MOD_NAME = "LessVesselPower";
        public const string MOD_VERSION = "1.0.4";

       static float vesselEnergyScale = 1.0f;
       
        internal void Awake()
        {
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(Patch));

            vesselEnergyScale = Config.Bind<float>("General", "VesselEnergyScale", 0.25f, "multiplies the power needed for logistic vessels by the set amount").Value;


        }

        [HarmonyPatch]
        public class Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StationComponent), "CalcTripEnergyCost")]
            public static void Postfix(ref long __result)
            {
                __result = (long)(__result * vesselEnergyScale);
                ;
            }
        }
    }
}