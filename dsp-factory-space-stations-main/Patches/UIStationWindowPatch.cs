using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace DSPFactorySpaceStations
{
    [HarmonyPatch]
    public static class UIStationWindowPatch
    {
        public static RectTransform contentTrs;
        public static RectTransform scrollTrs;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [HarmonyPriority(Priority.Last)]
        public static void OnStationIdChangePre(UIStationWindow __instance, ref string __state)
        {
            if (__instance.stationId == 0 || __instance.factory == null || __instance.transport?.stationPool == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);

            if (itemProto.ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID || !__instance.active)
            {
                return;
            }

            __state = stationComponent.name;
            if (string.IsNullOrEmpty(__state))
            {
                __state = "Factory Space Station #" + stationComponent.gid;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [HarmonyAfter(new string[] { DSPFactorySpaceStationsPlugin.GIGASTATIONS_GUID })]
        public static void OnStationIdChangePost(UIStationWindow __instance, string __state)
        {
            if (__instance.stationId == 0 || __instance.factory == null || __instance.transport?.stationPool == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);
            if (itemProto.ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID)
            {
                return;
            }

            int storageCount = stationComponent.storage.Length;
            __instance.nameInput.text = __state;

            if (__instance.active)
            {
                foreach (UIStationStorage slot in __instance.storageUIs)
                {
                    slot.popupBoxRect.anchoredPosition = new Vector2(5, 0);
                }

                recalculateWindowHeight(__instance, stationComponent);

                if (stationComponent.minerId == 0)
                {
                    UIRecipePicker.Popup(__instance.windowTrans.anchoredPosition + new Vector2(-300f, -135f), new Action<RecipeProto>(recipe => UIStationWindowPatch.OnRecipePickerReturn(__instance, recipe)));
                }
            }
        }

        public static void recalculateWindowHeight(UIStationWindow uiStationWindow, StationComponent stationComponent)
        {
            int storageCount = stationComponent.storage.Length;

            int visibleCount = storageCount;
            for (int i = storageCount - 1; i >= 0; i--)
            {
                if (stationComponent.storage[i].itemId != 0)
                {
                    break;
                }
                visibleCount--;
            }

            uiStationWindow.windowTrans.sizeDelta = new Vector2(600, 316 + 76 * (visibleCount + 1));
            // FIXME: Update scroll window dimensions from gigastations
            /*scrollTrs.sizeDelta = new Vector2(scrollTrs.sizeDelta.x, 76 * visibleCount);
            contentTrs.sizeDelta = new Vector2(contentTrs.sizeDelta.x, 76 * visibleCount);*/
            for (int i = 0; i < uiStationWindow.storageUIs.Length; i++)
            {
                if (i < visibleCount)
                {
                    uiStationWindow.storageUIs[i].station = stationComponent;
                    uiStationWindow.storageUIs[i].index = i;
                    uiStationWindow.storageUIs[i]._Open();
                }
                else
                {
                    uiStationWindow.storageUIs[i].station = null;
                    uiStationWindow.storageUIs[i].index = 0;
                    uiStationWindow.storageUIs[i]._Close();
                }
                uiStationWindow.storageUIs[i].ClosePopMenu();
            }
        }

        /*[HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        [HarmonyAfter(new string[] { DSPFactorySpaceStationsPlugin.GIGASTATIONS_GUID })]
        public static void OnStationUpdate(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];

            float size = __instance.powerGroupRect.sizeDelta.x - 140;
            float percent = stationComponent.energy / (float)stationComponent.energyMax;

            float diff = percent > 0.7 ? -30 : 30;

            __instance.energyText.rectTransform.anchoredPosition = new Vector2(Mathf.Round(size * percent + diff), 0.0f);
        }*/

        private static void OnRecipePickerReturn(UIStationWindow __instance, RecipeProto recipe)
        {
            if (recipe == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);
            if (itemProto.ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID) 
            {
                return;
            }

            SignData[] entitySignPool = __instance.factory.entitySignPool;
            entitySignPool[stationComponent.entityId].iconId0 = (uint)recipe.ID;
            entitySignPool[stationComponent.entityId].iconType = 2U; // seemingly required for recipe icons to work
            int recipeIconWidth = 11;
            entitySignPool[stationComponent.entityId].w = recipeIconWidth;

            StarSpaceStationsState spaceStationsState = StarSpaceStationsState.byStar(__instance.factory.planet.star);
            spaceStationsState.spaceStations[stationComponent.id] = new SpaceStationState();
            spaceStationsState.spaceStations[stationComponent.id].Init(stationComponent.id);
            var construction = new SpaceStationConstruction();
            construction.FromRecipe(recipe, 8 * 30);
            spaceStationsState.spaceStations[stationComponent.id].construction = construction;

            var maxStorage = itemProto.prefabDesc.stationMaxItemCount + __instance.factory.gameData.history.remoteStationExtraStorage;

            var store = stationComponent.storage;
            lock (store)
            {
                // FIXME: Handle warpers or antimatter rods being part of the recipe
                int i = 2;
                foreach (var item in construction.remainingConstructionItems)
                {
                    store[i].itemId = item.Key;
                    store[i].localLogic = ELogisticStorage.Demand;
                    store[i].remoteLogic = ELogisticStorage.Demand;
                    store[i].max = (int)Math.Min(maxStorage, item.Value);
                    i++;
                }

                // FIXME: Stop abusing a field to store the recipe ID
                stationComponent.minerId = recipe.ID;
            }

            __instance.factory.transport.RefreshTraffic(stationComponent.id);
            __instance.factory.gameData.galacticTransport.RefreshTraffic(stationComponent.gid);
            __instance.OnStationIdChange();
            recalculateWindowHeight(__instance, stationComponent);
        }
    }
}