using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Multiplier
{
    [BepInPlugin("com.Valoneu.Multiplier", "Multiplier", "1.0.0")]
    public class Multiplier : BaseUnityPlugin
    {
        private static int walkspeed_set;
        private static int walkspeedMultiply;
        private static int miningMultiply;
        private static int smeltMultiply;
        private static int chemicalMultiply;
        private static int refineMultiply;
        private static int assembleMultiply;
        private static int particleMultiply;
        private static int labMultiply;
        private static int fractionateMultiply;
        private static int ejectorMultiply;
        private static int siloMultiply;
        private static int gamaMultiply;

        public void Start()
        {
            Harmony.CreateAndPatchAll(typeof(Multiplier));
            walkspeedMultiply = Config.Bind("config", "walkspeedMultiply", 1, "Multiplies walking speed").Value;
            miningMultiply = Config.Bind("config", "miningMultiply", 1, "Multiplies speed of miners").Value;
            smeltMultiply = Config.Bind("config", "smeltMultiply", 1, "Multiplies speed of smelters").Value;
            chemicalMultiply = Config.Bind("config", "chemicalMultiply", 1, "Multiplies speed of chemical plants").Value;
            refineMultiply = Config.Bind("config", "refineMultiply", 1, "Multiplies speed of refineries").Value;
            assembleMultiply = Config.Bind("config", "assembleMultiply", 1, "Multiplies speed of assemblers").Value;
            particleMultiply = Config.Bind("config", "particleMultiply", 1, "Multiplies speed of particle colliders").Value;
            labMultiply = Config.Bind("config", "labMultiply", 1, "Multiplies speed of laboratories").Value;
            fractionateMultiply = Config.Bind("config", "fractionateMultiply", 1, "Multiplies % of fractionators").Value;
            ejectorMultiply = Config.Bind("config", "ejectorMultiply", 1, "Multiplies speed of EM rail ejectors").Value;
            siloMultiply = Config.Bind("config", "siloMultiply", 1, "Multiplies speed of silos").Value;
            gamaMultiply = Config.Bind("config", "gamaMultiply", 1, "Multiplies speed of ray recievers").Value;
        }

        private static int countDown = 100;

        // instead of adding the same patch multiple times we'll just call our other functions from here
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool))]
        // [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        public static bool FactorySystem_GameTick_Prefix(FactorySystem __instance)
        {
            if (countDown-- > 0)
            {
                Console.WriteLine($"Calling other functions for FactorySystem_GameTick_Prefix");
                try
                {
                    Smelt_patch(__instance);
                    Console.WriteLine($"called smelt");
                }
                catch (Exception e)
                {
                    Debug.Log($"smelt patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Chemical_patch(__instance);
                    Console.WriteLine($"called chem");
                }
                catch (Exception e)
                {
                    Debug.Log($"chemical patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Refine_patch(__instance);
                    Console.WriteLine($"called refine");
                }
                catch (Exception e)
                {
                    Debug.Log($"refine patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Assemble_patch(__instance);
                    Console.WriteLine($"called assemble");
                }
                catch (Exception e)
                {
                    Debug.Log($"assemble patch got exception : {e.Message}");
                    
                    countDown++;
                }

                try
                {
                    Particle_patch(__instance);
                    Console.WriteLine($"called particle");
                }
                catch (Exception e)
                {
                    Debug.Log($"particle patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Lab_patch(__instance);
                    Console.WriteLine($"called lab");
                }
                catch (Exception e)
                {
                    Debug.Log($"lab patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Fractionate_patch(__instance);
                    Console.WriteLine($"called fract");
                }
                catch (Exception e)
                {
                    Debug.Log($"fract patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Silo_patch(__instance);
                    Console.WriteLine($"called silo");
                }
                catch (Exception e)
                {
                    Debug.Log($"Silo_patch got exception : {e.Message}");
                    countDown++;
                }

                try
                {
                    Ejector_patch(__instance);
                    Console.WriteLine($"called ejector");
                }
                catch (Exception e)
                {
                    Debug.Log($"ejector patch got exception : {e.Message}");
                    countDown++;
                }
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void MechawalkingSpeed_get(GameHistoryData __instance)
        {
            for (int index = 8; index > 0; --index)
            {
                if (!__instance.techStates[2201].unlocked)
                {
                    walkspeed_set = 0;
                    break;
                }

                if (__instance.techStates[2200 + index].unlocked)
                {
                    walkspeed_set = index;
                    break;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), "Import")]
        public static void MechawalkingSpeed_patch(Mecha __instance)
        {
            if (walkspeed_set == 0)
                __instance.walkSpeed = Configs.freeMode.mechaWalkSpeed * walkspeedMultiply;
            else if (walkspeed_set >= 7)
            {
                __instance.walkSpeed = (float)(Configs.freeMode.mechaWalkSpeed + (double)((walkspeed_set - 6) * 2) + 6.0) * walkspeedMultiply;
            }
            else
            {
                if (walkspeed_set >= 7)
                    return;
                __instance.walkSpeed = (Configs.freeMode.mechaWalkSpeed + walkspeed_set) * walkspeedMultiply;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void MiningSpeedScale_patch(GameHistoryData __instance)
        {
            for (int index = 4; index > 0; --index)
            {
                if (__instance.techStates[3605].unlocked)
                {
                    __instance.miningSpeedScale = ((float)(__instance.techStates[3606].curLevel - 1) / 10f + Configs.freeMode.miningSpeedScale) * (float)Multiplier.miningMultiply;
                    break;
                }
                if (!__instance.techStates[3601].unlocked)
                {
                    __instance.miningSpeedScale = Configs.freeMode.miningSpeedScale * (float)Multiplier.miningMultiply;
                    break;
                }
                if (__instance.techStates[3600 + index].unlocked)
                {
                    __instance.miningSpeedScale = ((float)index / 10f + Configs.freeMode.miningSpeedScale) * (float)Multiplier.miningMultiply;
                    break;
                }
            }
        }

        public static void Smelt_patch(FactorySystem __instance)
        {
            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2302);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(2315);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(3700);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
                    UnityEngine.Debug.Log("SmelterSpeedChanged");
                    ItemProto itemProto4 = ((ProtoSet<ItemProto>)LDB.items).Select((int)__instance.factory.entityPool[entityId].protoId);
                    if (((Proto)itemProto4).ID == ((Proto)itemProto1).ID)
                        __instance.assemblerPool[index].speed = Multiplier.smeltMultiply * itemProto1.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto2).ID)
                        __instance.assemblerPool[index].speed = Multiplier.smeltMultiply * itemProto2.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto3).ID)
                        __instance.assemblerPool[index].speed = Multiplier.smeltMultiply * itemProto2.prefabDesc.assemblerSpeed;
                }
            }
        }

        public static void Chemical_patch(FactorySystem __instance)
        {
            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2309);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(3701);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(3702);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
                    UnityEngine.Debug.Log("ChemicalSpeedChanged");
                    ItemProto itemProto4 = ((ProtoSet<ItemProto>)LDB.items).Select((int)__instance.factory.entityPool[entityId].protoId);
                    if (((Proto)itemProto4).ID == ((Proto)itemProto1).ID)
                        __instance.assemblerPool[index].speed = Multiplier.chemicalMultiply * itemProto1.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto2).ID)
                        __instance.assemblerPool[index].speed = Multiplier.chemicalMultiply * itemProto2.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto3).ID)
                        __instance.assemblerPool[index].speed = Multiplier.chemicalMultiply * itemProto3.prefabDesc.assemblerSpeed;
                }
            }
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        public static void Refine_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2308);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                UnityEngine.Debug.Log("RefinerySpeedChanged");
                if (__instance.assemblerPool[index].id == index && (int)__instance.assemblerPool[index].recipeType == 3)
                    __instance.assemblerPool[index].speed = Multiplier.refineMultiply * itemProto.prefabDesc.assemblerSpeed;
            }
        }

        public static void Assemble_patch(FactorySystem __instance)
        {
            if (countDown-- > 0)
            {
                Console.WriteLine("***************************");
                Console.WriteLine("assemble patch called");
                Console.WriteLine("***************************");
            }

            ItemProto itemProto1 = LDB.items.Select(2303);
            ItemProto itemProto2 = LDB.items.Select(2304);
            ItemProto itemProto3 = LDB.items.Select(2305);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
                    Debug.Log("AssemblerSpeedChanged");
                    ItemProto itemProto4 = LDB.items.Select(__instance.factory.entityPool[entityId].protoId);
                    if (itemProto4.ID == itemProto1.ID)
                        __instance.assemblerPool[index].speed = assembleMultiply * itemProto1.prefabDesc.assemblerSpeed;
                    else if (itemProto4.ID == itemProto2.ID)
                        __instance.assemblerPool[index].speed = assembleMultiply * itemProto2.prefabDesc.assemblerSpeed;
                    else if (itemProto4.ID == itemProto3.ID)
                        __instance.assemblerPool[index].speed = assembleMultiply * itemProto3.prefabDesc.assemblerSpeed;
                }
            }
        }

        public static void Particle_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2310);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                UnityEngine.Debug.Log("ColliderSpeedChanged");
                if (__instance.assemblerPool[index].id == index && (int)__instance.assemblerPool[index].recipeType == 5)
                    __instance.assemblerPool[index].speed = Multiplier.particleMultiply * itemProto.prefabDesc.assemblerSpeed;
            }
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(FactorySystem), "GameTick", typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int))]
        public static void Lab_patch(FactorySystem __instance)
        {
            RecipeProto recipeProto1 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(9);
            RecipeProto recipeProto2 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(18);
            RecipeProto recipeProto3 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(27);
            RecipeProto recipeProto4 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(55);
            RecipeProto recipeProto5 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(102);
            RecipeProto recipeProto6 = ((ProtoSet<RecipeProto>)LDB.recipes).Select(75);
            for (int index = 1; index < __instance.labCursor; ++index)
            {
                if (__instance.labPool[index].recipeId > 0)
                {
                    UnityEngine.Debug.Log("LabSpeedChanged");
                    if (__instance.labPool[index].recipeId == ((Proto)recipeProto1).ID)
                        __instance.labPool[index].timeSpend = recipeProto1.TimeSpend * 10000 / Multiplier.labMultiply;
                    else if (__instance.labPool[index].recipeId == ((Proto)recipeProto2).ID)
                        __instance.labPool[index].timeSpend = recipeProto2.TimeSpend * 10000 / Multiplier.labMultiply;
                    else if (__instance.labPool[index].recipeId == ((Proto)recipeProto3).ID)
                        __instance.labPool[index].timeSpend = recipeProto3.TimeSpend * 10000 / Multiplier.labMultiply;
                    else if (__instance.labPool[index].recipeId == ((Proto)recipeProto4).ID)
                        __instance.labPool[index].timeSpend = recipeProto4.TimeSpend * 10000 / Multiplier.labMultiply;
                    else if (__instance.labPool[index].recipeId == ((Proto)recipeProto5).ID)
                        __instance.labPool[index].timeSpend = recipeProto5.TimeSpend * 10000 / Multiplier.labMultiply;
                    else if (__instance.labPool[index].recipeId == ((Proto)recipeProto6).ID)
                        __instance.labPool[index].timeSpend = recipeProto6.TimeSpend * 10000 / Multiplier.labMultiply;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        public static void Techspeed_patch(GameHistoryData __instance)
        {
            for (int index = 2; index > 0; --index)
            {
                Debug.Log("TechSpeedChanged");
                if (__instance.techStates[3903].unlocked)
                {
                    __instance.techSpeed = __instance.techStates[3904].curLevel * labMultiply;
                    break;
                }

                if (!__instance.techStates[3901].unlocked)
                {
                    __instance.techSpeed = Configs.freeMode.techSpeed * labMultiply;
                    break;
                }

                if (__instance.techStates[3900 + index].unlocked)
                {
                    __instance.techSpeed = (index + Configs.freeMode.techSpeed) * labMultiply;
                    break;
                }
            }
        }

        public static void Fractionate_patch(FactorySystem __instance)
        {
            for (int index = 1; index < __instance.fractionateCursor; ++index)
            {
                if (__instance.fractionatePool[index].id == index)
                {
                    UnityEngine.Debug.Log("FractionatorSpeedChanged");
                    __instance.fractionatePool[index].produceProb = (float)Multiplier.fractionateMultiply * 0.01f;
                }
            }
        }

        public static void Ejector_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2311);
            for (int index = 1; index < __instance.ejectorCursor; ++index)
            {
                if (__instance.ejectorPool[index].id == index)
                {
                    UnityEngine.Debug.Log("RailSpeedChanged");
                    __instance.ejectorPool[index].chargeSpend = itemProto.prefabDesc.ejectorChargeFrame * 10000 / Multiplier.ejectorMultiply;
                    __instance.ejectorPool[index].coldSpend = itemProto.prefabDesc.ejectorColdFrame * 10000 / Multiplier.ejectorMultiply;
                }
            }
        }

        public static void Silo_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2312);
            for (int index = 1; index < __instance.siloCursor; ++index)
            {
                if (__instance.siloPool[index].id == index)
                {
                    UnityEngine.Debug.Log("SiloSpeedChanged");
                    __instance.siloPool[index].chargeSpend = itemProto.prefabDesc.siloChargeFrame * 10000 / Multiplier.siloMultiply;
                    __instance.siloPool[index].coldSpend = itemProto.prefabDesc.siloColdFrame * 10000 / Multiplier.siloMultiply;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        public static void Gamma_patch(PowerSystem __instance)
        {
            ItemProto itemProto = LDB.items.Select(2208);
            for (int index = 1; index < __instance.genCursor; ++index)
            {
                // Debug.Log("RrSpeedChanged");
                if (__instance.genPool[index].id == index && __instance.genPool[index].gamma)
                {
                    __instance.genPool[index].genEnergyPerTick = gamaMultiply * itemProto.prefabDesc.genEnergyPerTick;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        public static void Power_patch(PowerSystem __instance)
        {
            ItemProto SmelterMK1 = LDB.items.Select(2302);
            ItemProto SmelterMK3 = LDB.items.Select(2315);
            ItemProto SmelterMK2 = LDB.items.Select(3700);
            ItemProto ChemicalMK1 = LDB.items.Select(2309);
            ItemProto ChemicalMK2 = LDB.items.Select(3701);
            ItemProto ChemicalMK3 = LDB.items.Select(3702);
            ItemProto AssemblerMK1 = LDB.items.Select(2303);
            ItemProto AssemblerMK2 = LDB.items.Select(2304);
            ItemProto AssemblerMK3 = LDB.items.Select(2305);
            ItemProto RefineryMK1 = LDB.items.Select(2308);
            ItemProto ColliderMK1 = LDB.items.Select(2310);
            ItemProto FractionatorMK1 = LDB.items.Select(2314);
            ItemProto LabMK1 = LDB.items.Select(2901);
            ItemProto RailMK1 = LDB.items.Select(2311);
            ItemProto SiloMK1 = LDB.items.Select(2312);

            for (int index = 1; index < __instance.consumerCursor; ++index)
            {
                int entityId = __instance.consumerPool[index].entityId;
                if (entityId > 0)
                {
                    ItemProto itemProtoCheck = LDB.items.Select(__instance.factory.entityPool[entityId].protoId);
                    if (SmelterMK1 != null)
                    {
                        if (itemProtoCheck.ID == SmelterMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = smeltMultiply * SmelterMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (SmelterMK3 != null)
                    {
                        if (itemProtoCheck.ID == SmelterMK3.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = smeltMultiply * SmelterMK3.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (SmelterMK2 != null)
                    {
                        if (itemProtoCheck.ID == SmelterMK2.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = smeltMultiply * SmelterMK2.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (ChemicalMK1 != null)
                    {
                        if (itemProtoCheck.ID == ChemicalMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = chemicalMultiply * ChemicalMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (ChemicalMK2 != null)
                    {
                        if (itemProtoCheck.ID == ChemicalMK2.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = chemicalMultiply * ChemicalMK2.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (ChemicalMK3 != null)
                    {
                        if (itemProtoCheck.ID == ChemicalMK3.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = chemicalMultiply * ChemicalMK3.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (AssemblerMK1 != null)
                    {
                        if (itemProtoCheck.ID == AssemblerMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = assembleMultiply * AssemblerMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (AssemblerMK2 != null)
                    {
                        if (itemProtoCheck.ID == AssemblerMK2.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = assembleMultiply * AssemblerMK2.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (AssemblerMK3 != null)
                    {
                        if (itemProtoCheck.ID == AssemblerMK3.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = assembleMultiply * AssemblerMK3.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (RefineryMK1 != null)
                    {
                        if (itemProtoCheck.ID == RefineryMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = refineMultiply * RefineryMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (ColliderMK1 != null)
                    {
                        if (itemProtoCheck.ID == ColliderMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = refineMultiply * ColliderMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (LabMK1 != null)
                    {
                        if (itemProtoCheck.ID == LabMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = refineMultiply * LabMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (RailMK1 != null)
                    {
                        if (itemProtoCheck.ID == RailMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = ejectorMultiply * RailMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (SiloMK1 != null)
                    {
                        if (itemProtoCheck.ID == SiloMK1.ID)
                        {
                            __instance.consumerPool[index].workEnergyPerTick = siloMultiply * SiloMK1.prefabDesc.workEnergyPerTick;
                        }
                    }

                    if (FractionatorMK1 != null)
                        if (itemProtoCheck.ID == FractionatorMK1.ID)
                        {
                            if (fractionateMultiply == 1)
                            {
                                __instance.consumerPool[index].workEnergyPerTick = FractionatorMK1.prefabDesc.workEnergyPerTick;
                            }
                            else if (fractionateMultiply != 1)
                            {
                                __instance.consumerPool[index].workEnergyPerTick =
                                    (long)(Math.Pow(1.055, fractionateMultiply) * fractionateMultiply * FractionatorMK1.prefabDesc.workEnergyPerTick);
                            }
                        }
                }
            }
        }
    }
}