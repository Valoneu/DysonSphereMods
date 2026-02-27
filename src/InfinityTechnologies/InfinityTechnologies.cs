using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using HarmonyLib;
using UnityEngine;
using DysonSphereMods.Shared;

namespace InfinityTechnologies
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), "LocalizationModule")]
    public class InfinityTechnologiesPlugin : BaseUnityPlugin
    {
        public static ModifierManager Modifiers;

        private void Awake()
        {
            Log.Init(Logger);
            Modifiers = new ModifierManager();

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(InfinityTechnologiesPlugin));
            harmony.PatchAll(typeof(Patches));

            TechDefinitions.Register();

            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        public static void OnTechUnlocked()
        {
            Modifiers.Recalculate();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void OnGameBegin()
        {
            Modifiers.Recalculate();
        }
    }

    public class ModifierManager
    {
        public float SpherePowerMultiplier = 1f;
        public float ProliferatorMultiplier = 1f;
        public float CombatSpeedMultiplier = 1f;
        public float WirelessPowerMultiplier = 1f;
        public float ResearchProductivityMultiplier = 1f;

        public void Recalculate()
        {
            var history = GameMain.history;
            if (history == null || LDB.techs == null) return;

            // Reset
            SpherePowerMultiplier = 1f;
            ProliferatorMultiplier = 1f;
            CombatSpeedMultiplier = 1f;
            WirelessPowerMultiplier = 1f;
            ResearchProductivityMultiplier = 1f;

            float GetLevelBonus(int techId, float perLevel)
            {
                if (history.techStates.TryGetValue(techId, out var state))
                {
                    var proto = LDB.techs.Select(techId);
                    if (proto != null)
                        return 1f + Math.Max(0, state.curLevel - proto.Level) * perLevel;
                }
                return 1f;
            }

            WirelessPowerMultiplier = GetLevelBonus(9002, 0.2f);
            SpherePowerMultiplier = GetLevelBonus(9051, 0.005f);
            ProliferatorMultiplier = GetLevelBonus(9052, 0.0025f);
            CombatSpeedMultiplier = GetLevelBonus(9054, 0.05f);
            ResearchProductivityMultiplier = GetLevelBonus(9055, 0.1f);

            Log.Debug($"Recalculated Modifiers: Combat={CombatSpeedMultiplier:F2}, Prolif={ProliferatorMultiplier:F3}, Wireless={WirelessPowerMultiplier:F2}");

            ApplyGlobalChanges();
        }

        private void ApplyGlobalChanges()
        {
            // Dyson Sphere
            if (GameMain.data?.dysonSpheres != null)
            {
                foreach (var ds in GameMain.data.dysonSpheres)
                    if (ds != null) Patches.UpdateDysonSpherePower(ds);
            }

            // Proliferator
            if (ProliferatorMultiplier > 1.0001f)
            {
                float bonus = ProliferatorMultiplier - 1f;
                for (int i = 1; i <= 10; i++)
                {
                    if (i == 1 || i == 2 || i == 4)
                    {
                        double baseInc = i == 1 ? 0.125 : (i == 2 ? 0.2 : 0.25);
                        Cargo.incTableMilli[i] = Math.Min(0.5, baseInc + bonus);
                        Cargo.accTableMilli[i] = Math.Min(2.5, (double)Cargo.accTable[i] / 1000.0 + bonus);
                    }
                }
            }
        }
    }

    public static class Patches
    {
        private static ModifierManager M => InfinityTechnologiesPlugin.Modifiers;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_Postfix()
        {
            Log.Info("Syncing Mod Technology Metadata...");

            void CopyTechIcon(int targetId, int sourceTechId)
            {
                var target = LDB.techs.Select(targetId);
                var source = LDB.techs.Select(sourceTechId);
                if (target != null && source != null)
                {
                    target.IconPath = source.IconPath;
                    Traverse.Create(target).Field("_iconSprite").SetValue(source.iconSprite);
                    Log.Debug($"Applied icon from tech {sourceTechId} to {targetId}");
                }
                else if (target == null) Log.Warning($"Target tech {targetId} not found for icon sync.");
                else Log.Warning($"Source tech {sourceTechId} not found for icon sync.");
            }

            void CopyItemIcon(int targetTechId, int sourceItemId)
            {
                var target = LDB.techs.Select(targetTechId);
                var source = LDB.items.Select(sourceItemId);
                if (target != null && source != null)
                {
                    target.IconPath = source.IconPath;
                    Traverse.Create(target).Field("_iconSprite").SetValue(source.iconSprite);
                    Log.Debug($"Applied icon from item {sourceItemId} to tech {targetTechId}");
                }
            }

            // Forced Icon Sync - Use tech names if IDs are slippery, but 2301 should be fine.
            CopyTechIcon(9001, 2301); // Inventory Capacity
            CopyTechIcon(9002, 1101); // High-Efficiency Plasma Control
            CopyTechIcon(9051, 1501); // Dyson Sphere
            CopyTechIcon(9052, 1153); // Proliferator Mk3
            CopyTechIcon(9054, 1817); // Gravity Missile
            CopyItemIcon(9055, 6006); // Universe Matrix

            // Forced Description Sync - Set both fields
            var inv = LDB.techs.Select(9001);
            if (inv != null) { 
                inv.Desc = inv.description = "TechDesc_9001".Translate(); 
            }
            var prolif = LDB.techs.Select(9052);
            if (prolif != null) { 
                prolif.Desc = prolif.description = "TechDesc_9052".Translate(); 
            }
            
            Log.Info("Metadata sync complete.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechProto), nameof(TechProto.UnlockFunctionText))]
        public static void TechProto_UnlockFunctionText_Postfix(TechProto __instance, ref string __result)
        {
            if (__instance.ID < 9000) return;

            var lines = new List<string>();
            bool hasVanilla = __instance.UnlockFunctions.Any(f => f < 100);

            for (int i = 0; i < __instance.UnlockFunctions.Length; i++)
            {
                int funcId = __instance.UnlockFunctions[i];
                if (funcId >= 100)
                {
                    string key = $"TechFunction_{funcId}";
                    string translated = key.Translate();
                    if (translated != key)
                        lines.Add(string.Format(translated, __instance.UnlockValues[i]));
                }
            }

            if (lines.Count > 0)
            {
                string customText = string.Join("\n", lines);
                if (string.IsNullOrEmpty(__result) || !hasVanilla)
                    __result = customText;
                else
                    __result += "\n" + customText;
            }
        }

        private static AccessTools.FieldRef<DysonSphere, bool> _needRecalculatePowerRef = AccessTools.FieldRefAccess<DysonSphere, bool>("needRecalculatePower");

        public static void UpdateDysonSpherePower(DysonSphere ds)
        {
            if (ds?.starData == null || M.SpherePowerMultiplier <= 1.0001f) return;
            float lumino = ds.starData.dysonLumino;
            ds.energyGenPerSail = (long)(Configs.freeMode.solarSailEnergyPerTick * lumino * M.SpherePowerMultiplier);
            ds.energyGenPerNode = (long)(Configs.freeMode.dysonNodeEnergyPerTick * lumino * M.SpherePowerMultiplier);
            ds.energyGenPerFrame = (long)(Configs.freeMode.dysonFrameEnergyPerTick * lumino * M.SpherePowerMultiplier);
            ds.energyGenPerShell = (long)(Configs.freeMode.dysonShellEnergyPerTick * lumino * M.SpherePowerMultiplier);

            _needRecalculatePowerRef(ds) = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Init))]
        public static void DysonSphere_Init_Postfix(DysonSphere __instance) => UpdateDysonSpherePower(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText))]
        public static void UITechTree_RefreshDataValueText_Postfix(UITechTree __instance)
        {
            if (__instance.upgradePage != 1) return;

            string currentText = __instance.dataValue1_8.text;
            if (string.IsNullOrEmpty(currentText) || currentText.Contains("TechName_90")) return;

            var sb = new StringBuilder(currentText);

            int modCount = 0;
            if (M.SpherePowerMultiplier > 1.0001f)
            {
                sb.Append($"\n{"TechName_9051".Translate()} +{M.SpherePowerMultiplier - 1f:P1}");
                modCount++;
            }
            if (M.ResearchProductivityMultiplier > 1.0001f)
            {
                sb.Append($"\n{"TechName_9055".Translate()} +{M.ResearchProductivityMultiplier - 1f:P0}");
                modCount++;
            }
            if (M.ProliferatorMultiplier > 1.0001f)
            {
                sb.Append($"\n{"TechName_9052".Translate()} +{M.ProliferatorMultiplier - 1f:P1}");
                modCount++;
            }
            if (M.WirelessPowerMultiplier > 1.0001f)
            {
                sb.Append($"\n{"TechName_9002".Translate()} +{M.WirelessPowerMultiplier - 1f:P0}");
                modCount++;
            }
            if (M.CombatSpeedMultiplier > 1.0001f)
            {
                sb.Append($"\n{"TechName_9054".Translate()} +{M.CombatSpeedMultiplier - 1f:P0}");
                modCount++;
            }

            __instance.dataValue1_8.text = sb.ToString();
            if (modCount > 0)
            {
                float targetHeight = (GameMain.history.inserterStackCountObsolete > 1 ? 168f : 150f) + modCount * 18f;
                __instance.rect1_8.sizeDelta = new Vector2(__instance.rect1_8.sizeDelta.x, targetHeight);
            }
        }

        // --- Research Productivity Fix ---
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateResearch")]
        public static void LabComponent_Research_Prefix(ref TechState ts, out long __state)
        {
            __state = ts.hashUploaded;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateResearch")]
        public static void LabComponent_Research_Postfix(ref TechState ts, ref long hashRegister, ref long uMatrixPoint, ref int techHashedThisFrame, long __state)
        {
            if (M.ResearchProductivityMultiplier <= 1.001f) return;
            long delta = ts.hashUploaded - __state;
            if (delta > 0)
            {
                long bonus = (long)(delta * (M.ResearchProductivityMultiplier - 1f));
                ts.hashUploaded = Math.Min(ts.hashNeeded, ts.hashUploaded + bonus);
                hashRegister += bonus;
                uMatrixPoint += bonus * ts.uPointPerHash;
                techHashedThisFrame += (int)bonus;
            }
        }

        // --- Lab UI Update for Productivity ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UILabWindow), "_OnUpdate")]
        public static void UILabWindow_OnUpdate_Postfix(UILabWindow __instance)
        {
            if (M.ResearchProductivityMultiplier <= 1.001f) return;
            if (__instance.labId == 0 || __instance.factory == null) return;
            LabComponent lab = __instance.factorySystem.labPool[__instance.labId];
            if (lab.id != __instance.labId || !lab.researchMode) return;

            string suffix = "哈希每秒".Translate(); 
            string text = __instance.speedText.text;
            if (text.EndsWith(suffix))
            {
                string numPart = text.Substring(0, text.Length - suffix.Length);
                if (float.TryParse(numPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float speed))
                {
                    speed *= M.ResearchProductivityMultiplier;
                    __instance.speedText.text = speed.ToString("0.0") + suffix;
                }
            }
        }

        // --- Drone/Vessel & Turret Attack Speed & Wireless Power Text Fixes ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
        public static void ItemProto_GetPropValue_Postfix(ItemProto __instance, int index, StringBuilder sb, int incLevel, ref string __result)
        {
            if (M.CombatSpeedMultiplier > 1.001f)
            {
                // CRITICAL FIX: index is the array position, not the property type.
                // We must check __instance.DescFields[index] to get the type (49=ROF, 63/66=ROF).
                if (index >= 0 && index < __instance.DescFields.Length)
                {
                    int propType = __instance.DescFields[index];

                    if (propType == 49 || propType == 63 || propType == 66)
                    {
                        float bonusRatio = M.CombatSpeedMultiplier - 1f;
                        string unit = "发每秒".Translate();

                        float baseROF = 0f;
                        if (__instance.prefabDesc.isTurret && propType == 49)
                        {
                            baseROF = __instance.prefabDesc.turretMuzzleCount <= 1 ? 
                                ((float)__instance.prefabDesc.turretROF * 60f / (float)__instance.prefabDesc.turretRoundInterval) : 
                                ((float)__instance.prefabDesc.turretROF * 60f * (float)__instance.prefabDesc.turretMuzzleCount / (float)(__instance.prefabDesc.turretRoundInterval + __instance.prefabDesc.turretMuzzleInterval * ((int)__instance.prefabDesc.turretMuzzleCount - 1)));
                        }
                        else if (__instance.prefabDesc.isCraftUnit)
                        {
                            if (propType == 63)
                            {
                                baseROF = __instance.prefabDesc.craftUnitMuzzleCount1 > 1 ? 
                                    (float)__instance.prefabDesc.craftUnitROF1 * 60f * (float)__instance.prefabDesc.craftUnitMuzzleCount1 / (float)(__instance.prefabDesc.craftUnitRoundInterval1 + __instance.prefabDesc.craftUnitMuzzleInterval1 * (__instance.prefabDesc.craftUnitMuzzleCount1 - 1)) : 
                                    (float)__instance.prefabDesc.craftUnitROF1 * 60f / (float)__instance.prefabDesc.craftUnitRoundInterval1;
                            }
                            else // 49 or 66
                            {
                                baseROF = __instance.prefabDesc.craftUnitMuzzleCount0 > 1 ? 
                                    (float)__instance.prefabDesc.craftUnitROF0 * 60f * (float)__instance.prefabDesc.craftUnitMuzzleCount0 / (float)(__instance.prefabDesc.craftUnitRoundInterval0 + __instance.prefabDesc.craftUnitMuzzleInterval0 * (__instance.prefabDesc.craftUnitMuzzleCount0 - 1)) : 
                                    (float)__instance.prefabDesc.craftUnitROF0 * 60f / (float)__instance.prefabDesc.craftUnitRoundInterval0;
                            }
                        }

                        if (baseROF > 0.001f)
                        {
                            float bonus = baseROF * bonusRatio;
                            string baseText = __result.EndsWith(unit) ? __result.Substring(0, __result.Length - unit.Length) : __result;
                            
                            if (baseText.Contains("<color="))
                            {
                                int lastPos = baseText.LastIndexOf("</color>");
                                if (lastPos > 0)
                                {
                                    string before = baseText.Substring(0, lastPos);
                                    string after = baseText.Substring(lastPos);
                                    __result = $"{before} + {bonus.ToString("0.##")}{after}{unit}";
                                }
                            }
                            else
                            {
                                __result = $"{baseText}<color=#61D8FFB8> + {bonus.ToString("0.##")}</color>{unit}";
                            }
                        }
                    }
                }
            }

            if (M.WirelessPowerMultiplier > 1.001f)
            {
                if (index >= 0 && index < __instance.DescFields.Length)
                {
                    int propType = __instance.DescFields[index];

                    // 11 is "Working Power" (Power Node)
                    if (propType == 11 && __instance.prefabDesc.isPowerNode && !__instance.prefabDesc.isAccumulator)
                    {
                        float bonusRatio = M.WirelessPowerMultiplier - 1f;
                        string unit = "W";
                        
                        long basePower = __instance.prefabDesc.workEnergyPerTick * 60L;
                        if (basePower > 0)
                        {
                            long bonusPower = (long)(basePower * bonusRatio);
                            string bonusStr = "";
                            if (bonusPower >= 1000000)
                            {
                                bonusStr = (bonusPower / 1000000.0).ToString("0.##") + " M";
                            }
                            else if (bonusPower >= 1000)
                            {
                                bonusStr = (bonusPower / 1000.0).ToString("0.##") + " k";
                            }
                            else
                            {
                                bonusStr = bonusPower.ToString();
                            }
                            
                            string baseText = __result.EndsWith(unit) ? __result.Substring(0, __result.Length - unit.Length) : __result;
                            
                            __result = $"{baseText}<color=#61D8FFB8> + {bonusStr}</color>{unit}";
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText))]
        public static void UITechTree_RefreshDataValueText_Prefix(UITechTree __instance, out float __state)
        {
            __state = 0f;
            if (__instance.upgradePage == 3 && M.CombatSpeedMultiplier > 1.001f)
            {
                __state = M.CombatSpeedMultiplier - 1f;
                GameMain.history.combatDroneROFRatio += __state;
                GameMain.history.combatShipROFRatio += __state;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText))]
        public static void UITechTree_RefreshDataValueText_Restore_Postfix(UITechTree __instance, float __state)
        {
            if (__state > 0f && __instance.upgradePage == 3)
            {
                GameMain.history.combatDroneROFRatio -= __state;
                GameMain.history.combatShipROFRatio -= __state;
            }
        }

        // --- Wireless Power Grid Drain Fix ---
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        public static IEnumerable<CodeInstruction> PowerSystem_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var workEnergyField = AccessTools.Field(typeof(PowerNodeComponent), nameof(PowerNodeComponent.workEnergyPerTick));
            var modifiersField = AccessTools.Field(typeof(InfinityTechnologiesPlugin), nameof(InfinityTechnologiesPlugin.Modifiers));
            var multiplierField = AccessTools.Field(typeof(ModifierManager), nameof(ModifierManager.WirelessPowerMultiplier));

            if (workEnergyField == null || modifiersField == null || multiplierField == null)
            {
                Log.Error("Failed to find fields for PowerSystem transpiler.");
                return instructions;
            }

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == workEnergyField)
                {
                    int offset = 1;
                    if (i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Conv_R8) offset = 2;
                    codes.Insert(i + offset, new CodeInstruction(OpCodes.Ldsfld, modifiersField));
                    codes.Insert(i + offset + 1, new CodeInstruction(OpCodes.Ldfld, multiplierField));
                    codes.Insert(i + offset + 2, new CodeInstruction(OpCodes.Conv_R8));
                    codes.Insert(i + offset + 3, new CodeInstruction(OpCodes.Mul));
                    break;
                }
            }
            return codes;
        }

        // --- Combat Attack Speed Fixes ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.GetCombatUpgradeData))]
        public static void GameHistoryData_GetCombatUpgradeData_Postfix(ref CombatUpgradeData combatUpgradeData)
        {
            if (M.CombatSpeedMultiplier > 1.001f)
            {
                float bonus = M.CombatSpeedMultiplier - 1f;
                combatUpgradeData.combatDroneROFRatio += bonus;
                combatUpgradeData.combatShipROFRatio += bonus;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TurretComponent), "Shoot")]
        public static void TurretComponent_Shoot_Prefix(ref float power)
        {
            if (M.CombatSpeedMultiplier > 1.001f)
            {
                power *= M.CombatSpeedMultiplier;
            }
        }

        // --- Turret UI Update for Attack Speed ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITurretWindow), "_OnUpdate")]
        public static void UITurretWindow_OnUpdate_Postfix(UITurretWindow __instance)
        {
            if (M.CombatSpeedMultiplier <= 1.001f) return;
            if (__instance.turretId == 0 || __instance.factory == null) return;
            
            string suffix = " /s";
            string text = __instance.bpmText.text;
            if (text.EndsWith(suffix))
            {
                string numPart = text.Substring(0, text.Length - suffix.Length);
                if (float.TryParse(numPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float bpm))
                {
                    bpm *= M.CombatSpeedMultiplier;
                    __instance.bpmText.text = bpm.ToString("0.##") + suffix;
                }
            }
        }

        // --- Dyson Sphere Power UI Update ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEOverview), "_OnInit")]
        public static void UIDEOverview_OnInit_Postfix(UIDEOverview __instance, bool __result)
        {
            if (!__result || __instance.starLuminText == null) return;
            DysonSphere dysonSphere = Traverse.Create(__instance).Field("dysonSphere").GetValue<DysonSphere>();
            if (dysonSphere == null || M.SpherePowerMultiplier <= 1.001f) return;

            string baseLuminText = dysonSphere.starData.dysonLumino.ToString("0.000");
            float bonusLumin = dysonSphere.starData.dysonLumino * (M.SpherePowerMultiplier - 1f);
            __instance.starLuminText.text = $"x {baseLuminText} + {bonusLumin.ToString("0.000")}";
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarDetail), "OnStarDataSet")]
        public static void UIStarDetail_OnStarDataSet_Postfix(UIStarDetail __instance)
        {
            if (__instance.star == null || __instance.luminoValueText == null || M.SpherePowerMultiplier <= 1.001f) return;

            string baseLuminText = __instance.star.dysonLumino.ToString("0.000");
            float bonusLumin = __instance.star.dysonLumino * (M.SpherePowerMultiplier - 1f);
            __instance.luminoValueText.text = $"{baseLuminText} + {bonusLumin.ToString("0.000")} L    ";
        }
    }

    public static class TechDefinitions
    {
        public static void Register()
        {
            RegisterStrings();

            var inv = RegisterTech(9001, "TechName_9001", "TechDesc_9001", "Inventory capacity expanded.", "Icons/Tech/2301", new[] { 6006 }, 50000, 50000, new Vector2(42, 8));
            inv.MaxLevel = 3;
            inv.UnlockFunctions = new[] { 5, 38 };
            inv.UnlockValues = new double[] { 1.0, 1.0 };

            RegisterInfinite(9002, "TechName_9002", "TechDesc_9002", "Icons/Tech/1101", 102, 0.2, 50000, 50000, new Vector2(42, 4));

            RegisterInfinite(9051, "TechName_9051", "TechDesc_9051", "Icons/Tech/1501", 106, 0.005, 10000000, 10000000, new Vector2(50, 8));
            
            var prolif = RegisterInfinite(9052, "TechName_9052", "TechDesc_9052", "Icons/Tech/1153", 107, 0.0025, 10000000, 10000000, new Vector2(50, 4));
            prolif.MaxLevel = 100;

            RegisterInfinite(9054, "TechName_9054", "TechDesc_9054", "Icons/Tech/1817", 109, 0.05, 300000, 300000, new Vector2(50, 0));
            RegisterInfinite(9055, "TechName_9055", "TechDesc_9055", "Icons/ItemRecipe/6006", 110, 0.1, 5000000, 5000000, new Vector2(50, -4));
        }

        private static TechProto RegisterInfinite(int id, string name, string desc, string icon, int func, double val, int cost, int costCoef, Vector2 pos)
        {
            var proto = RegisterTech(id, name, desc, name.Translate(), icon, new[] { 6006 }, cost, costCoef, pos);
            proto.MaxLevel = 100000;
            proto.UnlockFunctions = new[] { func };
            proto.UnlockValues = new[] { val };
            proto.RefreshTranslation();
            return proto;
        }

        private static TechProto RegisterTech(int id, string name, string desc, string concl, string icon, int[] items, int itemCount, int costCoef, Vector2 pos)
        {
            int[] itemPoints = Enumerable.Repeat(36, items.Length).ToArray();
            long hashNeeded = (long)itemCount * 100;
            
            var proto = ProtoRegistry.RegisterTech(id, name, desc, concl, icon, new int[0], items, itemPoints, hashNeeded, new int[0], pos);
            proto.LevelCoef1 = (int)((long)costCoef * 100);
            proto.LevelCoef2 = 0;
            return proto;
        }

        private static void RegisterStrings()
        {
            void Reg(string key, string en) => LocalizationModule.RegisterTranslation(key, en);

            Reg("TechName_9001", "Infinite Inventory");
            Reg("TechDesc_9001", "Increases the player's inventory capacity further (adds 1 row and 1 column per level). Limited to 3 levels.");
            Reg("TechName_9002", "Wireless Power Boost");
            Reg("TechDesc_9002", "Increases the speed at which the mecha charges from wireless power towers.");

            Reg("TechName_9051", "Dyson Sphere Efficiency");
            Reg("TechDesc_9051", "Increases the total power output generated by the Dyson Sphere.");
            Reg("TechName_9052", "Proliferator Enhancement");
            Reg("TechDesc_9052", "Increases the extra output/speed bonus from Proliferated items. Limited to 100 levels.");
            Reg("TechName_9054", "Logistics Combat Optimization");
            Reg("TechDesc_9054", "Increases shooting speed of turrets, drones and vessels.");
            Reg("TechName_9055", "Research Productivity");
            Reg("TechDesc_9055", "Increases the amount of research hashes produced per matrix consumed.");

            Reg("TechFunction_102", "Wireless charging power +{0:P0}");
            Reg("TechFunction_106", "Sphere energy extraction +{0:P1}");
            Reg("TechFunction_107", "Proliferation effects +{0:P1}");
            Reg("TechFunction_109", "Combat fire rate +{0:P0}");
            Reg("TechFunction_110", "Research hashes +{0:P0}");
        }
    }
}
