using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace SortByStorage
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class SortByStoragePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.SortByStorage";
        public const string NAME = "SortByStorage";
        public const string VERSION = "1.0.0";

        private void Awake()
        {
            Log.Init(Logger);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(SortByStoragePatch));
            Logger.LogInfo($"{NAME} v{VERSION} loaded!");
        }

        public static class SortByStoragePatch
        {
            public const int SORT_STORAGE_DESC = 5;

            [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnInit))]
            [HarmonyPostfix]
            public static void UIStatisticsWindow__OnInit_Postfix(UIStatisticsWindow __instance)
            {
                if (__instance.productSortBox != null)
                {
                    bool found = false;
                    foreach (var data in __instance.productSortBox.ItemsData)
                    {
                        if (data == SORT_STORAGE_DESC)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        __instance.productSortBox.Items.Add("Storage".Translate());
                        __instance.productSortBox.ItemsData.Add(SORT_STORAGE_DESC);
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
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> UIStatisticsWindow_DetermineProductEntryList_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var addMethod = AccessTools.Method(typeof(UIProductEntryList), nameof(UIProductEntryList.Add), new[] { typeof(int), typeof(long), typeof(long) });
                var addWithAccMethod = AccessTools.Method(typeof(UIProductEntryList), nameof(UIProductEntryList.Add), new[] { typeof(int), typeof(long), typeof(long), typeof(long) });

                if (addMethod == null || addWithAccMethod == null)
                {
                    Log.Warning("Could not find UIProductEntryList.Add methods.");
                    return instructions;
                }

                for (int i = 0; i < codes.Count; i++)
                {
                    // Look for the call to productEntryList.Add(itemId, total[plv], total[index1])
                    if (codes[i].opcode == OpCodes.Callvirt && (System.Reflection.MethodInfo)codes[i].operand == addMethod)
                    {
                        // The stack currently has: this.productEntryList, itemId, total[plv], total[index1]
                        // We need to inject productStat.storageCount before the call.
                        // We need to find where productStat is stored. It's likely a local variable.
                        
                        // In the decompiled code, productStat is local index 4 or similar.
                        // Let's look back to see where it's loaded.
                        
                        int productStatLocalIdx = -1;
                        for (int j = i - 1; j >= 0; j--)
                        {
                            // In the loop: productStat = productPool[index5]
                            // It will be followed by loading fields from it.
                            if (codes[j].opcode == OpCodes.Ldfld && ((System.Reflection.FieldInfo)codes[j].operand).Name == "itemId")
                            {
                                // The instruction before this must load the object
                                if (codes[j-1].IsLdloc())
                                {
                                    productStatLocalIdx = ExtractLocalIndex(codes[j-1]);
                                    break;
                                }
                            }
                        }

                        if (productStatLocalIdx != -1)
                        {
                            codes.Insert(i, new CodeInstruction(OpCodes.Ldloc, productStatLocalIdx));
                            codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ProductStat), nameof(ProductStat.storageCount))));
                            codes[i + 2].operand = addWithAccMethod;
                            i += 2;
                        }
                    }
                }

                return codes;
            }

            private static int ExtractLocalIndex(CodeInstruction instruction)
            {
                if (instruction.opcode == OpCodes.Ldloc_0) return 0;
                if (instruction.opcode == OpCodes.Ldloc_1) return 1;
                if (instruction.opcode == OpCodes.Ldloc_2) return 2;
                if (instruction.opcode == OpCodes.Ldloc_3) return 3;
                if (instruction.opcode == OpCodes.Ldloc_S || instruction.opcode == OpCodes.Ldloc)
                {
                    return Convert.ToInt32(instruction.operand);
                }
                return -1;
            }

            [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.DetermineProductEntryList))]
            [HarmonyPostfix]
            public static void UIStatisticsWindow_DetermineProductEntryList_Postfix(UIStatisticsWindow __instance)
            {
                if (__instance.isProductionTab && __instance.sortMethod == SORT_STORAGE_DESC)
                {
                    int max = __instance.productEntryList.entryDatasCursor - 1;
                    if (max > 0)
                    {
                        QuickSortByStorageDesc(__instance.productEntryList.entryDatas, 0, max, ItemProto.itemIndices);
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
        }
    }
}
