using BepInEx;
using HarmonyLib;

namespace MaxLVLIncrease
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class plugin : BaseUnityPlugin 
    {
        private void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(LVLPatch));
        }
    }

    public class LVLPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(TechState), MethodType.Constructor)]
        public static void MaxLevelPatch(ref TechState __instance)
        {
            if (__instance.maxLevel == 10000)
                __instance.maxLevel = 50000;
        }
    }
}
