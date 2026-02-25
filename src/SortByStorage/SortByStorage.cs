using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace SortByStorage
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class SortByStoragePlugin : BaseUnityPlugin
    {
        public const int SORT_STORAGE_DESC = 5;
        public const int SORT_STORAGE_ASC = 6;

        private void Awake()
        {
            Log.Init(Logger);
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(SortByStoragePlugin));
            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} initialized successfully!");
        }

        [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnInit))]
        [HarmonyPostfix]
        public static void UIStatisticsWindow__OnInit_Postfix(UIStatisticsWindow __instance)
        {
            if (__instance.productSortBox != null)
            {
                bool foundDesc = false;
                bool foundAsc = false;
                foreach (var data in __instance.productSortBox.ItemsData)
                {
                    if (data == SORT_STORAGE_DESC) foundDesc = true;
                    if (data == SORT_STORAGE_ASC) foundAsc = true;
                }

                if (!foundDesc)
                {
                    __instance.productSortBox.Items.Add("Stored Descending".Translate());
                    __instance.productSortBox.ItemsData.Add(SORT_STORAGE_DESC);
                }
                if (!foundAsc)
                {
                    __instance.productSortBox.Items.Add("Stored Ascending".Translate());
                    __instance.productSortBox.ItemsData.Add(SORT_STORAGE_ASC);
                }

                if (!foundDesc || !foundAsc)
                {
                    __instance.productSortBox.UpdateItems();
                }
            }
        }

        [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.DetermineProductEntryList))]
        [HarmonyPrefix]
        public static void UIStatisticsWindow_DetermineProductEntryList_Prefix(UIStatisticsWindow __instance)
        {
            if (__instance.isProductionTab)
            {
                __instance.productionStat.RefreshItemsStorageCount(__instance.astroFilter);
            }
        }

        [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.DetermineProductEntryList))]
        [HarmonyPostfix]
        public static void UIStatisticsWindow_DetermineProductEntryList_Postfix(UIStatisticsWindow __instance)
        {
            if (!__instance.isProductionTab) return;

            // Compute the real storage count because vanilla UIProductEntryList.Add ignores it.
            for (int i = 0; i < __instance.productEntryList.entryDatasCursor; i++)
            {
                var entry = __instance.productEntryList.entryDatas[i];
                if (entry == null) continue;

                long storageCount = 0;
                for (int j = 0; j < __instance.statGroupCursor; ++j)
                {
                    var uiStatGroup = __instance.statGroup[j];
                    if (uiStatGroup.productPool == null || uiStatGroup.itemIndices == null) continue;

                    int poolIndex = uiStatGroup.itemIndices[entry.itemId];
                    if (poolIndex > 0 && poolIndex < uiStatGroup.poolCursor && poolIndex < uiStatGroup.productPool.Length)
                    {
                        var stat = uiStatGroup.productPool[poolIndex];
                        if (stat != null)
                        {
                            storageCount += stat.storageCount;
                        }
                    }
                }
                entry.accumulated = storageCount;
            }

            if (__instance.sortMethod == SORT_STORAGE_DESC)
            {
                int max = __instance.productEntryList.entryDatasCursor - 1;
                if (max > 0)
                {
                    QuickSortByStorageDesc(__instance.productEntryList.entryDatas, 0, max, ItemProto.itemIndices);
                    __instance.productEntryList.RefreshDatasIndices(__instance.lastListCursor);
                }
            }
            else if (__instance.sortMethod == SORT_STORAGE_ASC)
            {
                int max = __instance.productEntryList.entryDatasCursor - 1;
                if (max > 0)
                {
                    QuickSortByStorageAsc(__instance.productEntryList.entryDatas, 0, max, ItemProto.itemIndices);
                    __instance.productEntryList.RefreshDatasIndices(__instance.lastListCursor);
                }
            }
        }

        public static void QuickSortByStorageDesc(UIProductEntryData[] datas, int min, int max, int[] itemIndices)
        {
            if (datas == null || datas.Length == 0 || min >= max)
                return;
            int num = PartitionByStorageDesc(datas, min, max, itemIndices);
            QuickSortByStorageDesc(datas, min, num - 1, itemIndices);
            QuickSortByStorageDesc(datas, num + 1, max, itemIndices);
        }

        public static int PartitionByStorageDesc(UIProductEntryData[] datas, int left, int right, int[] itemIndices)
        {
            UIProductEntryData data = datas[left];
            while (left < right)
            {
                while ((data.accumulated > datas[right].accumulated || (data.accumulated == datas[right].accumulated && itemIndices[data.itemId] <= itemIndices[datas[right].itemId])) && left < right)
                    --right;
                if (left < right)
                    datas[left] = datas[right];
                while ((datas[left].accumulated > data.accumulated || (datas[left].accumulated == data.accumulated && itemIndices[datas[left].itemId] <= itemIndices[data.itemId])) && left < right)
                    ++left;
                if (left < right)
                    datas[right] = datas[left];
            }
            if (left == right)
                datas[left] = data;
            return left;
        }

        public static void QuickSortByStorageAsc(UIProductEntryData[] datas, int min, int max, int[] itemIndices)
        {
            if (datas == null || datas.Length == 0 || min >= max)
                return;
            int num = PartitionByStorageAsc(datas, min, max, itemIndices);
            QuickSortByStorageAsc(datas, min, num - 1, itemIndices);
            QuickSortByStorageAsc(datas, num + 1, max, itemIndices);
        }

        public static int PartitionByStorageAsc(UIProductEntryData[] datas, int left, int right, int[] itemIndices)
        {
            UIProductEntryData data = datas[left];
            while (left < right)
            {
                while ((data.accumulated < datas[right].accumulated || (data.accumulated == datas[right].accumulated && itemIndices[data.itemId] <= itemIndices[datas[right].itemId])) && left < right)
                    --right;
                if (left < right)
                    datas[left] = datas[right];
                while ((datas[left].accumulated < data.accumulated || (datas[left].accumulated == data.accumulated && itemIndices[datas[left].itemId] <= itemIndices[data.itemId])) && left < right)
                    ++left;
                if (left < right)
                    datas[right] = datas[left];
            }
            if (left == right)
                datas[left] = data;
            return left;
        }
    }
}
