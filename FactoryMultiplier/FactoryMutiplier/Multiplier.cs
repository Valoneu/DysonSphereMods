// Decompiled with JetBrains decompiler
// Type: Multiplier.Multiplier
// Assembly: FactoryMutiplier, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AFE8885A-2900-4516-BE60-4BE0B832FDFD
// Assembly location: D:\FactoryMutiplier.dll

using BepInEx;
using HarmonyLib;
using System;
using xiaoye97;

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

        private void Start()
        {
            LDBTool.PostAddDataAction += PostAddModifyPower;
            Harmony.CreateAndPatchAll(typeof(Multiplier), (string)null);
            Multiplier.walkspeedMultiply = this.Config.Bind<int>("config", "walkspeedMultiply", 1, "Multiplies walking speed").Value;
            Multiplier.miningMultiply = this.Config.Bind<int>("config", "miningMultiply", 1, "Multiplies speed of miners").Value;
            Multiplier.smeltMultiply = this.Config.Bind<int>("config", "smeltMultiply", 1, "Multiplies speed of smelters").Value;
            Multiplier.chemicalMultiply = this.Config.Bind<int>("config", "chemicalMultiply", 1, "Multiplies speed of chemical plants").Value;
            Multiplier.refineMultiply = this.Config.Bind<int>("config", "refineMultiply", 1, "Multiplies speed of refineries").Value;
            Multiplier.assembleMultiply = this.Config.Bind<int>("config", "assembleMultiply", 1, "Multiplies speed of assemblers").Value;
            Multiplier.particleMultiply = this.Config.Bind<int>("config", "particleMultiply", 1, "Multiplies speed of particle colliders").Value;
            Multiplier.labMultiply = this.Config.Bind<int>("config", "labMultiply", 1, "Multiplies speed of laboratories").Value;
            Multiplier.fractionateMultiply = this.Config.Bind<int>("config", "fractionateMultiply", 1, "Multiplies % of fractionators").Value;
            Multiplier.ejectorMultiply = this.Config.Bind<int>("config", "ejectorMultiply", 1, "Multiplies speed of EM rail ejectors").Value;
            Multiplier.siloMultiply = this.Config.Bind<int>("config", "siloMultiply", 1, "Multiplies speed of silos").Value;
            Multiplier.gamaMultiply = this.Config.Bind<int>("config", "gamaMultiply", 1, "Multiplies speed of ray recievers").Value;

        }

        private void Update()
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        private static void MechawalkingSpeed_get(GameHistoryData __instance)
        {
            for (int index = 8; index > 0; --index)
            {
                if (!__instance.techStates[2201].unlocked)
                {
                    Multiplier.walkspeed_set = 0;
                    break;
                }
                if (__instance.techStates[2200 + index].unlocked)
                {
                    Multiplier.walkspeed_set = index;
                    break;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), "Import")]
        private static void MechawalkingSpeed_patch(Mecha __instance)
        {
            if (Multiplier.walkspeed_set == 0)
                __instance.walkSpeed = Configs.freeMode.mechaWalkSpeed * (float)Multiplier.walkspeedMultiply;
            else if (Multiplier.walkspeed_set >= 7)
            {
                __instance.walkSpeed = (float)((double)Configs.freeMode.mechaWalkSpeed + (double)((Multiplier.walkspeed_set - 6) * 2) + 6.0) * (float)Multiplier.walkspeedMultiply;
            }
            else
            {
                if (Multiplier.walkspeed_set >= 7)
                    return;
                __instance.walkSpeed = (Configs.freeMode.mechaWalkSpeed + (float)Multiplier.walkspeed_set) * (float)Multiplier.walkspeedMultiply;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        private static void MiningSpeedScale_patch(GameHistoryData __instance)
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Smelt_patch(FactorySystem __instance)
        {
            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2302);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(2315);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(3700);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Chemical_patch(FactorySystem __instance)
        {
            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2309);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(3701);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(3702);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Refine_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2308);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                if (__instance.assemblerPool[index].id == index && (int)__instance.assemblerPool[index].recipeType == 3)
                    __instance.assemblerPool[index].speed = Multiplier.refineMultiply * itemProto.prefabDesc.assemblerSpeed;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Assemble_patch(FactorySystem __instance)
        {
            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2303);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(2304);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(2305);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                int entityId = __instance.assemblerPool[index].entityId;
                if (entityId > 0)
                {
                    ItemProto itemProto4 = ((ProtoSet<ItemProto>)LDB.items).Select((int)__instance.factory.entityPool[entityId].protoId);
                    if (((Proto)itemProto4).ID == ((Proto)itemProto1).ID)
                        __instance.assemblerPool[index].speed = Multiplier.assembleMultiply * itemProto1.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto2).ID)
                        __instance.assemblerPool[index].speed = Multiplier.assembleMultiply * itemProto2.prefabDesc.assemblerSpeed;
                    else if (((Proto)itemProto4).ID == ((Proto)itemProto3).ID)
                        __instance.assemblerPool[index].speed = Multiplier.assembleMultiply * itemProto3.prefabDesc.assemblerSpeed;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Particle_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2310);
            for (int index = 1; index < __instance.assemblerCursor; ++index)
            {
                if (__instance.assemblerPool[index].id == index && (int)__instance.assemblerPool[index].recipeType == 5)
                    __instance.assemblerPool[index].speed = Multiplier.particleMultiply * itemProto.prefabDesc.assemblerSpeed;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Lab_patch(FactorySystem __instance)
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
        private static void Techspeed_patch(GameHistoryData __instance)
        {
            for (int index = 2; index > 0; --index)
            {
                if (__instance.techStates[3903].unlocked)
                {
                    __instance.techSpeed = __instance.techStates[3904].curLevel * Multiplier.labMultiply;
                    break;
                }
                if (!__instance.techStates[3901].unlocked)
                {
                    __instance.techSpeed = Configs.freeMode.techSpeed * Multiplier.labMultiply;
                    break;
                }
                if (__instance.techStates[3900 + index].unlocked)
                {
                    __instance.techSpeed = (index + Configs.freeMode.techSpeed) * Multiplier.labMultiply;
                    break;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Fractionate_patch(FactorySystem __instance)
        {
            for (int index = 1; index < __instance.fractionateCursor; ++index)
            {
                if (__instance.fractionatePool[index].id == index)
                    __instance.fractionatePool[index].produceProb = (float)Multiplier.fractionateMultiply * 0.01f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Ejector_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2311);
            for (int index = 1; index < __instance.ejectorCursor; ++index)
            {
                if (__instance.ejectorPool[index].id == index)
                {
                    __instance.ejectorPool[index].chargeSpend = itemProto.prefabDesc.ejectorChargeFrame * 10000 / Multiplier.ejectorMultiply;
                    __instance.ejectorPool[index].coldSpend = itemProto.prefabDesc.ejectorColdFrame * 10000 / Multiplier.ejectorMultiply;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void Silo_patch(FactorySystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2312);
            for (int index = 1; index < __instance.siloCursor; ++index)
            {
                if (__instance.siloPool[index].id == index)
                {
                    __instance.siloPool[index].chargeSpend = itemProto.prefabDesc.siloChargeFrame * 10000 / Multiplier.siloMultiply;
                    __instance.siloPool[index].coldSpend = itemProto.prefabDesc.siloColdFrame * 10000 / Multiplier.siloMultiply;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        private static void Gamma_patch(PowerSystem __instance)
        {
            ItemProto itemProto = ((ProtoSet<ItemProto>)LDB.items).Select(2208);
            for (int index = 1; index < __instance.genCursor; ++index)
            {
                if (__instance.genPool[index].id == index && __instance.genPool[index].gamma)
                    __instance.genPool[index].genEnergyPerTick = (long)Multiplier.gamaMultiply * itemProto.prefabDesc.genEnergyPerTick;
            }
        }
      
        public static void PostAddModifyPower()
        {
            ItemProto smelter = ((ProtoSet<ItemProto>)LDB.items).Select(3700);
            smelter.prefabDesc.workEnergyPerTick *= Multiplier.smeltMultiply;

            ItemProto chem = ((ProtoSet<ItemProto>)LDB.items).Select(3701);
            chem.prefabDesc.workEnergyPerTick *= Multiplier.chemicalMultiply;

            ItemProto chem2 = ((ProtoSet<ItemProto>)LDB.items).Select(3702);
            chem2.prefabDesc.workEnergyPerTick *= Multiplier.chemicalMultiply;

            var protos = new ItemProto[] { chem,chem2,smelter };
            for (int i = 0; i < protos.Length; i++)
            {
                if (protos[i] is null)
                    UnityEngine.Debug.Log("The proto at the following index is null: " + i);
            }
        }
       [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        private static void Power_patch(PowerSystem __instance)
        {

            ItemProto itemProto1 = ((ProtoSet<ItemProto>)LDB.items).Select(2302);
            ItemProto itemProto2 = ((ProtoSet<ItemProto>)LDB.items).Select(2315);
            ItemProto itemProto3 = ((ProtoSet<ItemProto>)LDB.items).Select(2309);
            ItemProto itemProto4 = ((ProtoSet<ItemProto>)LDB.items).Select(2308);
            ItemProto itemProto5 = ((ProtoSet<ItemProto>)LDB.items).Select(2303);
            ItemProto itemProto6 = ((ProtoSet<ItemProto>)LDB.items).Select(2304);
            ItemProto itemProto7 = ((ProtoSet<ItemProto>)LDB.items).Select(2305);
            ItemProto itemProto8 = ((ProtoSet<ItemProto>)LDB.items).Select(2310);
            ItemProto itemProto9 = ((ProtoSet<ItemProto>)LDB.items).Select(2314);
            ItemProto itemProto10 = ((ProtoSet<ItemProto>)LDB.items).Select(2901);
            ItemProto itemProto11 = ((ProtoSet<ItemProto>)LDB.items).Select(2311);
            ItemProto itemProto12 = ((ProtoSet<ItemProto>)LDB.items).Select(2312);
            

                var protos = new ItemProto[] { itemProto1, itemProto2, itemProto3, itemProto4, itemProto5, itemProto6, itemProto7, itemProto8, itemProto9, itemProto10, itemProto12 };
                for (int i = 0; i < protos.Length; i++)
                {
                    if (protos[i] is null)
                        UnityEngine.Debug.Log("The proto at the following index is null: " + i);
                }

                for (int index = 1; index < __instance.consumerCursor; ++index)
                {
                    int entityId = __instance.consumerPool[index].entityId;
                    if (entityId > 0)
                    {
                        ItemProto itemProto13 = ((ProtoSet<ItemProto>)LDB.items).Select((int)__instance.factory.entityPool[entityId].protoId);
                        if (((Proto)itemProto13).ID == ((Proto)itemProto1).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.smeltMultiply * itemProto1.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto2).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.smeltMultiply * itemProto2.prefabDesc.workEnergyPerTick;
 
                        if (((Proto)itemProto13).ID == ((Proto)itemProto3).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.chemicalMultiply * itemProto3.prefabDesc.workEnergyPerTick;
             
                        if (((Proto)itemProto13).ID == ((Proto)itemProto4).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.refineMultiply * itemProto4.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto5).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.assembleMultiply * itemProto5.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto6).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.assembleMultiply * itemProto6.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto7).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.assembleMultiply * itemProto7.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto8).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.particleMultiply * itemProto8.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto9).ID)
                        {
                            if (Multiplier.fractionateMultiply == 1)
                                __instance.consumerPool[index].workEnergyPerTick = itemProto9.prefabDesc.workEnergyPerTick;
                            else if (Multiplier.fractionateMultiply != 1)
                                __instance.consumerPool[index].workEnergyPerTick = (long)(Math.Pow(1.055, (double)Multiplier.fractionateMultiply) * (double)Multiplier.fractionateMultiply * (double)itemProto9.prefabDesc.workEnergyPerTick);
                        }
                        if (((Proto)itemProto13).ID == ((Proto)itemProto10).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.labMultiply * itemProto10.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto11).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.ejectorMultiply * itemProto11.prefabDesc.workEnergyPerTick;
                        if (((Proto)itemProto13).ID == ((Proto)itemProto12).ID)
                            __instance.consumerPool[index].workEnergyPerTick = (long)Multiplier.siloMultiply * itemProto12.prefabDesc.workEnergyPerTick;

                    }
                }
            }

        }
    }
