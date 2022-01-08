using System;
using FactoryMultiplier.Util;
using HarmonyLib;
using static FactoryMultiplier.Util.Log;
using static FactoryMultiplier.Util.PluginConfig;

namespace FactoryMultiplier
{
    public static class PowerConsumptionPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        private static void PowerSystem_GameTick_Prefix(PowerSystem __instance)
        {
            try
            {
                MultiplyPowerConsumption(__instance);
            }
            catch (Exception e)
            {
                logger.LogWarning($"Multiply power failed. {e.Message} {e.StackTrace}");
            }

            try
            {
                // MultiplyGamma(__instance);
            }
            catch (Exception e)
            {
                logger.LogWarning($"Multiply gamma exception {e.Message} {e.StackTrace}");
            }
        }

        private static bool _loggedMultiLtOne;
        private static bool _loggedAsmOnce;

        private static void MultiplyPowerConsumption(PowerSystem powerSystem)
        {
            for (var index = 1; index < powerSystem.consumerCursor; ++index)
            {
                var powerConsumerComponent = powerSystem.consumerPool[index];
                var entityId = powerConsumerComponent.entityId;
                if (entityId > 0)
                {
                    var itemProto = LDB.items.Select(powerSystem.factory.entityPool[entityId].protoId);
                    if (itemProto.Type == EItemType.Logistics)
                        continue;
                  
                    var multiplier = -1;


                    if (itemProto.prefabDesc.isAssembler)
                    {
                        var recipe = ItemUtil.GetRecipeByProtoId(itemProto.ID);
                        multiplier = GetMultiplierByRecipe(recipe);
                    }
                    else
                    {
                        multiplier = GetMultiplierFromPrefabDesc(itemProto.prefabDesc, multiplier);
                    }


                    if (multiplier < 1)
                    {
                        LogOnce("Multiplier is < 1 ({0}) for {1}, {2}", ref _loggedMultiLtOne, multiplier, itemProto, powerConsumerComponent);
                        continue;
                    }
                    if ( powerConsumerComponent.id == 6512)
                    {
                        LogOnce("item is asmbler {0}, {1} \n {2} \n {3}", ref _loggedAsmOnce, multiplier, itemProto, powerConsumerComponent,
                            itemProto.prefabDesc);
                    }

                    var prefabEnergyPerTick = itemProto.prefabDesc.workEnergyPerTick;
                    powerSystem.consumerPool[index].workEnergyPerTick = multiplier * prefabEnergyPerTick;
                }
            }
        }
    }
}