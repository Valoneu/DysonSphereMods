using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace DSPFactorySpaceStations
{
    [HarmonyPatch]
    public static class SaveFixPatch
    {
        public static int updateCounter = 0;

        [HarmonyPatch(typeof(EntityData), "Import")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(ref EntityData __instance)
        {
            if (__instance.protoId == DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID)
            {
                __instance.modelIndex = (short)DSPFactorySpaceStationsPlugin.factorySpaceStationModel.ID;
                updateCounter++;
            }
        }

        /*internal static string GetFixMessage()
        {
            if (updateCounter <= 0) return "";

            return string.Format(("ModificationWarn").Translate(), SaveFixPatch.updateCounter);
        }*/
    }
}