using System;
using FactoryMultiplier.Util;
using HarmonyLib;

namespace FactoryMultiplier
{
    public static class AssemblerPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        private static void FactorySystem_GameTick_Prefix(FactorySystem __instance)
        {
            MultiplyAssemblers(__instance);
            MultiplyLabs(__instance);
            MultiplyFractionators(__instance);
            MultiplySilos(__instance);
        }
        private static void MultiplySilos(FactorySystem factorySystem)
        {
            ItemProto itemProto = LDB.items.Select(2312);
            // Log.LogWithFrequency("{0} for silo",
                // JsonUtility.ToJson(itemProto));
                
                /*
{"Name":"垂直发射井","ID":2312,"SID":"","Type":6,"SubID":0,"MiningFrom":"","ProduceFrom":"","StackSize":10,
"Grade":0,"Upgrades":[],"IsFluid":false,"IsEntity":true,"CanBuild":true,"BuildInGas":false,
"IconPath":"Icons/ItemRecipe/vertical-launching-silo","ModelIndex":74,"ModelCount":1,
"HpMax":8000,"Ability":0,"HeatValue":0,"Potential":0,"ReactorInc":0.0,"FuelType":0,"BuildIndex":803,"BuildMode":1,"GridIndex":2310,"UnlockKey":0,"PreTechOverride":0,"DescFields":[35,11,12,1],"Description":"I垂直发射井"}

                 */
            for (int index = 1; index < factorySystem.siloCursor; ++index)
            {
                if (factorySystem.siloPool[index].id == index)
                {
                    factorySystem.siloPool[index].chargeSpend = itemProto.prefabDesc.siloChargeFrame * 10000 / PluginConfig.siloMultiplier;
                    factorySystem.siloPool[index].coldSpend = itemProto.prefabDesc.siloColdFrame * 10000 / PluginConfig.siloMultiplier;
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
    }
}