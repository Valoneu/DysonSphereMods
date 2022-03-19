using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace DSPFactorySpaceStations
{
    [HarmonyPatch]
    public static class StationComponentPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "NewConsumerComponent")]
        public static bool NewConsumerComponentPrefix(PowerSystem __instance, ref int entityId, ref long work, ref long idle)
        {
            var itemProto = LDB.items.Select(__instance.factory.entityPool[entityId].protoId);
            if (itemProto.ID == DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID)
            {
                work = 1000000; // FIXME: Can this entire patch be replaced by configuring workEnergyPerTick?
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Init")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        public static void StationComponentInitPostfix(StationComponent __instance, ref int _id, ref int _entityId, ref int _pcId, ref PrefabDesc _desc, ref EntityData[] _entityPool) // Do when LDB is done
        {
            if (_entityPool[_entityId].protoId != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID) 
            {
                return;
            }

            __instance.needs = new int[13];

            __instance.energyMax = _desc.stationMaxEnergyAcc;
            __instance.warperMaxCount = 100;
            // FIXME: Choose sensible value and move these configs to central place
            __instance.energyPerTick = 1000000;

            __instance.storage = new StationStore[12];
            // demand antimatter rods
            __instance.storage[0].itemId = 1803;
            __instance.storage[0].localLogic = ELogisticStorage.Demand;
            __instance.storage[0].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[0].max = 750;
            // demand warpers
            __instance.storage[1].itemId = 1210;
            __instance.storage[1].localLogic = ELogisticStorage.Demand;
            __instance.storage[1].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[1].max = 600;

            __instance.slots = new SlotData[12];
            for (var i = 0; i < __instance.storage.Length; i++)
            {
                __instance.slots[i].storageIdx = i;
            }

            recalculateShipDiskPos(__instance, _entityPool);
        }

        public static void recalculateShipDiskPos(StationComponent stationComponent, EntityData[] entityPool)
        {
            ItemProto itemProto = LDB.items.Select(entityPool[stationComponent.entityId].protoId);
            int n = stationComponent.workShipDatas.Length;
            float shipDiskRadius = 11.5f * 2; // the height is stationComponent.shipDockPos
            for (int i = 0; i < n; i++)
            {
                stationComponent.shipDiskRot[i] = Quaternion.Euler(0f, 360f / (float)n * (float)i, 0f);
                stationComponent.shipDiskPos[i] = stationComponent.shipDiskRot[i] * new Vector3(0f, 0f, shipDiskRadius);
            }
            for (int i = 0; i < n; i++)
            {
                stationComponent.shipDiskRot[i] = stationComponent.shipDockRot * stationComponent.shipDiskRot[i];
                stationComponent.shipDiskPos[i] = stationComponent.shipDockPos + stationComponent.shipDockRot * stationComponent.shipDiskPos[i];
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
        public static void InternalTickLocal(StationComponent __instance, PlanetFactory factory, float dt)
        {
            ItemProto itemProto = LDB.items.Select(factory.entityPool[__instance.entityId].protoId);
            if (itemProto.ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID) 
            {
                return;
            }

            // Simulate getting energy from antimatter rods
            // No state available, so removing them probabilistically for now
            StationStore[] store = __instance.storage;
            lock (store)
            {
                if (__instance.storage[0].count == 0)
                {
                    __instance.energyPerTick = 0;
                }
                else if (__instance.energy < __instance.energyMax) {
                    __instance.energyPerTick = 1_500_000_000;
                    var rng = new System.Random();
                    if (rng.Next((int)Math.Round(1.0 / dt)) == 0)
                    {
                        store[0].inc -= 1;
                        store[0].count -= 1;
                    }
                }
                __instance.energy = Math.Min(__instance.energyMax, __instance.energy + (int)Math.Round(__instance.energyPerTick * dt));
            }

            // FIXME: Stop abusing minerId field to store recipe ID
            if (__instance.minerId == 0)
            {
                return;
            }

            var recipe = LDB.recipes.Select(__instance.minerId);
            StarSpaceStationsState spaceStationsState = StarSpaceStationsState.byStar(factory.planet.star);
            var construction = spaceStationsState.spaceStations[__instance.id].construction;
            if (!construction.Complete())
            {
                lock (store)
                {
                    for (int i = 2; i < store.Length; i++)
                    {
                        if (store[i].itemId == 0)
                        {
                            continue;
                        }
                        construction.remainingConstructionItems[store[i].itemId] -= store[i].count;
                        if (construction.remainingConstructionItems[store[i].itemId] <= 0)
                        {
                            var unused = -construction.remainingConstructionItems[store[i].itemId];
                            store[i].count = unused;
                            store[i].inc = unused;
                            store[i].max = 0;
                            construction.remainingConstructionItems[store[i].itemId] = 0;
                        } else {
                            store[i].count = 0;
                            store[i].inc = 0;
                            store[i].max = Math.Min(store[i].max, construction.remainingConstructionItems[store[i].itemId]);
                        }
                    }
                    if (!construction.Complete())
                    {
                        return;
                    }

                    var maxStorage = (itemProto.prefabDesc.stationMaxItemCount + factory.gameData.history.remoteStationExtraStorage);

                    for (int i = 2; i < 12; i++) {
                        store[i] = new StationStore();
                    }

                    // FIXME: Handle warpers or antimatter rods being part of the recipe
                    for (var i = 0; i < recipe.Items.Length; i++)
                    {
                        store[i + 2].itemId = recipe.Items[i];
                        store[i + 2].localLogic = ELogisticStorage.Demand;
                        store[i + 2].remoteLogic = ELogisticStorage.Demand;
                        store[i + 2].max = maxStorage;
                    }
                    for (var i = 0; i < recipe.Results.Length; i++)
                    {
                        store[i + 2 + recipe.Items.Length].itemId = recipe.Results[i];
                        store[i + 2 + recipe.Items.Length].localLogic = ELogisticStorage.Supply;
                        store[i + 2 + recipe.Items.Length].remoteLogic = ELogisticStorage.Supply;
                        store[i + 2 + recipe.Items.Length].max = maxStorage;
                    }
                    if (UIRoot.instance.uiGame.stationWindow != null && UIRoot.instance.uiGame.stationWindow.active && UIRoot.instance.uiGame.stationWindow.stationId == __instance.id)
                    {
                        UIStationWindowPatch.recalculateWindowHeight(UIRoot.instance.uiGame.stationWindow, __instance);
                    }
                }

                // Try to prevent glitches when construction items are also part of the production items
                factory.transport.RefreshTraffic(__instance.id);
                factory.gameData.galacticTransport.RefreshTraffic(__instance.gid);
            }

            lock (store)
            {
                // Stop producing if any recipe results are full
                // FIXME: Be willing to dump some things like hydrogen
                for (int i = 0; i < recipe.Results.Length; i++)
                {
                    if (store[2 + i + recipe.Items.Length].count >= store[2 + i + recipe.Items.Length].max)
                    {
                        return;
                    }
                }
                
                var maxRateOfProduction = 10000;
                var maxProductions = maxRateOfProduction;
                for (int i = 0; i < recipe.Items.Length; i++)
                {
                    var possibleProductionsFromItem = store[2 + i].count / recipe.ItemCounts[i];
                    maxProductions = Math.Min(maxProductions, possibleProductionsFromItem);
                }
                var energyPerProduction = 1_000_000_000 / maxRateOfProduction;
                var possibleProductionsFromEnergy = __instance.energy / energyPerProduction;
                maxProductions = Math.Min(maxProductions, (int)possibleProductionsFromEnergy);

                var producedSinceLastTick = (int)Math.Round(maxProductions * dt);
                for (int i = 0; i < recipe.Items.Length; i++)
                {
                    var itemConsumed = producedSinceLastTick * recipe.ItemCounts[i];
                    store[2 + i].inc -= itemConsumed;
                    store[2 + i].count -= itemConsumed;
                }
                for (int i = 0; i < recipe.Results.Length; i++)
                {
                    var itemProduced = producedSinceLastTick * recipe.ResultCounts[i];
                    store[2 + recipe.Items.Length + i].inc += itemProduced;
                    store[2 + recipe.Items.Length + i].count += itemProduced;
                }
                __instance.energy -= energyPerProduction * producedSinceLastTick;
            }
        }
    }
}