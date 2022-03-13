using BepInEx;
using HarmonyLib;

namespace PilerMax
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class plugin : BaseUnityPlugin 
    {
        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
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
