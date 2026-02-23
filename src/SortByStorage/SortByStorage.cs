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
                    __instance.productSortBox.Items.Add("Storage (Desc)".Translate());
                    __instance.productSortBox.ItemsData.Add(SORT_STORAGE_DESC);
                }
                if (!foundAsc)
                {
                    __instance.productSortBox.Items.Add("Storage (Asc)".Translate());
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
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UIStatisticsWindow_DetermineProductEntryList_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            var addMethod = AccessTools.Method(typeof(UIProductEntryList), nameof(UIProductEntryList.Add), new[] { typeof(int), typeof(long), typeof(long) });
            var addWithAccMethod = AccessTools.Method(typeof(UIProductEntryList), nameof(UIProductEntryList.Add), new[] { typeof(int), typeof(long), typeof(long), typeof(long) });

            if (addMethod == null || addWithAccMethod == null)
            {
                Log.Warning("Could not find UIProductEntryList.Add methods.");
                return instructions;
            }

            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, addMethod));

            while (matcher.IsValid)
            {
                var callPos = matcher.Pos;
                var productStatIdx = -1;
                object productStatOperand = null;

                for (int i = callPos - 1; i >= 0; i--)
                {
                    var instr = matcher.InstructionAt(i);
                    if (instr.opcode == OpCodes.Ldfld && ((System.Reflection.FieldInfo)instr.operand).Name == "itemId")
                    {
                        var prevInstr = matcher.InstructionAt(i - 1);
                        if (prevInstr.IsLdloc())
                        {
                            productStatIdx = ExtractLocalIndex(prevInstr);
                            productStatOperand = prevInstr.operand;
                            break;
                        }
                    }
                }

                if (productStatIdx != -1)
                {
                    matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldloc, productStatOperand ?? productStatIdx),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ProductStat), nameof(ProductStat.storageCount)))
                    );
                    
                    matcher.SetOperandAndAdvance(addWithAccMethod);
                }
                else
                {
                    matcher.Advance(1);
                }

                matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, addMethod));
            }

            return matcher.InstructionEnumeration();
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
            if (!__instance.isProductionTab) return;

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
