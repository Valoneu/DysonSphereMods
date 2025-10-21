using FactoryMultiplier.Util;
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using UnityEngine;
using static FactoryMultiplier.Util.Log;

namespace FactoryMultiplier
{
    public static class PowerGenerationPatcher
    {
        enum FuelConsumerType
        {
            None,
            Chemical,
            Nuclear,
            Antimatter,
            Storage,
        }

        private static FuelConsumerType GetFuelConsumerType(PowerGeneratorComponent powerGenerator)
        {
            var fuelMask = powerGenerator.fuelMask;
            if ((fuelMask & 1) == 1)
            {
                return FuelConsumerType.Chemical;
            }
            if ((fuelMask & 2) == 2)
            {
                return FuelConsumerType.Nuclear;
            }
            if ((fuelMask & 4) == 4)
            {
                return FuelConsumerType.Antimatter;
            }
            if ((fuelMask & 8) == 8)
            {
                return FuelConsumerType.Storage;
            }


            return FuelConsumerType.None;
        }

        private static ConcurrentDictionary<int, ConcurrentDictionary<int, FuelConsumerType>> _planetIdToEntityIdToConsumerType = new ConcurrentDictionary<int, ConcurrentDictionary<int, FuelConsumerType>>();
        private static bool _loggedNoneConsumerOnce;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(bool), typeof(int) })]
        private static void PowerSystem_GameTick_Prefix_GenPool(PowerSystem __instance)
        {
            for (int index = 1; index < __instance.genCursor; ++index)
            {
                ref PowerGeneratorComponent generatorComponent = ref __instance.genPool[index];

                var entityData = __instance.factory.entityPool[generatorComponent.entityId];
                var itemProto = LDB.items.Select(entityData.protoId);

                if (generatorComponent.photovoltaic)
                {
                    generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genSolarMultiplier;
                }
                if (generatorComponent.wind)
                {
                    generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genWindMultiplier;
                }
                if (generatorComponent.geothermal)
                {
                    generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genGeoMultiplier;
                }


                if (IsFuelConsumer(generatorComponent))
                {
                    if (generatorComponent.fuelId > 0)
                    {
                        if (!_planetIdToEntityIdToConsumerType.TryGetValue(__instance.factory.planetId,
                                        out ConcurrentDictionary<int, FuelConsumerType> planetFuelConsumingGenerators))

                        {
                            _planetIdToEntityIdToConsumerType[__instance.factory.planetId] = planetFuelConsumingGenerators = new ConcurrentDictionary<int, FuelConsumerType>();
                        }

                        if (!planetFuelConsumingGenerators.TryGetValue(generatorComponent.entityId, out FuelConsumerType type))
                        {
                            planetFuelConsumingGenerators[generatorComponent.entityId] = type = GetFuelConsumerType(generatorComponent);
                        }

                        switch (type)
                        {
                            case FuelConsumerType.None:
                                LogOnce("Invalid fuel consumer type for generator {0}", ref _loggedNoneConsumerOnce, generatorComponent);
                                break;
                            case FuelConsumerType.Chemical:
                                generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genThermalMultiplier;
                                generatorComponent.useFuelPerTick = itemProto.prefabDesc.useFuelPerTick * PluginConfig.genThermalMultiplier;
                                break;
                            case FuelConsumerType.Nuclear:
                                generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genFusionMultiplier;
                                generatorComponent.useFuelPerTick = itemProto.prefabDesc.useFuelPerTick * PluginConfig.genFusionMultiplier;
                                break;
                            case FuelConsumerType.Antimatter:
                                generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genStarMultiplier;
                                generatorComponent.useFuelPerTick = itemProto.prefabDesc.useFuelPerTick * PluginConfig.genStarMultiplier;

                                break;
                            case FuelConsumerType.Storage:
                                generatorComponent.genEnergyPerTick = itemProto.prefabDesc.genEnergyPerTick * PluginConfig.genExchMultiplier;
                                break;
                            default:
                                Log.Warn($"how did this even happen? {JsonUtility.ToJson(generatorComponent)}");
                                break;
                        }
                    }
                }
            }
        }


        private static bool IsFuelConsumer(PowerGeneratorComponent generatorComponent)
        {
            int[] fuelNeed = ItemProto.fuelNeeds[generatorComponent.fuelMask];
            if (fuelNeed == null)
                return false;
            return fuelNeed.Length > 0;
        }
    }
}