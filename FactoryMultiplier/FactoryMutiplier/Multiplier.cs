using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace Multiplier
{
    [BepInPlugin("com.Valoneu.Multiplier", "Multiplier", "2.0.0")]
    public class Multiplier : BaseUnityPlugin
    {
        private static ConfigEntry<int> walkspeed_set;
        private static ConfigEntry<int> walkspeedMultiply;
        private static ConfigEntry<int> miningMultiply;
        private static ConfigEntry<int> smeltMultiply;
        private static ConfigEntry<int> chemicalMultiply;
        private static ConfigEntry<int> refineMultiply;
        private static ConfigEntry<int> assembleMultiply;
        private static ConfigEntry<int> particleMultiply;
        private static ConfigEntry<int> labMultiply;
        private static ConfigEntry<int> fractionateMultiply;
        private static ConfigEntry<int> ejectorMultiply;
        private static ConfigEntry<int> siloMultiply;
        private static ConfigEntry<int> gamaMultiply;
        private static ManualLogSource logger;


        internal void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }

        public void Awake()
        {
            _harmony = new Harmony("com.Valoneu.Multiplier");
            logger = Logger;
            _harmony.PatchAll(typeof(Multiplier));
            walkspeedMultiply = Config.Bind("config", "walkspeedMultiply", 1, "Multiplies walking speed");
            miningMultiply = Config.Bind("config", "miningMultiply", 1, "Multiplies speed of miners");
            smeltMultiply = Config.Bind("config", "smeltMultiply", 1, "Multiplies speed of smelters");
            chemicalMultiply = Config.Bind("config", "chemicalMultiply", 1, "Multiplies speed of chemical plants");
            refineMultiply = Config.Bind("config", "refineMultiply", 1, "Multiplies speed of refineries");
            assembleMultiply = Config.Bind("config", "assembleMultiply", 1, "Multiplies speed of assemblers");
            particleMultiply = Config.Bind("config", "particleMultiply", 1, "Multiplies speed of particle colliders");
            labMultiply = Config.Bind("config", "labMultiply", 1, "Multiplies speed of laboratories");
            fractionateMultiply = Config.Bind("config", "fractionateMultiply", 1, "Multiplies % of fractionators");
            ejectorMultiply = Config.Bind("config", "ejectorMultiply", 1, "Multiplies speed of EM rail ejectors");
            siloMultiply = Config.Bind("config", "siloMultiply", 1, "Multiplies speed of silos");
            gamaMultiply = Config.Bind("config", "gamaMultiply", 1, "Multiplies speed of ray recievers");
            logger.LogInfo($"Plugin com.Valoneu.Multiplier is loaded!");
        }

        private static readonly Dictionary<string, DateTime> _lastLogTime = new Dictionary<string, DateTime>();
        private Harmony _harmony;

        private enum Level
        {
            Debug,
            Info,
            Warn
        }

        private static void LogWithFrequency(Level level, string msgTemplate, params object[] args)
        {
            if (!_lastLogTime.TryGetValue(msgTemplate, out var lastTime))
            {
                lastTime = DateTime.Now.Subtract(TimeSpan.FromMinutes(3));
            }

            try
            {
                var msg = string.Format(msgTemplate, args);
                if ((DateTime.Now - lastTime).TotalMinutes < 1)
                {
                    return;
                }

                _lastLogTime[msgTemplate] = DateTime.Now;
                switch (level)
                {
                    case Level.Debug:
                        logger.LogDebug(msg);
                        break;
                    case Level.Info:
                        logger.LogInfo(msg);
                        break;
                    case Level.Warn:
                        logger.LogWarning(msg);
                        break;
                    default:
                        logger.LogInfo(msg);
                        break;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning($"exception with log message: {e.Message}\r\n {e}\r\n{e.StackTrace}\r\n{msgTemplate}");
            }
        }

        // instead of adding the same patch multiple times we'll just call our other functions from here
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        public static bool FactorySystem_GameTick_Prefix(FactorySystem __instance)
        {
            try
            {
                MultiplyAssemblerPoolMachines(__instance);
                LogWithFrequency(Level.Info, "MultiplyAssemblerPoolMachines ran successfully");
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, $"assemble patch got exception : {e.Message}");
            }


            try
            {
                MultiplyLabs(__instance);
                LogWithFrequency(Level.Info, "Labs ran");
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "Labs exception {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                Fractionate_patch(__instance);
                LogWithFrequency(Level.Info, "fract ran");
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "fract got exception : {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                Silo_patch(__instance);
                LogWithFrequency(Level.Info, "Silo ran");
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "silo got exception : {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                Ejector_patch(__instance);
                LogWithFrequency(Level.Info, "ejector ran");
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "ejector got exception : {0}, {1}", e.Message, e.StackTrace);
            }


            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void MechawalkingSpeed_get(GameHistoryData __instance)
        {
            for (var index = 8; index > 0; --index)
            {
                if (!__instance.techStates[2201].unlocked)
                {
                    walkspeed_set.Value = 0;
                    break;
                }

                if (__instance.techStates[2200 + index].unlocked)
                {
                    walkspeed_set.Value = index;
                    break;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), "Import")]
        public static void MechawalkingSpeed_patch(Mecha __instance)
        {
            if (walkspeed_set.Value == 0)
            {
                __instance.walkSpeed = Configs.freeMode.mechaWalkSpeed * walkspeedMultiply.Value;
            }
            else if (walkspeed_set.Value >= 7)
            {
                __instance.walkSpeed = (float)(Configs.freeMode.mechaWalkSpeed + (double)((walkspeed_set.Value - 6) * 2) + 6.0) * walkspeedMultiply.Value;
            }
            else
            {
                if (walkspeed_set.Value >= 7)
                {
                    return;
                }

                __instance.walkSpeed = (Configs.freeMode.mechaWalkSpeed + walkspeed_set.Value) * walkspeedMultiply.Value;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void MiningSpeedScale_patch(GameHistoryData __instance)
        {
            for (var index = 4; index > 0; --index)
            {
                if (__instance.techStates[3605].unlocked)
                {
                    __instance.miningSpeedScale = ((__instance.techStates[3606].curLevel - 1) / 10f + Configs.freeMode.miningSpeedScale) * miningMultiply.Value;
                    break;
                }

                if (!__instance.techStates[3601].unlocked)
                {
                    __instance.miningSpeedScale = Configs.freeMode.miningSpeedScale * miningMultiply.Value;
                    break;
                }

                if (__instance.techStates[3600 + index].unlocked)
                {
                    __instance.miningSpeedScale = (index / 10f + Configs.freeMode.miningSpeedScale) * miningMultiply.Value;
                    break;
                }
            }
        }

        public static void MultiplyAssemblerPoolMachines(FactorySystem __instance)
        {
            for (var index = 1; index < __instance.assemblerCursor; ++index)
            {
                var assembler = __instance.assemblerPool[index];
                var assemblerEntityId = assembler.entityId;
                if (assemblerEntityId > 0)
                {
                    var itemProto = LDB.items.Select(__instance.factory.entityPool[assemblerEntityId].protoId);
                    switch (itemProto.prefabDesc.assemblerRecipeType)
                    {
                        case ERecipeType.Assemble:
                            assembler.speed = assembleMultiply.Value * itemProto.prefabDesc.assemblerSpeed;
                            break;
                        case ERecipeType.Chemical:
                            LogWithFrequency(Level.Debug, "changing chem planet speed to {0}  {1}", assembler.speed,
                                chemicalMultiply.Value * itemProto.prefabDesc.assemblerSpeed);
                            assembler.speed = chemicalMultiply.Value * itemProto.prefabDesc.assemblerSpeed;
                            break;
                        case ERecipeType.Exchange: // not multiplied
                            break;
                        case ERecipeType.Fractionate:
                            logger.LogWarning($"unexpectedly found assembler pool component with recipe set to fract");
                            break;
                        case ERecipeType.Particle:
                            assembler.speed = particleMultiply.Value * itemProto.prefabDesc.assemblerSpeed;
                            break;
                        case ERecipeType.Refine:

                            assembler.speed = refineMultiply.Value * itemProto.prefabDesc.assemblerSpeed;
                            break;
                        case ERecipeType.Research:

                            var labComponent = __instance.labPool[index];
                            var recipeProto = LDB.recipes.Select(labComponent.recipeId);
                            labComponent.timeSpend = recipeProto.TimeSpend * 10000 / labMultiply.Value;
                            break;
                        case ERecipeType.Smelt:
                            __instance.assemblerPool[index].speed = smeltMultiply.Value * itemProto.prefabDesc.assemblerSpeed;
                            break;
                        case ERecipeType.PhotonStore: // not multiplied
                            break;
                        default:
                            LogWithFrequency(Level.Warn, $"Unhandled case in assembler pool processing {itemProto.prefabDesc.assemblerRecipeType}");
                            break;
                    }
                }
            }
        }

        private static void MultiplyLabs(FactorySystem factorySystem)
        {
            for (var index = 1; index < factorySystem.labCursor; ++index)
            {
                if (factorySystem.labPool[index].recipeId > 0)
                {
                    var recipeProto = LDB.recipes.Select(factorySystem.labPool[index].recipeId);
                    if (recipeProto != null)
                    {
                        factorySystem.labPool[index].timeSpend = recipeProto.TimeSpend * 10000 / labMultiply.Value;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void Techspeed_patch(GameHistoryData __instance)
        {
            for (var index = 2; index > 0; --index)
            {
                logger.LogDebug("TechSpeedChanged");
                if (__instance.techStates[3903].unlocked)
                {
                    __instance.techSpeed = __instance.techStates[3904].curLevel * labMultiply.Value;
                    break;
                }

                if (!__instance.techStates[3901].unlocked)
                {
                    __instance.techSpeed = Configs.freeMode.techSpeed * labMultiply.Value;
                    break;
                }

                if (__instance.techStates[3900 + index].unlocked)
                {
                    __instance.techSpeed = (index + Configs.freeMode.techSpeed) * labMultiply.Value;
                    break;
                }
            }
        }

        public static void Fractionate_patch(FactorySystem __instance)
        {
            for (var index = 1; index < __instance.fractionateCursor; ++index)
            {
                if (__instance.fractionatePool[index].id == index)
                {
                    __instance.fractionatePool[index].produceProb = fractionateMultiply.Value * 0.01f;
                }
            }
        }

        private static void Ejector_patch(FactorySystem factorySystem)
        {
            for (var index = 1; index < factorySystem.ejectorCursor; ++index)
            {
                if (factorySystem.ejectorPool[index].id == index)
                {
                    var ejectorProto = LDB.items.Select(factorySystem.factory.entityPool[factorySystem.ejectorPool[index].entityId].protoId);
                    factorySystem.ejectorPool[index].chargeSpend = ejectorProto.prefabDesc.ejectorChargeFrame * 10000 / ejectorMultiply.Value;
                    factorySystem.ejectorPool[index].coldSpend = ejectorProto.prefabDesc.ejectorColdFrame * 10000 / ejectorMultiply.Value;
                }
            }
        }

        private static void Silo_patch(FactorySystem factory)
        {
            for (var index = 1; index < factory.siloCursor; ++index)
            {
                if (factory.siloPool[index].id == index)
                {
                    var siloProto = LDB.items.Select(factory.factory.entityPool[factory.siloPool[index].entityId].protoId);
                    factory.siloPool[index].chargeSpend = siloProto.prefabDesc.siloChargeFrame * 10000 / siloMultiply.Value;
                    factory.siloPool[index].coldSpend = siloProto.prefabDesc.siloColdFrame * 10000 / siloMultiply.Value;
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        [HarmonyPatch(typeof(PowerSystem), "GameTick", typeof(long), typeof(bool), typeof(bool))]
        public static void PowerSystem_GameTick_Prefix(PowerSystem __instance)
        {
            try
            {
                LogWithFrequency(Level.Info, "Running MultiplyPowerConsumption");
                MultiplyPowerConsumption(__instance);
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "Multiply power failed. {0}, {1}", e.Message, e.StackTrace);
            }
            try
            {
                LogWithFrequency(Level.Info, "Running MultiplyGamma");
                MultiplyGamma(__instance);
            }
            catch (Exception e)
            {
                LogWithFrequency(Level.Warn, "Multiply gamma failed. {0}, {1}", e.Message, e.StackTrace);
            }
        }

        public static void MultiplyGamma(PowerSystem powerSystem)
        {
            var itemProto = LDB.items.Select(2208);
            for (var index = 1; index < powerSystem.genCursor; ++index)
            {
                if (powerSystem.genPool[index].id == index && powerSystem.genPool[index].gamma)
                {
                    powerSystem.genPool[index].genEnergyPerTick = gamaMultiply.Value * itemProto.prefabDesc.genEnergyPerTick;
                }
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

                    if (itemProto?.prefabDesc == null || itemProto.prefabDesc.assemblerRecipeType == ERecipeType.None)
                    {
                        LogWithFrequency(Level.Debug, itemProto == null ? $"itemproto == null " : $"prefab desc == null {itemProto.prefabDesc == null}");
                        continue;
                    }

                    var prefabEnergyPerTick = itemProto.prefabDesc.workEnergyPerTick;
                    var idleEnergyPerTick = itemProto.prefabDesc.idleEnergyPerTick;
                    long multiplierByType = 1;
                    LogWithFrequency(Level.Info, "Running power multiplier for recipetype {0}", itemProto.prefabDesc.assemblerRecipeType);
                    switch (itemProto.prefabDesc.assemblerRecipeType)
                    {
                        case ERecipeType.Assemble:
                            multiplierByType = assembleMultiply.Value;
                            break;
                        case ERecipeType.Chemical:
                            multiplierByType = chemicalMultiply.Value;
                            break;
                        case ERecipeType.Exchange: // not multiplied
                            break;
                        case ERecipeType.Fractionate:
                            multiplierByType = 1;
                            powerConsumerComponent.workEnergyPerTick =
                                (long)(Math.Pow(1.055, fractionateMultiply.Value) * fractionateMultiply.Value * prefabEnergyPerTick);
                            break;
                        case ERecipeType.Particle:
                            multiplierByType = particleMultiply.Value;
                            break;
                        case ERecipeType.Refine:
                            multiplierByType = refineMultiply.Value;
                            break;
                        case ERecipeType.Research:
                            multiplierByType = labMultiply.Value;
                            break;
                        case ERecipeType.Smelt:
                            multiplierByType = smeltMultiply.Value;
                            break;
                        case ERecipeType.PhotonStore: // not multiplied
                            break;
                        default:
                            LogWithFrequency(Level.Warn, "Unhandled recipe type in power consumption: {0}", itemProto.prefabDesc.assemblerRecipeType);
                            break;
                    }

                    powerConsumerComponent.workEnergyPerTick = multiplierByType * prefabEnergyPerTick;
                    powerConsumerComponent.idleEnergyPerTick = multiplierByType * idleEnergyPerTick;
                    
                }
            }
        }
    }
}