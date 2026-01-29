using BepInEx;
using HarmonyLib;

namespace PilerMax
{
    [BepInPlugin("com.Valoneu.PilerMax", "PilerMax", "1.0.0")]
    public class plugin : BaseUnityPlugin 
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("com.Valoneu.PilerMax");
            harmony.PatchAll(typeof(PilerPatch));
        }
    }

    public class PilerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PilerComponent), "InternalUpdate")]
        public static void PilerStack(ref PilerComponent __instance)
        {
            if (__instance.cacheItemId1 != 0 && __instance.cacheCdTick < 2)
                __instance.cacheCdTick = 2;
        }
    }
}
