using System;
using FactoryMultiplier.Util;
using HarmonyLib;
using DysonSphereMods.Shared;
using static FactoryMultiplier.Util.PluginConfig;

namespace FactoryMultiplier
{
    public static class PowerConsumptionPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(bool), typeof(int) })]
        private static void PowerSystem_GameTick_Prefix(PowerSystem __instance)
        {
            try
            {
                MultiplyPowerConsumption(__instance);
            }
            catch (Exception e)
            {
                Log.Warning($"Multiply power failed. {e.Message} {e.StackTrace}");
            }

            try
            {
                MultiplyReceivers(__instance);
            }
            catch (Exception e)
            {
                Log.Warning($"Multiply gamma exception {e.Message} {e.StackTrace}");
            }
        }
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
                    if (itemProto == null)
                        continue;

                    // --- FINAL FIX ---
                    // Skip special buildings with dynamic power sliders.
                    // 3009 is the Battlefield Analysis Base ProtoId you found.
                    if (itemProto.prefabDesc.isStation ||
                        itemProto.prefabDesc.isPowerExchanger ||
                        itemProto.ID == 3009)
                    {
                        continue;
                    }

                    var multiplier = -1;

                    if (itemProto.prefabDesc.isAssembler)
                    {
                        var recipe = ItemUtil.GetRecipeByProtoId(itemProto.ID);
                        multiplier = GetMultiplierByRecipe(recipe);
                    }
                    else
                    {
                        if (itemProto.prefabDesc.isLab)
                        {
                            var entityData = powerSystem.factory.entityPool[powerConsumerComponent.entityId];
                            var labComponent = powerSystem.factory.factorySystem.labPool[entityData.labId];
                            if (labComponent.researchMode)
                                multiplier = 1;
                            else
                                multiplier = labMultiplier;
                        }
                        else
                        {
                            multiplier = GetMultiplierFromPrefabDesc(itemProto.prefabDesc);
                        }
                    }

                    var prefabEnergyPerTick = itemProto.prefabDesc.workEnergyPerTick;
                    powerSystem.consumerPool[index].workEnergyPerTick = (int)(drawMultiplier.Value * multiplier * prefabEnergyPerTick);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.SetPCState))]
        public static void StationComponent_SetPCState_Postfix(ref StationComponent __instance, PowerConsumerComponent[] pcPool)
        {
            __instance.energyPerTick = (long)(pcPool[__instance.pcId].requiredEnergy * genExchMultiplier);
            pcPool[__instance.pcId].requiredEnergy = __instance.energyPerTick;
        }

        public static void MultiplyReceivers(PowerSystem powerSystem)
        {
            for (int index = 1; index < powerSystem.genCursor; ++index)
            {
                int entityId = powerSystem.genPool[index].entityId;
                int protoId = powerSystem.factory.entityPool[entityId].protoId;
                var itemProto = LDB.items.Select(protoId);
                if (powerSystem.genPool[index].id == index && powerSystem.genPool[index].gamma)
                {
                    powerSystem.genPool[index].genEnergyPerTick = gammaMultiplier.Value * itemProto.prefabDesc.genEnergyPerTick;
                }
            }
        }
    }
}