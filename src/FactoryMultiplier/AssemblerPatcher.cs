using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
            MultiplyLabs(__instance);
        }

        private static ConcurrentDictionary<int, int> _baseSpeedByProtoId = new();
        private static ConcurrentDictionary<int, int> _labBaseSpeedByProtoId = new();
        private static ConcurrentDictionary<int, int> _minerBaseSpeedByProtoId = new();
        private static ConcurrentDictionary<int, int> _inserterDelayByProtoId = new();

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

        private static void MultiplyLabs(FactorySystem factorySystem)
        {
            int multiplier = PluginConfig.multiplierEnabled.Value ? PluginConfig.labMultiplier : 1;

            for (int index = 1; index < factorySystem.labCursor; ++index)
            {
                ref var lab = ref factorySystem.labPool[index];
                if (lab.id == index)
                {
                    int entityId = lab.entityId;
                    int protoId = factorySystem.factory.entityPool[entityId].protoId;

                    if (!_labBaseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
                    {
                        var proto = LDB.items.Select(protoId);
                        baseSpeed = _labBaseSpeedByProtoId[protoId] = proto.prefabDesc.labAssembleSpeed;
                    }

                    // Update speedOverride for instant effect during replication
                    if (lab.replicating && lab.speed > 0)
                    {
                        double ratio = (double)lab.speedOverride / lab.speed;
                        lab.speedOverride = (int)(ratio * multiplier * baseSpeed);
                    }

                    lab.speed = multiplier * baseSpeed;
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
            if (speed > 10) speed = 10;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpgradeBeltComponent))]
        public static void UpgradeBelt_Prefix(ref int speed)
        {
            // Note: The game's UpgradeBeltComponent reads the base speed and passes it in.
            // We only need to multiply it here.
            speed *= PluginConfig.beltMultiplier;
            if (speed > 10) speed = 10;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
        public static void StationComponent_InternalTickLocal_Prefix(ref int droneCarries)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                droneCarries *= PluginConfig.beltMultiplier;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickRemote))]
        public static void StationComponent_InternalTickRemote_Prefix(ref int shipCarries)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                shipCarries *= PluginConfig.beltMultiplier;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateCollection))]
        public static void StationComponent_UpdateCollection_Prefix(StationComponent __instance, ref float collectSpeedRate)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                // Ensure the base rate isn't negative (can happen if energy costs > gas giant heat in vanilla formula)
                float baseRate = collectSpeedRate < 0 ? 0 : collectSpeedRate;
                
                float multi = PluginConfig.miningMultiplier.Value;
                float targetRate = baseRate * multi;

                if (__instance.isCollector && __instance.collectionPerTick != null)
                {
                    float maxCollectionPerTick = 0f;
                    for (int i = 0; i < __instance.collectionPerTick.Length; i++)
                    {
                        if (__instance.collectionPerTick[i] > maxCollectionPerTick) 
                            maxCollectionPerTick = __instance.collectionPerTick[i];
                    }

                    if (maxCollectionPerTick > 0.0001f)
                    {
                        // Cap the rate so that we produce at most ~1000 items per tick.
                        // This fills the station in 10-50 ticks but prevents 
                        // statistics register overflows (negative production numbers).
                        float limitRate = 1000f / maxCollectionPerTick;
                        if (targetRate > limitRate)
                        {
                            // Never cap below the speed provided by vanilla tech
                            targetRate = Math.Max(baseRate, limitRate);
                        }
                    }

                    // Absolute safety cap for collectors to prevent float->int cast overflow (2.1B)
                    if (targetRate > 1000000f) targetRate = 1000000f;
                }

                collectSpeedRate = targetRate;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateOutputSlots))]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateInputSlots))]
        public static IEnumerable<CodeInstruction> StationComponent_UpdateSlots_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                // Look for 'local.counter = 1' which is 'ldc.i4.1' followed by 'stfld SlotData.counter'
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Stfld && 
                    codes[i + 1].operand.ToString().Contains("counter"))
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetSlotCounterValue)));
                }
            }
            return codes;
        }

        public static int GetSlotCounterValue()
        {
            return PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1 ? 0 : 1;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static void FractionatorComponent_InternalUpdate_Prefix(ref FractionatorComponent __instance, PlanetFactory factory)
        {
            if (!PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return;

            int multi = PluginConfig.beltMultiplier;
            var traffic = factory.cargoTraffic;
            
            // Loop belt picking logic to fill internal buffer faster
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.fluidInputCount >= __instance.fluidInputMax) break;

                if (__instance.belt1 > 0 && !__instance.isOutput1)
                {
                    if (__instance.fluidId > 0)
                    {
                        if (traffic.TryPickItemAtRear(__instance.belt1, __instance.fluidId, null, out byte stack, out byte inc) > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                        }
                    }
                    else
                    {
                        int needId = traffic.TryPickItemAtRear(__instance.belt1, 0, RecipeProto.fractionatorNeeds, out byte stack, out byte inc);
                        if (needId > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                            __instance.SetRecipe(needId, factory.entitySignPool);
                        }
                    }
                }
                
                if (__instance.belt2 > 0 && !__instance.isOutput2 && __instance.fluidInputCount < __instance.fluidInputMax)
                {
                    if (__instance.fluidId > 0)
                    {
                        if (traffic.TryPickItemAtRear(__instance.belt2, __instance.fluidId, null, out byte stack, out byte inc) > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                        }
                    }
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static IEnumerable<CodeInstruction> Fractionator_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                // 1. Look for ldc.r8 30.0 which is the hardcoded limit for fractionation speed
                if (codes[i].opcode == OpCodes.Ldc_R8 && (double)codes[i].operand == 30.0)
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetFractionatorLimit)));
                }

                // 2. Patch Deuterium output stack size (ldc.i4.1 before TryInsertItemAtHead)
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 2 < codes.Count && 
                    codes[i + 1].opcode == OpCodes.Ldc_I4_0 && 
                    codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand.ToString().Contains("TryInsertItemAtHead"))
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetBeltMultiplier)));
                }

                // 3. Patch Hydrogen output stack size (ldc.i4.1 before TryUpdateItemAtHeadAndFillBlank)
                // Note: It's usually ldc.i4.1 followed by a ldloc for 'inc'
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 2 < codes.Count && 
                    codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand.ToString().Contains("TryUpdateItemAtHeadAndFillBlank"))
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AssemblerPatcher), nameof(GetBeltMultiplier)));
                }
            }
            return codes;
        }

        public static double GetFractionatorLimit()
        {
            return PluginConfig.multiplierEnabled.Value ? PluginConfig.beltMultiplier * 30.0 : 30.0;
        }

        public static int GetBeltMultiplier()
        {
            return PluginConfig.multiplierEnabled.Value ? PluginConfig.beltMultiplier : 1;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static void Fractionator_InternalUpdate_Postfix(ref FractionatorComponent __instance, PlanetFactory factory)
        {
            if (!PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return;

            var traffic = factory.cargoTraffic;
            int multi = PluginConfig.beltMultiplier;

            // Unload Deuterium faster by attempting multiple insertions if backup occurs
            // Note: This helps if there are gaps, but for full speed we rely on stack output in the transpiler
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.productOutputCount <= 0) break;
                if (__instance.belt0 > 0 && __instance.isOutput0)
                {
                    if (traffic.TryInsertItemAtHead(__instance.belt0, __instance.productId, (byte)1, (byte)0))
                        __instance.productOutputCount--;
                    else break;
                }
                else break;
            }

            // Unload remaining Hydrogen faster
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.fluidOutputCount <= 0) break;
                int bId = __instance.belt1 > 0 && __instance.isOutput1 ? __instance.belt1 : (__instance.belt2 > 0 && __instance.isOutput2 ? __instance.belt2 : 0);
                if (bId == 0) break;

                var cp = traffic.GetCargoPath(traffic.beltPool[bId].segPathId);
                if (cp == null) break;

                int inc = __instance.fluidOutputInc / __instance.fluidOutputCount;
                if (cp.TryUpdateItemAtHeadAndFillBlank(__instance.fluidId, 4, (byte)1, (byte)inc))
                {
                    __instance.fluidOutputCount--;
                    __instance.fluidOutputInc -= inc;
                }
                else break;
            }
        }

        // =================================================================
        // REVERSE PATCHES FOR SAFE LOOPING
        // =================================================================

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpdateSplitter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallOriginalUpdateSplitter(CargoTraffic instance, ref SplitterComponent sp)
        {
            // This method is replaced by Harmony with the original method IL
            throw new NotImplementedException("It's a stub");
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PilerComponent), nameof(PilerComponent.InternalUpdate))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallOriginalPilerUpdate(ref PilerComponent instance, CargoTraffic _traffic, AnimData[] _animPool)
        {
            // This method is replaced by Harmony with the original method IL
            throw new NotImplementedException("It's a stub");
        }

        // =================================================================
        // LOOPING POSTFIXES
        // =================================================================

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpdateSplitter))]
        public static void UpdateSplitter_Postfix(ref SplitterComponent sp, CargoTraffic __instance)
        {
            if (PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1)
            {
                int multi = PluginConfig.beltMultiplier;
                // Run (multi - 1) additional updates
                for (int i = 0; i < multi - 1; i++)
                {
                    CallOriginalUpdateSplitter(__instance, ref sp);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PilerComponent), nameof(PilerComponent.InternalUpdate))]
        public static void PilerComponent_InternalUpdate_Postfix(ref PilerComponent __instance, CargoTraffic _traffic, AnimData[] _animPool)
        {
            if (PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1)
            {
                // Force cooldown to zero to allow operation
                __instance.cacheCdTick = 0;

                int multi = PluginConfig.beltMultiplier;
                
                // Ensure piler has enough timeSpend to operate multiple times if needed
                // We add extra timeSpend for the additional loops
                if (__instance.timeSpend < 10000)
                {
                    __instance.timeSpend = 10000;
                }

                // Run (multi - 1) additional updates
                for (int i = 0; i < multi - 1; i++)
                {
                    CallOriginalPilerUpdate(ref __instance, _traffic, _animPool);
                    // Reset cooldown between extra ticks too, just in case
                    if (__instance.cacheCdTick > 0) __instance.cacheCdTick = 0;
                }
            }
        }
        
        private static void MultiplyFractionators(FactorySystem factorySystem)
        {
            for (int index = 1; index < factorySystem.fractionatorCursor; ++index)
            {
                if (factorySystem.fractionatorPool[index].id == index)
                    factorySystem.fractionatorPool[index].produceProb = PluginConfig.fractionatorMultiplier * 0.01f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void MultiplyLabResearch(ref LabComponent __instance, ref float research_speed)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                if (__instance.speed > 0)
                {
                    research_speed *= (float)__instance.speed / 10000f;
                }
                else
                {
                    research_speed *= PluginConfig.labMultiplier;
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        public static void EjectorComponent_InternalUpdate_Prefix(ref EjectorComponent __instance)
        {
            var ejectorProto = ItemUtil.EjectorProto;
            __instance.chargeSpend = ejectorProto.prefabDesc.ejectorChargeFrame * 10000 / PluginConfig.ejectorMultiplier;
            __instance.coldSpend = ejectorProto.prefabDesc.ejectorColdFrame * 10000 / PluginConfig.ejectorMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SiloComponent), "InternalUpdate")]
        public static void SiloComponent_InternalUpdate_Prefix(ref SiloComponent __instance)
        {
            var siloProto = ItemUtil.SiloProto;
            __instance.chargeSpend = siloProto.prefabDesc.siloChargeFrame * 10000 / PluginConfig.siloMultiplier;
            __instance.coldSpend = siloProto.prefabDesc.siloColdFrame * 10000 / PluginConfig.siloMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate_Bidirectional))]
        public static void InserterComponent_InternalUpdate_Prefix(ref InserterComponent __instance, ref float power, PlanetFactory factory)
        {
            if (__instance.id == 0 || __instance.entityId == 0)
                return;
            
            int multi = PluginConfig.inserterMultiplier;
            if (multi <= 1) return;

            // Multiply power to speed up progress and animations
            power *= multi;

            // Overclock delay (wait time at picking/inserting)
            int protoId = factory.entityPool[__instance.entityId].protoId;
            if (!_inserterDelayByProtoId.TryGetValue(protoId, out int baseDelay))
            {
                ItemProto inserterProto = LDB.items.Select(protoId);
                baseDelay = _inserterDelayByProtoId[protoId] = inserterProto?.prefabDesc?.inserterDelay ?? 0;
            }
            
            if (baseDelay > 0)
            {
                __instance.delay = baseDelay / multi;
            }

            // Fixed progress increment in Picking stage for non-bidirectional sorters
            if (!__instance.bidirectional && __instance.stage == EInserterStage.Picking && __instance.itemId > 0)
            {
                // The original code will add 10000. We want it to add multi * 10000.
                __instance.time += 10000 * (multi - 1);
            }
        }

        // =================================================================
        // INSERTER TRANSPILERS
        // For bidirectional sorters, we need to increase transfer count per tick.
        // =================================================================

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate_Bidirectional))]
        public static IEnumerable<CodeInstruction> InserterComponent_InternalUpdate_TranspilerBidirectional(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                // We target the initialization of 'num1' and 'num6' which are 'ldc.i4.1' followed by 'stloc'
                // This is safer than replacing every '1' in the method.
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 1 < codes.Count && 
                    (codes[i + 1].opcode == OpCodes.Stloc_S || codes[i + 1].opcode == OpCodes.Stloc_0 || 
                     codes[i + 1].opcode == OpCodes.Stloc_1 || codes[i + 1].opcode == OpCodes.Stloc_2 || 
                     codes[i + 1].opcode == OpCodes.Stloc_3 || codes[i + 1].opcode == OpCodes.Stloc))
                {
                    // Preservation of labels is CRITICAL to avoid "Label not marked" crashes
                    var newInst = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PluginConfig), "get_inserterMultiplier"));
                    newInst.labels = codes[i].labels;
                    codes[i] = newInst;
                }
            }
            return codes;
        }
    }
}