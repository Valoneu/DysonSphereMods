using System;
using System.Collections.Concurrent;
using FactoryMultiplier.Util;
using HarmonyLib;

namespace FactoryMultiplier
{
    public static class AssemblerPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool) })]
        private static void FactorySystem_GameTick_Prefix(FactorySystem __instance)
        {
            MultiplyAssemblers(__instance);
            MultiplyFractionators(__instance);
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

        // =================================================================
        // FINAL, ROBUST BELT PATCHING STRATEGY
        // These patches handle belts built or upgraded AFTER the game has loaded.
        // =================================================================

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.NewBeltComponent))]
        public static void NewBelt_Prefix(ref int speed)
        {
            speed *= PluginConfig.beltMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpgradeBeltComponent))]
        public static void UpgradeBelt_Prefix(ref int speed)
        {
            // Note: The game's UpgradeBeltComponent reads the base speed and passes it in.
            // We only need to multiply it here.
            speed *= PluginConfig.beltMultiplier;
        }


        private static void MultiplyFractionators(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.fractionatorCursor; ++index)
            {
                if (factorySystem.fractionatorPool[index].id == index)
                    factorySystem.fractionatorPool[index].produceProb = PluginConfig.fractionatorMultiplier * 0.01f;
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
                // In 0.10.34, LabComponent uses 'speed' instead of 'timeSpend'.
                // Base speed is usually 10000.
                __instance.speed = 10000 * PluginConfig.labMultiplier;
                // 'extraSpeed' handles the proliferation/extra production logic.
                // It's scaled by 100000 (10x the base speed usually).
                __instance.extraSpeed = 100000 * PluginConfig.labMultiplier;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        public static void EjectorComponent_InternalUpdate_Prefix(ref EjectorComponent __instance)
        {
            var ejectorProto = ItemUtil.ejectorProto;
            __instance.chargeSpend = ejectorProto.prefabDesc.ejectorChargeFrame * 10000 / PluginConfig.ejectorMultiplier;
            __instance.coldSpend = ejectorProto.prefabDesc.ejectorColdFrame * 10000 / PluginConfig.ejectorMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SiloComponent), "InternalUpdate")]
        public static void SiloComponent_InternalUpdate_Prefix(ref SiloComponent __instance)
        {
            __instance.chargeSpend = ItemUtil.GetSiloProto().prefabDesc.siloChargeFrame * 10000 / PluginConfig.siloMultiplier;
            __instance.coldSpend = ItemUtil.GetSiloProto().prefabDesc.siloColdFrame * 10000 / PluginConfig.siloMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        public static void InserterComponent_InternalUpdate_Prefix(ref InserterComponent __instance, PlanetFactory factory)
        {
            if (__instance.id == 0 || __instance.entityId == 0)
                return;
            var entityData = factory.entityPool[__instance.entityId];

            ItemProto inserterProto = LDB.items.Select(entityData.protoId);
            if (inserterProto.prefabDesc != null)
            {
                __instance.speed = 10000 * PluginConfig.inserterMultiplier;
                __instance.delay = inserterProto.prefabDesc.inserterDelay / PluginConfig.inserterMultiplier;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
        public static void InserterComponent_InternalUpdateNoAnim_Prefix(ref InserterComponent __instance, PlanetFactory factory)
        {
            if (__instance.id == 0 || __instance.entityId == 0)
                return;
            var entityData = factory.entityPool[__instance.entityId];

            ItemProto inserterProto = LDB.items.Select(entityData.protoId);
            if (inserterProto.prefabDesc != null)
            {
                __instance.speed = 10000 * PluginConfig.inserterMultiplier;
                __instance.delay = inserterProto.prefabDesc.inserterDelay / PluginConfig.inserterMultiplier;
            }
        }
    }
}