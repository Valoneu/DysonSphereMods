using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using FactoryMultiplier.Util;
using HarmonyLib;
using DysonSphereMods.Shared;

namespace FactoryMultiplier
{
    public static class AssemblerPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.LogicFrame))]
        private static void GameLogic_LogicFrame_Prefix(GameLogic __instance)
        {
            if (__instance.factories != null)
            {
                for (int i = 0; i < __instance.factoryCount; i++)
                {
                    var factory = __instance.factories[i];
                    if (factory != null && factory.factorySystem != null)
                    {
                        ProcessFactoryTick(factory.factorySystem, "GameLogic");
                    }
                }
            }
        }

        private static void ProcessFactoryTick(FactorySystem __instance, string source)
        {
            // Simple debounce/deduplication: 
            // We can't easily dedup without storing state on the FactorySystem. 
            // However, running the multiplier logic twice is idempotent (setting speed = constant * multiplier).
            
            MultiplyAssemblers(__instance);
            MultiplyFractionators(__instance);
            MultiplyMiners(__instance);
        }

        private static ConcurrentDictionary<int, int> _baseSpeedByProtoId = new();
        private static ConcurrentDictionary<int, int> _minerBaseSpeedByProtoId = new();

        private static void MultiplyMiners(FactorySystem factorySystem)
        {
            // Even if disabled, we loop to restore speed to normal (1x)
            int multiplier = PluginConfig.multiplierEnabled.Value ? PluginConfig.miningMultiplier.Value : 1;

            for (int index = 1; index < factorySystem.minerCursor; ++index)
            {
                ref var miner = ref factorySystem.minerPool[index];
                if (miner.id == index)
                {
                    int entityId = miner.entityId;
                    int protoId = factorySystem.factory.entityPool[entityId].protoId;

                    if (!_minerBaseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
                    {
                        // Mining speed (period) isn't directly exposed as 'speed' in prefabDesc typically?
                        // Actually, MinerComponent.speed is usually 10000.
                        // Let's assume standard 10000 base speed for miners if not found.
                        // Checking NewMinerComponent: "this.minerPool[index].speed = 10000;"
                        baseSpeed = 10000;
                        _minerBaseSpeedByProtoId[protoId] = baseSpeed;
                    }

                    miner.speed = multiplier * baseSpeed;
                }
            }
        }

        private static void MultiplyAssemblers(FactorySystem factorySystem)
        {
            // Removed early return to allow resetting speed when disabled

            for (int index = 1; index < factorySystem.assemblerCursor; ++index)
            {
                ref var assembler = ref factorySystem.assemblerPool[index];
                if (assembler.id == index)
                {
                    int entityId = assembler.entityId;
                    int protoId = factorySystem.factory.entityPool[entityId].protoId;

                    if (!_baseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
                    {
                        var proto = LDB.items.Select(protoId);
                        baseSpeed = _baseSpeedByProtoId[protoId] = proto.prefabDesc.assemblerSpeed;
                    }

                    ERecipeType recipeType = assembler.recipeId > 0 ? assembler.recipeType : ERecipeType.None;
                    if (recipeType == ERecipeType.None)
                        recipeType = ItemUtil.GetRecipeByProtoId(protoId);

                    int multi = PluginConfig.GetMultiplierByRecipe(recipeType);
                    
                    // Update speedOverride for instant effect during replication
                    if (assembler.replicating && assembler.speed > 0)
                    {
                        double ratio = (double)assembler.speedOverride / assembler.speed;
                        assembler.speedOverride = (int)(ratio * multi * baseSpeed);
                    }

                    assembler.speed = multi * baseSpeed;
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
            if (__instance.recipeId > 0 && PluginConfig.multiplierEnabled.Value)
            {
                // In 0.10.34, LabComponent uses 'speed' instead of 'timeSpend'.
                // Base speed is usually 10000.
                __instance.speed = 10000 * PluginConfig.labMultiplier;
                
                // 'extraSpeed' handles the proliferation/extra production logic.
                // It's scaled by 10x the base speed usually (100000).
                // We multiply the base 100000 by our multiplier.
                __instance.extraSpeed = 100000 * PluginConfig.labMultiplier;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void MultiplyLabResearch(ref float research_speed)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                research_speed *= PluginConfig.labMultiplier;
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
                // __instance.speed = 10000 * PluginConfig.inserterMultiplier; // Removed because speed is now const
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
                // __instance.speed = 10000 * PluginConfig.inserterMultiplier; // Removed because speed is now const
                __instance.delay = inserterProto.prefabDesc.inserterDelay / PluginConfig.inserterMultiplier;
            }
        }

        // =================================================================
        // INSERTER TRANSPILERS
        // Replace hardcoded '10000' and '10000.0' with multiplied values
        // =================================================================

        public static int GetInserterSpeed() => (int)(10000 * PluginConfig.inserterMultiplier);
        public static double GetInserterSpeedDouble() => 10000.0 * PluginConfig.inserterMultiplier;

        private static IEnumerable<CodeInstruction> InserterTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == 10000)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetInserterSpeed)));
                }
                else if (instruction.opcode == OpCodes.Ldc_R8 && Math.Abs((double)instruction.operand - 10000.0) < 0.001)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetInserterSpeedDouble)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        public static IEnumerable<CodeInstruction> InserterComponent_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return InserterTranspiler(instructions);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
        public static IEnumerable<CodeInstruction> InserterComponent_InternalUpdateNoAnim_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return InserterTranspiler(instructions);
        }
    }
}