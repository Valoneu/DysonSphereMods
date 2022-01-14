using System;
using System.Collections.Concurrent;
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
            MultiplyFractionators(factorySystem);
            MultiplyEjectors(factorySystem);
            MultiplySorters(factorySystem);
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

        private static ConcurrentDictionary<int, RecipeProto> _recipeProtosById = new();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateAssemble")]
        private static void MultiplyLab(ref LabComponent __instance)
        {
            if (__instance.recipeId > 0)
            {
                if (!_recipeProtosById.TryGetValue(__instance.recipeId, out RecipeProto proto))
                {
                    RecipeProto recipeProto = LDB.recipes.Select(__instance.recipeId);
                    if (recipeProto.ID > 0)
                    {
                        proto = _recipeProtosById[__instance.recipeId] = recipeProto;
                    }
                    else
                    {
                        return;
                    }
                }

                __instance.timeSpend = proto.TimeSpend * 10000 / PluginConfig.labMultiplier;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SiloComponent), "InternalUpdate")]
        public static void SiloComponent_InternalUpdate_Prefix(ref SiloComponent __instance)
        {
            __instance.chargeSpend = ItemUtil.GetSiloProto().prefabDesc.siloChargeFrame * 10000 / PluginConfig.siloMultiplier;
            __instance.coldSpend = ItemUtil.GetSiloProto().prefabDesc.siloColdFrame * 10000 / PluginConfig.siloMultiplier;
        }
    }
}