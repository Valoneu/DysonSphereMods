using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(LocalizationModule))]
    public class InfinityTechnologiesPlugin : BaseUnityPlugin
    {
        public static ModifierManager Modifiers { get; private set; }

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
        public static void OnTechUnlocked(int func)
        {
            if (func >= 102 && func <= 110)
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
        public float FuelEfficiencyMultiplier = 1f;
        public float CombatSpeedMultiplier = 1f;
        public float RayReceiverMultiplier = 1f;
        public float AccumulatorMultiplier = 1f;
        public float WirelessPowerMultiplier = 1f;
        public float ResearchProductivityMultiplier = 1f;
        public int AutoRepairBonus = 0;

        public void Recalculate()
        {
            var history = GameMain.history;
            if (history == null || LDB.techs == null) return;

            // Reset
            SpherePowerMultiplier = 1f;
            ProliferatorMultiplier = 1f;
            FuelEfficiencyMultiplier = 1f;
            CombatSpeedMultiplier = 1f;
            RayReceiverMultiplier = 1f;
            AccumulatorMultiplier = 1f;
            WirelessPowerMultiplier = 1f;
            ResearchProductivityMultiplier = 1f;
            AutoRepairBonus = 0;

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

            int GetLevels(int techId)
            {
                if (history.techStates.TryGetValue(techId, out var state))
                {
                    var proto = LDB.techs.Select(techId);
                    if (proto != null)
                        return Math.Max(0, state.curLevel - proto.Level);
                }
                return 0;
            }

            WirelessPowerMultiplier = GetLevelBonus(9002, 0.2f);
            RayReceiverMultiplier = GetLevelBonus(9003, 0.1f);
            AccumulatorMultiplier = GetLevelBonus(9004, 0.2f);
            AutoRepairBonus = GetLevels(9005);
            SpherePowerMultiplier = GetLevelBonus(9051, 0.01f);
            ProliferatorMultiplier = GetLevelBonus(9052, 0.0025f);
            FuelEfficiencyMultiplier = GetLevelBonus(9053, 0.02f);
            CombatSpeedMultiplier = GetLevelBonus(9054, 0.05f);
            ResearchProductivityMultiplier = GetLevelBonus(9055, 0.1f);

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
                for (int i = 1; i <= 10; i++)
                {
                    if (i == 1 || i == 2 || i == 4)
                    {
                        double baseInc = i == 1 ? 0.125 : (i == 2 ? 0.2 : 0.25);
                        Cargo.incTableMilli[i] = Math.Min(0.5, baseInc * ProliferatorMultiplier);
                        Cargo.accTableMilli[i] = Math.Min(2.5, (double)Cargo.accTable[i] / 1000.0 * ProliferatorMultiplier);
                    }
                }
            }
        }
    }

    public static class Patches
    {
        private static ModifierManager M => InfinityTechnologiesPlugin.Modifiers;

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
        [HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.GameTick))]
        public static void ConstructionModuleComponent_GameTick_Prefix(ref ConstructionModuleComponent __instance)
        {
            if (M.AutoRepairBonus > 0)
                __instance.autoReconstructAcc += M.AutoRepairBonus / 60f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), "reactorPowerGenEnhanced", MethodType.Getter)]
        public static void Mecha_ReactorPowerGen_Postfix(ref double __result)
        {
            if (M.FuelEfficiencyMultiplier > 1.001f)
                __result *= M.FuelEfficiencyMultiplier;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mecha), "reactorPowerForWeaponEnhanced", MethodType.Getter)]
        public static void Mecha_ReactorPowerForWeapon_Postfix(ref double __result)
        {
            if (M.FuelEfficiencyMultiplier > 1.001f)
                __result *= M.FuelEfficiencyMultiplier;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.GenerateEnergy))]
        public static IEnumerable<CodeInstruction> Mecha_GenerateEnergy_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var reactorEnergyField = AccessTools.Field(typeof(Mecha), nameof(Mecha.reactorEnergy));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == reactorEnergyField)
                {
                    int j = i + 1;
                    while (j < codes.Count && j < i + 10 && codes[j].opcode != OpCodes.Sub) j++;

                    if (j < codes.Count && codes[j].opcode == OpCodes.Sub)
                    {
                        codes.Insert(j, new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(InfinityTechnologiesPlugin), nameof(InfinityTechnologiesPlugin.Modifiers))));
                        codes.Insert(j + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ModifierManager), nameof(ModifierManager.FuelEfficiencyMultiplier))));
                        codes.Insert(j + 2, new CodeInstruction(OpCodes.Div));
                        break;
                    }
                }
            }
            return codes;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mecha), nameof(Mecha.MarkEnergyChange))]
        public static void Mecha_MarkEnergyChange_Prefix(Mecha __instance, int func, ref double change)
        {
            if (func == 2 && M.WirelessPowerMultiplier > 1.001f)
            {
                double extra = change * (M.WirelessPowerMultiplier - 1f);
                change *= M.WirelessPowerMultiplier;
                __instance.coreEnergy = Math.Min(__instance.coreEnergyCap, __instance.coreEnergy + extra);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.EnergyCap_Gamma_Req))]
        public static void RayReceiver_EnergyCap_Postfix(ref long __result)
        {
            if (M.RayReceiverMultiplier > 1.001f) __result = (long)(__result * M.RayReceiverMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.MaxOutputCurrent_Gamma))]
        public static void RayReceiver_MaxOutput_Postfix(ref long __result)
        {
            if (M.RayReceiverMultiplier > 1.001f) __result = (long)(__result * M.RayReceiverMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerAccumulatorComponent), nameof(PowerAccumulatorComponent.InputCap))]
        public static void Accumulator_InputCap_Postfix(ref long __result)
        {
            if (M.AccumulatorMultiplier > 1.001f) __result = (long)(__result * M.AccumulatorMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerAccumulatorComponent), nameof(PowerAccumulatorComponent.OutputCap))]
        public static void Accumulator_OutputCap_Postfix(ref long __result)
        {
            if (M.AccumulatorMultiplier > 1.001f) __result = (long)(__result * M.AccumulatorMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), "EnergyCap_Fuel")]
        public static void Building_FuelEfficiency_Cap_Postfix(ref long __result)
        {
            if (M.FuelEfficiencyMultiplier > 1.001f) __result = (long)(__result * M.FuelEfficiencyMultiplier);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_Gauss")]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_Laser")]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_Cannon")]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_Plasma")]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_Missile")]
        [HarmonyPatch(typeof(TurretComponent), "Shoot_LocalPlasma")]
        public static void Turret_Shoot_Prefix(ref float power)
        {
            if (M.CombatSpeedMultiplier > 1.001f) power *= M.CombatSpeedMultiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateResearch")]
        public static void LabComponent_Research_Prefix(ref TechState ts, long hashRegister, out long __state)
        {
            __state = ts.hashUploaded;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateResearch")]
        public static void LabComponent_Research_Postfix(ref TechState ts, ref long hashRegister, long __state)
        {
            if (M.ResearchProductivityMultiplier <= 1.001f) return;
            long delta = ts.hashUploaded - __state;
            if (delta > 0)
            {
                long bonus = (long)(delta * (M.ResearchProductivityMultiplier - 1f));
                ts.hashUploaded = Math.Min(ts.hashNeeded, ts.hashUploaded + bonus);
                hashRegister += bonus;
            }
        }
    }

    public static class TechDefinitions
    {
        public static void Register()
        {
            RegisterStrings();

            // 9001: Inventory (+1 row and +1 column per level)
            var inv = RegisterTech(9001, "TechName_9001", "TechDesc_9001", "Inventory capacity expanded.", "Icons/Tech/2104", new[] { 6006 }, 1000, 50000, new Vector2(42, 10));
            inv.MaxLevel = 3;
            inv.UnlockFunctions = new[] { 5, 38 };
            inv.UnlockValues = new double[] { 1.0, 1.0 };

            // 9002-9006: Normal Price Techs
            RegisterInfinite(9002, "TechName_9002", "TechDesc_9002", "Icons/Tech/1503", 102, 0.2, 50000, new Vector2(42, 6));
            RegisterInfinite(9003, "TechName_9003", "TechDesc_9003", "Icons/Tech/1508", 103, 0.1, 100000, new Vector2(42, 2));
            RegisterInfinite(9004, "TechName_9004", "TechDesc_9004", "Icons/Tech/1501", 104, 0.2, 25000, new Vector2(42, -2));
            RegisterInfinite(9005, "TechName_9005", "TechDesc_9005", "Icons/Tech/2407", 105, 0.1, 80000, new Vector2(42, -6));

            var logistic = RegisterTech(9006, "TechName_9006", "TechDesc_9006", "Personal logistics expanded.", "Icons/Tech/3201", new[] { 6006 }, 1000, 10000, new Vector2(42, -10));
            logistic.MaxLevel = 100000;
            logistic.UnlockFunctions = new[] { 32 };
            logistic.UnlockValues = new double[] { 1.0 };

            // 9051-9055: Giga Price Techs
            RegisterInfinite(9051, "TechName_9051", "TechDesc_9051", "Icons/Tech/1510", 106, 0.01, 10000000, new Vector2(50, 10));

            var prolif = RegisterInfinite(9052, "TechName_9052", "TechDesc_9052", "Icons/Tech/1134", 107, 0.0025, 1000000, new Vector2(50, 5));
            prolif.MaxLevel = 100;

            RegisterInfinite(9053, "TechName_9053", "TechDesc_9053", "Icons/Tech/1506", 108, 0.02, 200000, new Vector2(50, 0));
            RegisterInfinite(9054, "TechName_9054", "TechDesc_9054", "Icons/Tech/3107", 109, 0.05, 300000, new Vector2(50, -5));
            RegisterInfinite(9055, "TechName_9055", "TechDesc_9055", "Icons/Tech/2501", 110, 0.1, 5000000, new Vector2(50, -10));
        }

        private static TechProto RegisterInfinite(int id, string name, string desc, string icon, int func, double val, int cost, Vector2 pos)
        {
            var proto = RegisterTech(id, name, desc, name.Translate(), icon, new[] { 6006 }, 1000, cost, pos);
            proto.MaxLevel = 100000;
            proto.UnlockFunctions = new[] { func };
            proto.UnlockValues = new[] { val };
            proto.RefreshTranslation();
            return proto;
        }

        private static TechProto RegisterTech(int id, string name, string desc, string concl, string icon, int[] items, int itemCount, int cost, Vector2 pos)
        {
            return ProtoRegistry.RegisterTech(id, name, desc, concl, icon, new int[0], items, Enumerable.Repeat(itemCount, items.Length).ToArray(), cost, new int[0], pos);
        }

        private static void RegisterStrings()
        {
            void Reg(string key, string en) => LocalizationModule.RegisterTranslation(key, en);

            Reg("TechName_9001", "Infinite Inventory");
            Reg("TechDesc_9001", "Increases the player's inventory capacity further (adds 1 row and 1 column per level).");
            Reg("TechName_9002", "Wireless Power Boost");
            Reg("TechDesc_9002", "Increases the speed at which the mecha charges from wireless power towers.");
            Reg("TechName_9003", "Ray Receiver Overclock");
            Reg("TechDesc_9003", "Increases the maximum power output of Ray Receivers.");
            Reg("TechName_9004", "Accumulator Optimization");
            Reg("TechDesc_9004", "Increases the charge and discharge speed of Accumulators.");
            Reg("TechName_9005", "Auto-Repair Efficiency");
            Reg("TechDesc_9005", "Increases the speed at which construction drones repair structures.");
            Reg("TechName_9006", "Infinite Logistics Slots");
            Reg("TechDesc_9006", "Increases the number of player's personal logistics slots.");

            Reg("TechName_9051", "Dyson Sphere Efficiency");
            Reg("TechDesc_9051", "Increases the total power output generated by the Dyson Sphere.");
            Reg("TechName_9052", "Proliferator Enhancement");
            Reg("TechDesc_9052", "Increases the extra output/speed bonus from Proliferated items.");
            Reg("TechName_9053", "Advanced Fuel Consumption");
            Reg("TechDesc_9053", "Increases the energy yield of all fuels burned in the mecha.");
            Reg("TechName_9054", "Logistics Combat Optimization");
            Reg("TechDesc_9054", "Increases shooting speed of turrets, drones and vessels.");
            Reg("TechName_9055", "Research Productivity");
            Reg("TechDesc_9055", "Increases the amount of research hashes produced per matrix consumed.");

            Reg("TechFunction_102", "Wireless charging power +{0:P0}");
            Reg("TechFunction_103", "Ray receiver output +{0:P0}");
            Reg("TechFunction_104", "Accumulator throughput +{0:P0}");
            Reg("TechFunction_105", "Drone repair speed +{0:P0}");
            Reg("TechFunction_106", "Sphere energy extraction +{0:P0}");
            Reg("TechFunction_107", "Proliferation effects +{0:P1}");
            Reg("TechFunction_108", "Fuel efficiency +{0:P0}");
            Reg("TechFunction_109", "Combat fire rate +{0:P0}");
            Reg("TechFunction_110", "Research hashes +{0:P0}");
        }
    }
}
