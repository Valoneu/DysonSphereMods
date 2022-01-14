using System;
using FactoryMultiplier.Util;
using HarmonyLib;

namespace FactoryMultiplier
{
    public static class AssemblerPatcher
    {
        // Depending on whether GameMain.multithreadSystem.multithreadSystemEnable one of these next two will be used 
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool) })]
        private static void FactorySystem_GameTick_TwoArgs_Prefix(FactorySystem __instance)
        {
            FactorySystemPatch(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void FactorySystem_GameTick_Prefix(FactorySystem __instance)
        {
            FactorySystemPatch(__instance);
        }
        
        private static void FactorySystemPatch(FactorySystem factorySystem)
        {
            MultiplyAssemblers(factorySystem);
            MultiplyLabs(factorySystem);
            MultiplyFractionators(factorySystem);
            MultiplySilos(factorySystem);
            MultiplyEjectors(factorySystem);
            MultiplySorters(factorySystem);
        }

        private static void MultiplySilos(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.siloCursor; ++index)
            {
                var siloComponent = factorySystem.siloPool[index];
                if (siloComponent.id == index && ItemUtil.IsSilo(siloComponent.entityId))
                {
                    var itemProto = LDB.items.Select(siloComponent.entityId);
                    siloComponent.chargeSpend = itemProto.prefabDesc.siloChargeFrame * 10000 / PluginConfig.siloMultiplier;
                    siloComponent.coldSpend = itemProto.prefabDesc.siloColdFrame * 10000 / PluginConfig.siloMultiplier;
                }
            }
        }

        private static void MultiplyEjectors(FactorySystem factorySystem)
        {
            ItemProto proto = null;
            for (int index = 1; index < factorySystem.ejectorCursor; ++index)
            {
                var ejectorComponent = factorySystem.ejectorPool[index];
                if (ejectorComponent.id == index)
                {
                    if (proto == null)
                    {
                        var entityData = factorySystem.factory.entityPool[ejectorComponent.entityId];
                        proto = LDB.items.Select(entityData.protoId);
                    }

                    ejectorComponent.chargeSpend = proto.prefabDesc.ejectorChargeFrame * 10000 / PluginConfig.ejectorMultiplier;
                    ejectorComponent.coldSpend = proto.prefabDesc.ejectorColdFrame * 10000 / PluginConfig.ejectorMultiplier;
                }
            }
        }

        private static void MultiplyFractionators(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.fractionateCursor; ++index)
            {
                if (factorySystem.fractionatePool[index].id == index)
                    factorySystem.fractionatePool[index].produceProb = PluginConfig.fractionatorMultiplier * 0.01f;
            }
        }

        private static void MultiplyLabs(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.labCursor; ++index)
            {
                var labComponent = factorySystem.labPool[index];
                if (labComponent.id != index) 
                    continue;
                RecipeProto recipeProto = LDB.recipes.Select(labComponent.recipeId);
                if (labComponent.recipeId > 0)
                {
                    labComponent.timeSpend = recipeProto.TimeSpend * 10000 / PluginConfig.labMultiplier;
                }
            }
        }

        private static void MultiplyAssemblers(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.assemblerCursor; ++index)
            {
                int entityId = factorySystem.assemblerPool[index].entityId;
                if (entityId > 0)
                {
                    ItemProto assemblerProto = LDB.items.Select(factorySystem.factory.entityPool[entityId].protoId);
                    ERecipeType eRecipeType = ItemUtil.GetRecipeByProtoId(assemblerProto.ID);
                    int multi = PluginConfig.GetMultiplierByRecipe(eRecipeType);

                    factorySystem.assemblerPool[index].speed = multi * assemblerProto.prefabDesc.assemblerSpeed;
                }
            }
        }

        private static void MultiplySorters(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.inserterCursor; ++index)
            {
                int entityId = factorySystem.inserterPool[index].entityId;
                if (entityId > 0)
                {
                    ItemProto inserterProto = LDB.items.Select(factorySystem.factory.entityPool[entityId].protoId);
                    factorySystem.inserterPool[index].stt = inserterProto.prefabDesc.inserterSTT / PluginConfig.inserterMultiplier.Value;
                }
            }
        }
    }
}