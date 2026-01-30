using HarmonyLib;
using FactoryMultiplier.Util;

namespace FactoryMultiplier
{
    [HarmonyPatch(typeof(TurretComponent))]
    public static class TurretPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TurretComponent.InternalUpdate))]
        public static void InternalUpdate_Prefix(ref float power)
        {
             if (PluginConfig.multiplierEnabled.Value)
                 power *= PluginConfig.turretMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TurretComponent.Aim))]
        public static void Aim_Prefix(ref float power)
        {
             if (PluginConfig.multiplierEnabled.Value)
                 power *= PluginConfig.turretMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TurretComponent.Shoot))]
        public static void Shoot_Prefix(ref float power)
        {
             if (PluginConfig.multiplierEnabled.Value)
                 power *= PluginConfig.turretMultiplier;
        }
    }
}
