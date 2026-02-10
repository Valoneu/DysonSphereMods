using BepInEx;
using HarmonyLib;

namespace PilerMax
{
    [BepInPlugin("com.Valoneu.PilerMax", "PilerMax", "1.0.0")]
    public class PilerMaxPlugin : BaseUnityPlugin 
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("com.Valoneu.PilerMax");
            harmony.PatchAll(typeof(PilerPatch));
            Logger.LogInfo("PilerMaxPlugin loaded!");
        }
    }

    public static class PilerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PilerComponent), nameof(PilerComponent.InternalUpdate))]
        public static void InternalUpdate_Prefix(ref PilerComponent __instance)
        {
            if (__instance.cacheItemId1 != 0 && __instance.cacheCdTick < 2)
            {
                __instance.cacheCdTick = 2;
            }
        }
    }
}
