using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
namespace LessShipPower
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class LessShipPowerPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> VesselEnergyScale;
        private void Awake()
        {
            Log.Init(Logger);
            VesselEnergyScale = Config.Bind("General", "VesselEnergyScale", 0.25f, "Multiplies the power needed for logistic vessels by the set amount");
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(StationPatcher));
            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }
    }
    [HarmonyPatch(typeof(StationComponent))]
    public static class StationPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StationComponent.CalcTripEnergyCost))]
        public static void CalcTripEnergyCost_Postfix(ref long __result)
        {
            __result = (long)(__result * LessShipPowerPlugin.VesselEnergyScale.Value);
        }
    }
}