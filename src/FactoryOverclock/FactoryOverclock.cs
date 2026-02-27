using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using HarmonyLib;
using UnityEngine;
using DysonSphereMods.Shared;
namespace FactoryOverclock
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem), nameof(LocalizationModule))]
    public class MultiplierPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.FactoryOverclock";
        public const string NAME = "FactoryOverclock";
        public const string VERSION = "3.0.0";
        private Harmony _harmony;
        public static int BattlefieldAnalysisBaseProtoId = 3009;
        private void Awake()
        {
            Log.Init(Logger);
            PluginConfig.InitConfig(Config);
            InitKeyBinds();
            _harmony = new Harmony(GUID);
            _harmony.PatchAll(typeof(PowerConsumptionPatcher));
            _harmony.PatchAll(typeof(PowerGenerationPatcher));
            _harmony.PatchAll(typeof(FactorySystemPatcher));
            _harmony.PatchAll(typeof(StationPatcher));
            _harmony.PatchAll(typeof(BeltPatcher));
            _harmony.PatchAll(typeof(BuildingPatcher));
            _harmony.PatchAll(typeof(TurretPatcher));
            _harmony.PatchAll(typeof(MultiplierPlugin));
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("jinxOAO.MoreMegaStructure"))
            {
                Log.Info("MoreMegaStructure detected, applying compatibility patches.");
                _harmony.PatchAll(typeof(StationPatcher.MoreMegaStructureCompat));
            }
            Log.Info($"{NAME} {VERSION} loaded.");
        }
        private void Update()
        {
            bool testKeyInvoked = PluginConfig.keyTestMode.Value && VFInput.alt && Input.GetKeyDown(KeyCode.Alpha1);
            var keyBind = CustomKeyBindSystem.GetKeyBind("ToggleOverclock");
            if (keyBind.keyValue || testKeyInvoked)
            {
                PluginConfig.multiplierEnabled.Value = !PluginConfig.multiplierEnabled.Value;
                if (!PluginConfig.multiplierEnabled.Value)
                {
                    Log.Info("Reverting factory to normal speed.");
                    UIRealtimeTip.Popup("Reverting factory to normal");
                }
                else
                {
                    Log.Info($"Applying multipliers. Asm={PluginConfig.assembleMultiplier.Value}, Mine={PluginConfig.miningMultiplier.Value}, Smelt={PluginConfig.smeltMultiplier.Value}");
                    UIRealtimeTip.Popup("Applying multipliers to factory");
                }
                RefreshAllSystems();
            }
        }
        public void RefreshAllSystems()
        {
            if (GameMain.data?.factories == null) return;
            Log.Info("Refreshing all factory systems...");
            foreach (var factory in GameMain.data.factories)
            {
                if (factory == null) continue;
                if (factory.powerSystem != null)
                {
                    PowerGenerationPatcher.SyncGenerators(factory.powerSystem);
                    PowerConsumptionPatcher.SyncPowerSystems(factory.powerSystem);
                }
                if (factory.factorySystem != null)
                {
                    FactorySystemPatcher.SyncAll(factory.factorySystem);
                }
                if (factory.cargoTraffic != null)
                {
                    BeltPatcher.SyncBelts(factory);
                }
            }
        }
        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleOverclock"))
            {
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1214,
                    key = new CombineKey((int)PluginConfig.toggleOverclockKey.Value.MainKey, 0, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleOverclock",
                    canOverride = true
                });
            }
            LocalizationModule.RegisterTranslation("KEYToggleOverclock", "Enable/disable factory OverClock");
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void GameBegin_Postfix()
        {
            var plugin = FindObjectOfType<MultiplierPlugin>();
            plugin?.RefreshAllSystems();
        }
    }
    public static class PluginConfig
    {
        public static ConfigEntry<int> smeltMultiplier;
        public static ConfigEntry<int> chemicalMultiplier;
        public static ConfigEntry<int> refineMultiplier;
        public static ConfigEntry<int> assembleMultiplier;
        public static ConfigEntry<int> particleMultiplier;
        public static ConfigEntry<int> miningMultiplier;
        private static ConfigEntry<int> _labMultiplier;
        private static ConfigEntry<int> _fractionatorMultiplier;
        private static ConfigEntry<int> _ejectorMultiplier;
        private static ConfigEntry<int> _siloMultiplier;
        public static ConfigEntry<int> gammaMultiplier;
        private static ConfigEntry<int> _inserterMultiplier;
        private static ConfigEntry<int> _turretMultiplier;
        private static ConfigEntry<int> _beltMultiplier;
        public static ConfigEntry<double> drawMultiplier;
        private static ConfigEntry<int> _genWindMultiplier;
        private static ConfigEntry<int> _genSolarMultiplier;
        private static ConfigEntry<int> _genGeoMultiplier;
        private static ConfigEntry<int> _genThermalMultiplier;
        private static ConfigEntry<int> _genFusionMultiplier;
        private static ConfigEntry<int> _genStarMultiplier;
        private static ConfigEntry<int> _genExchMultiplier;
        public static int genWindMultiplier => multiplierEnabled.Value ? _genWindMultiplier.Value : 1;
        public static int genSolarMultiplier => multiplierEnabled.Value ? _genSolarMultiplier.Value : 1;
        public static int genGeoMultiplier => multiplierEnabled.Value ? _genGeoMultiplier.Value : 1;
        public static int genThermalMultiplier => multiplierEnabled.Value ? _genThermalMultiplier.Value : 1;
        public static int genFusionMultiplier => multiplierEnabled.Value ? _genFusionMultiplier.Value : 1;
        public static int genStarMultiplier => multiplierEnabled.Value ? _genStarMultiplier.Value : 1;
        public static int genExchMultiplier => multiplierEnabled.Value ? _genExchMultiplier.Value : 1;
        public static ConfigEntry<bool> keyTestMode;
        public static ConfigEntry<KeyboardShortcut> toggleOverclockKey;
        public static ConfigEntry<bool> multiplierEnabled;
        public static ConfigEntry<bool> enableAssemblerPopupLogMessage;
        public static int siloMultiplier => multiplierEnabled.Value ? _siloMultiplier.Value : 1;
        public static int inserterMultiplier => multiplierEnabled.Value ? _inserterMultiplier.Value : 1;
        public static int beltMultiplier => multiplierEnabled.Value ? _beltMultiplier.Value : 1;
        public static int ejectorMultiplier => multiplierEnabled.Value ? _ejectorMultiplier.Value : 1;
        public static int fractionatorMultiplier => multiplierEnabled.Value ? _fractionatorMultiplier.Value : 1;
        public static int labMultiplier => multiplierEnabled.Value ? _labMultiplier.Value : 1;
        public static int turretMultiplier => multiplierEnabled.Value ? _turretMultiplier.Value : 1;
        public static void InitConfig(ConfigFile confFile)
        {
            smeltMultiplier = confFile.Bind("1. Factory", "smeltMultiplier", 2, new ConfigDescription("Multiplies speed of smelters", new AcceptableValueRange<int>(1, 20)));
            chemicalMultiplier = confFile.Bind("1. Factory", "chemicalMultiplier", 2, new ConfigDescription("Multiplies speed of chemical plants", new AcceptableValueRange<int>(1, 20)));
            refineMultiplier = confFile.Bind("1. Factory", "refineMultiplier", 2, new ConfigDescription("Multiplies speed of refineries", new AcceptableValueRange<int>(1, 20)));
            assembleMultiplier = confFile.Bind("1. Factory", "assembleMultiplier", 2, new ConfigDescription("Multiplies speed of assemblers", new AcceptableValueRange<int>(1, 20)));
            particleMultiplier = confFile.Bind("1. Factory", "particleMultiplier", 2, new ConfigDescription("Multiplies speed of particle colliders", new AcceptableValueRange<int>(1, 20)));
            _labMultiplier = confFile.Bind("1. Factory", "labMultiplier", 2, new ConfigDescription("Multiplies speed of laboratories", new AcceptableValueRange<int>(1, 20)));
            _fractionatorMultiplier = confFile.Bind("1. Factory", "fractionateMultiplier", 2, new ConfigDescription("Multiplies % of fractionators", new AcceptableValueRange<int>(1, 20)));
            _ejectorMultiplier = confFile.Bind("1. Factory", "ejectorMultiplier", 2, new ConfigDescription("Multiplies speed of EM rail ejectors", new AcceptableValueRange<int>(1, 100)));
            _siloMultiplier = confFile.Bind("1. Factory", "siloMultiplier", 2, new ConfigDescription("Multiplies speed of silos", new AcceptableValueRange<int>(1, 100)));
            gammaMultiplier = confFile.Bind("1. Factory", "gammaMultiplier", 2, new ConfigDescription("Multiplies speed of ray recievers", new AcceptableValueRange<int>(1, 1000)));
            miningMultiplier = confFile.Bind("1. Factory", "miningMultiplier", 2, new ConfigDescription("Multiplies speed of mining machines", new AcceptableValueRange<int>(1, 20)));
            _inserterMultiplier = confFile.Bind("1. Factory", "sorterMultiplier", 2, new ConfigDescription("Multiplies speed of sorter", new AcceptableValueList<int>(1, 2, 4, 8)));
            _turretMultiplier = confFile.Bind("1. Factory", "turretMultiplier", 2, new ConfigDescription("Multiplies speed of turrets", new AcceptableValueRange<int>(1, 20)));
            _beltMultiplier = confFile.Bind("1. Factory", "beltMultiplier", 1, new ConfigDescription("Multiplies speed of belts (max 2x)", new AcceptableValueRange<int>(1, 2)));
            drawMultiplier = confFile.Bind("1. Factory", "drawMultipler", 1.0, new ConfigDescription("Multiplies how much your factory will draw on top of your normal overclock", new AcceptableValueRange<double>(0.1, 10)));
            _genWindMultiplier = confFile.Bind("2. Generator", "generatorWindMultiplier", 2, new ConfigDescription("Multiplies speed of wind turbines", new AcceptableValueRange<int>(1, 100)));
            _genSolarMultiplier = confFile.Bind("2. Generator", "generatorSolarMultiplier", 2, new ConfigDescription("Multiplies speed of solar panels", new AcceptableValueRange<int>(1, 100)));
            _genGeoMultiplier = confFile.Bind("2. Generator", "generatorGeothermalMultiplier", 2, new ConfigDescription("Multiplies speed of geothermal plants", new AcceptableValueRange<int>(1, 100)));
            _genThermalMultiplier = confFile.Bind("2. Generator", "generatorThermalMultiplier", 2, new ConfigDescription("Multiplies speed of thermal plants", new AcceptableValueRange<int>(1, 100)));
            _genFusionMultiplier = confFile.Bind("2. Generator", "generatorFusionMultiplier", 2, new ConfigDescription("Multiplies speed of fusion power plants", new AcceptableValueRange<int>(1, 100)));
            _genStarMultiplier = confFile.Bind("2. Generator", "generatorArtificialStarMultiplier", 2, new ConfigDescription("Multiplies speed of artificial stars", new AcceptableValueRange<int>(1, 100)));
            _genExchMultiplier = confFile.Bind("2. Generator", "generatorExchangerMultiplier", 2, new ConfigDescription("Multiplies speed of energy exchangers", new AcceptableValueRange<int>(1, 100)));
            keyTestMode = confFile.Bind("3. Advanced", "keyTestMode", false, "Uses alt+1 as keybind for scriptengine support");
            toggleOverclockKey = confFile.Bind("3. Advanced", "toggleOverclockKey", new KeyboardShortcut(KeyCode.KeypadMinus), "Key to toggle overclock");
            multiplierEnabled = confFile.Bind("3. Advanced", "multiplierEnabled", true, "Determine whether we are currently multiplying values");
            enableAssemblerPopupLogMessage = confFile.Bind("3. Advanced", "enableAssemblerPopupLogMessage", false, "Ignore - For debugging, log message when UI window is opened");
        }
        public static int GetMultiplierByRecipe(ERecipeType eRecipeType)
        {
            if (!multiplierEnabled.Value)
                return 1;
            switch (eRecipeType)
            {
                case ERecipeType.Assemble: return assembleMultiplier.Value;
                case ERecipeType.Chemical: return chemicalMultiplier.Value;
                case ERecipeType.Exchange: return 1;
                case ERecipeType.Fractionate: return fractionatorMultiplier;
                case ERecipeType.Particle: return particleMultiplier.Value;
                case ERecipeType.Refine: return refineMultiplier.Value;
                case ERecipeType.Research: return labMultiplier;
                case ERecipeType.Smelt: return smeltMultiplier.Value;
                case ERecipeType.PhotonStore: return gammaMultiplier.Value;
                default: return 1;
            }
        }
    }
    public static class ItemUtil
    {
        private static readonly ERecipeType[] _recipeByProtoId = new ERecipeType[15000];
        private static readonly bool[] _recipeCacheInit = new bool[15000];
        public static ERecipeType GetRecipeByProtoId(int protoId)
        {
            if (protoId < 0 || protoId >= 15000) return ERecipeType.None;
            if (_recipeCacheInit[protoId]) return _recipeByProtoId[protoId];
            var itemProto = LDB.items.Select(protoId);
            if (itemProto?.prefabDesc != null)
            {
                var type = itemProto.prefabDesc.assemblerRecipeType;
                _recipeByProtoId[protoId] = type;
                _recipeCacheInit[protoId] = true;
                return type;
            }
            return ERecipeType.None;
        }
        private static readonly bool[] _rayPhotonReceiverProtosArray = new bool[15000];
        private static bool _rayPhotonReceiverProtosInit = false;
        public static bool IsPhotonRayReceiver(int protoId)
        {
            if (protoId < 0 || protoId >= 15000) return false;
            if (!_rayPhotonReceiverProtosInit)
            {
                foreach (var item in LDB.items.dataArray)
                {
                    if (item.prefabDesc.gammaRayReceiver && item.ID >= 0 && item.ID < 15000)
                        _rayPhotonReceiverProtosArray[item.ID] = true;
                }
                _rayPhotonReceiverProtosInit = true;
            }
            return _rayPhotonReceiverProtosArray[protoId];
        }
        private static int _ejectorChargeFrame = -1;
        public static int EjectorChargeFrame
        {
            get
            {
                if (_ejectorChargeFrame == -1)
                {
                    var p = LDB.items.dataArray.FirstOrDefault(i => i.prefabDesc.isEjector);
                    _ejectorChargeFrame = p?.prefabDesc?.ejectorChargeFrame ?? 0;
                }
                return _ejectorChargeFrame;
            }
        }
        private static int _siloChargeFrame = -1;
        public static int SiloChargeFrame
        {
            get
            {
                if (_siloChargeFrame == -1)
                {
                    var p = LDB.items.dataArray.FirstOrDefault(i => i.prefabDesc.isSilo);
                    _siloChargeFrame = p?.prefabDesc?.siloChargeFrame ?? 0;
                }
                return _siloChargeFrame;
            }
        }
    }
    public static class BeltPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.NewBeltComponent))]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpgradeBeltComponent))]
        public static void Belt_Prefix(ref int speed)
        {
            speed *= PluginConfig.beltMultiplier;
        }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpdateSplitter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallOriginalUpdateSplitter(CargoTraffic instance, ref SplitterComponent sp) { throw new NotImplementedException(); }
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(PilerComponent), nameof(PilerComponent.InternalUpdate))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallOriginalPilerUpdate(ref PilerComponent instance, CargoTraffic _traffic, AnimData[] _animPool) { throw new NotImplementedException(); }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.UpdateSplitter))]
        public static void UpdateSplitter_Postfix(ref SplitterComponent sp, CargoTraffic __instance)
        {
            if (PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1)
            {
                for (int i = 0; i < PluginConfig.beltMultiplier - 1; i++)
                    CallOriginalUpdateSplitter(__instance, ref sp);
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PilerComponent), nameof(PilerComponent.InternalUpdate))]
        public static void Piler_InternalUpdate_Postfix(ref PilerComponent __instance, CargoTraffic _traffic, AnimData[] _animPool)
        {
            if (PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1)
            {
                __instance.cacheCdTick = 0;
                if (__instance.timeSpend < 10000) __instance.timeSpend = 10000;
                for (int i = 0; i < PluginConfig.beltMultiplier - 1; i++)
                {
                    CallOriginalPilerUpdate(ref __instance, _traffic, _animPool);
                    __instance.cacheCdTick = 0;
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static void Fractionator_InternalUpdate_Prefix(ref FractionatorComponent __instance, PlanetFactory factory)
        {
            if (!PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return;
            int multi = PluginConfig.beltMultiplier;
            var traffic = factory.cargoTraffic;
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.fluidInputCount >= __instance.fluidInputMax) break;
                if (__instance.belt1 > 0 && !__instance.isOutput1)
                {
                    if (__instance.fluidId > 0)
                    {
                        if (traffic.TryPickItemAtRear(__instance.belt1, __instance.fluidId, null, out byte stack, out byte inc) > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                        }
                    }
                    else
                    {
                        int needId = traffic.TryPickItemAtRear(__instance.belt1, 0, RecipeProto.fractionatorNeeds, out byte stack, out byte inc);
                        if (needId > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                            __instance.SetRecipe(needId, factory.entitySignPool);
                        }
                    }
                }
                if (__instance.belt2 > 0 && !__instance.isOutput2 && __instance.fluidInputCount < __instance.fluidInputMax)
                {
                    if (__instance.fluidId > 0)
                    {
                        if (traffic.TryPickItemAtRear(__instance.belt2, __instance.fluidId, null, out byte stack, out byte inc) > 0)
                        {
                            __instance.fluidInputCount += (int)stack;
                            __instance.fluidInputInc += (int)inc;
                            __instance.fluidInputCargoCount += 1f;
                        }
                    }
                }
            }
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static IEnumerable<CodeInstruction> Fractionator_InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R8 && (double)codes[i].operand == 30.0)
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.Method(typeof(BeltPatcher), nameof(GetFractionatorLimit));
                }
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 2 < codes.Count &&
                    codes[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                    codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand.ToString().Contains("TryInsertItemAtHead"))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.Method(typeof(BeltPatcher), nameof(GetBeltMultiplier));
                }
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 2 < codes.Count &&
                    codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].operand.ToString().Contains("TryUpdateItemAtHeadAndFillBlank"))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.Method(typeof(BeltPatcher), nameof(GetBeltMultiplier));
                }
            }
            return codes;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static void Fractionator_InternalUpdate_Postfix(ref FractionatorComponent __instance, PlanetFactory factory)
        {
            if (!PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return;
            var traffic = factory.cargoTraffic;
            int multi = PluginConfig.beltMultiplier;
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.productOutputCount <= 0) break;
                if (__instance.belt0 > 0 && __instance.isOutput0)
                {
                    if (traffic.TryInsertItemAtHead(__instance.belt0, __instance.productId, (byte)1, (byte)0))
                        __instance.productOutputCount--;
                    else break;
                }
                else break;
            }
            for (int i = 0; i < multi - 1; i++)
            {
                if (__instance.fluidOutputCount <= 0) break;
                int bId = __instance.belt1 > 0 && __instance.isOutput1 ? __instance.belt1 : (__instance.belt2 > 0 && __instance.isOutput2 ? __instance.belt2 : 0);
                if (bId == 0) break;
                var cp = traffic.GetCargoPath(traffic.beltPool[bId].segPathId);
                if (cp == null) break;
                int inc = __instance.fluidOutputInc / __instance.fluidOutputCount;
                if (cp.TryUpdateItemAtHeadAndFillBlank(__instance.fluidId, 4, (byte)1, (byte)inc))
                {
                    __instance.fluidOutputCount--;
                    __instance.fluidOutputInc -= inc;
                }
                else break;
            }
        }
        public static double GetFractionatorLimit()
        {
            return PluginConfig.multiplierEnabled.Value ? PluginConfig.beltMultiplier * 30.0 : 30.0;
        }
        public static int GetBeltMultiplier()
        {
            return PluginConfig.multiplierEnabled.Value ? PluginConfig.beltMultiplier : 1;
        }
        public static void SyncBelts(PlanetFactory factory)
        {
            var traffic = factory.cargoTraffic;
            if (traffic == null) return;
            int multi = PluginConfig.beltMultiplier;
            for (int i = 1; i < traffic.beltCursor; i++)
            {
                if (traffic.beltPool[i].id == i)
                {
                    int entityId = traffic.beltPool[i].entityId;
                    int protoId = factory.entityPool[entityId].protoId;
                    var beltProto = LDB.items.Select(protoId);
                    if (beltProto != null)
                    {
                        int s = beltProto.prefabDesc.beltSpeed * multi;
                        if (s > 10) s = 10; 
                        traffic.beltPool[i].speed = s;
                    }
                }
            }
            for (int i = 1; i < traffic.pathCursor; i++)
            {
                var path = traffic.pathPool[i];
                if (path != null && path.id == i && path.chunks != null)
                {
                    for (int j = 0; j < path.chunkCount; j++)
                    {
                        if (j * 3 + 2 >= path.chunks.Length) break;
                        int begin = path.chunks[j * 3];
                        int speed = 1;
                        if (path.belts != null)
                        {
                            int maxSegIndex = -1;
                            foreach (int bId in path.belts)
                            {
                                if (bId > 0 && bId < traffic.beltCursor)
                                {
                                    ref var belt = ref traffic.beltPool[bId];
                                    if (belt.id == bId && belt.segIndex <= begin && belt.segIndex > maxSegIndex)
                                    {
                                        maxSegIndex = belt.segIndex;
                                        speed = belt.speed;
                                    }
                                }
                            }
                        }
                        path.chunks[j * 3 + 2] = speed;
                    }
                }
            }
        }
    }
    public static class BuildingPatcher
    {
        private static readonly ConcurrentDictionary<int, int> _inserterDelayByProtoId = new();
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        public static void Ejector_Prefix(ref EjectorComponent __instance, ref float power)
        {
            int multi = PluginConfig.ejectorMultiplier;
            if (multi <= 1) return;
            power *= multi;
            var chargeFrame = ItemUtil.EjectorChargeFrame;
            if (chargeFrame <= 0) return;
            __instance.chargeSpend = chargeFrame * 10000 / multi;
            __instance.coldSpend = chargeFrame * 10000 / multi;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        public static void Silo_Prefix(ref SiloComponent __instance, ref float power)
        {
            int multi = PluginConfig.siloMultiplier;
            if (multi <= 1) return;
            power *= multi;
            var chargeFrame = ItemUtil.SiloChargeFrame;
            if (chargeFrame <= 0) return;
            __instance.chargeSpend = chargeFrame * 10000 / multi;
            __instance.coldSpend = chargeFrame * 10000 / multi;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate_Bidirectional))]
        public static void Inserter_InternalUpdate_Prefix(ref InserterComponent __instance, ref float power, PlanetFactory factory)
        {
            if (__instance.id == 0 || __instance.entityId == 0) return;
            int multi = PluginConfig.inserterMultiplier;
            if (multi <= 1) return;
            power *= multi;
            int protoId = factory.entityPool[__instance.entityId].protoId;
            if (!_inserterDelayByProtoId.TryGetValue(protoId, out int baseDelay))
            {
                var proto = LDB.items.Select(protoId);
                baseDelay = _inserterDelayByProtoId[protoId] = proto?.prefabDesc?.inserterDelay ?? 0;
            }
            if (baseDelay > 0) __instance.delay = baseDelay / multi;
            if (!__instance.bidirectional && __instance.stage == EInserterStage.Picking && __instance.itemId > 0)
                __instance.time += 10000 * (multi - 1);
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate_Bidirectional))]
        public static IEnumerable<CodeInstruction> Inserter_Bidirectional_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 1 < codes.Count &&
                    (codes[i + 1].opcode == OpCodes.Stloc_0 || codes[i + 1].opcode == OpCodes.Stloc_1 ||
                     codes[i + 1].opcode == OpCodes.Stloc_2 || codes[i + 1].opcode == OpCodes.Stloc_3))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.PropertyGetter(typeof(PluginConfig), nameof(PluginConfig.inserterMultiplier));
                }
            }
            return codes;
        }
    }
    public static class FactorySystemPatcher
    {
        private static readonly ConcurrentDictionary<int, int> _baseSpeedByProtoId = new();
        private static readonly ConcurrentDictionary<int, int> _labBaseSpeedByProtoId = new();
        private static readonly ConcurrentDictionary<int, int> _minerBaseSpeedByProtoId = new();
        public static void SyncAll(FactorySystem factorySystem)
        {
            SyncAssemblers(factorySystem);
            SyncLabs(factorySystem);
            SyncMiners(factorySystem);
        }
        public static void SyncMiners(FactorySystem factorySystem)
        {
            for (int i = 1; i < factorySystem.minerCursor; i++)
            {
                if (factorySystem.minerPool[i].id == i)
                    SyncMiner(ref factorySystem.minerPool[i], factorySystem.factory);
            }
        }
        public static void SyncMiner(ref MinerComponent miner, PlanetFactory factory)
        {
            int protoId = factory.entityPool[miner.entityId].protoId;
            int multi = PluginConfig.miningMultiplier.Value;
            if (!_minerBaseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
            {
                baseSpeed = 10000;
                _minerBaseSpeedByProtoId[protoId] = baseSpeed;
            }
            miner.speed = multi * baseSpeed;
        }
        public static void SyncLabs(FactorySystem factorySystem)
        {
            for (int i = 1; i < factorySystem.labCursor; i++)
            {
                if (factorySystem.labPool[i].id == i)
                    SyncLab(ref factorySystem.labPool[i], factorySystem.factory);
            }
        }
        public static void SyncLab(ref LabComponent lab, PlanetFactory factory)
        {
            int protoId = factory.entityPool[lab.entityId].protoId;
            int multi = PluginConfig.labMultiplier;
            if (!_labBaseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
            {
                var proto = LDB.items.Select(protoId);
                baseSpeed = _labBaseSpeedByProtoId[protoId] = proto?.prefabDesc?.labAssembleSpeed ?? 10000;
            }
            if (lab.replicating && lab.speed > 0)
            {
                double ratio = (double)lab.speedOverride / lab.speed;
                lab.speedOverride = (int)(ratio * multi * baseSpeed);
            }
            lab.speed = multi * baseSpeed;
        }
        public static void SyncAssemblers(FactorySystem factorySystem)
        {
            for (int i = 1; i < factorySystem.assemblerCursor; i++)
            {
                if (factorySystem.assemblerPool[i].id == i)
                    SyncAssembler(ref factorySystem.assemblerPool[i], factorySystem.factory);
            }
        }
        public static void SyncAssembler(ref AssemblerComponent assembler, PlanetFactory factory)
        {
            int protoId = factory.entityPool[assembler.entityId].protoId;
            if (!_baseSpeedByProtoId.TryGetValue(protoId, out int baseSpeed))
            {
                var proto = LDB.items.Select(protoId);
                baseSpeed = _baseSpeedByProtoId[protoId] = proto?.prefabDesc?.assemblerSpeed ?? 10000;
            }
            var recipeType = assembler.recipeId > 0 ? assembler.recipeType : ItemUtil.GetRecipeByProtoId(protoId);
            int multi = PluginConfig.GetMultiplierByRecipe(recipeType);
            if (assembler.replicating && assembler.speed > 0)
            {
                double ratio = (double)assembler.speedOverride / assembler.speed;
                assembler.speedOverride = (int)(ratio * multi * baseSpeed);
            }
            assembler.speed = multi * baseSpeed;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewAssemblerComponent))]
        public static void NewAssemblerComponent_Postfix(FactorySystem __instance, int __result)
        {
            if (__result > 0) SyncAssembler(ref __instance.assemblerPool[__result], __instance.factory);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewLabComponent))]
        public static void NewLabComponent_Postfix(FactorySystem __instance, int __result)
        {
            if (__result > 0) SyncLab(ref __instance.labPool[__result], __instance.factory);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewMinerComponent))]
        public static void NewMinerComponent_Postfix(FactorySystem __instance, int __result)
        {
            if (__result > 0) SyncMiner(ref __instance.minerPool[__result], __instance.factory);
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void MultiplyLabResearch(ref float research_speed)
        {
            if (PluginConfig.multiplierEnabled.Value && PluginConfig.labMultiplier > 1)
                research_speed *= PluginConfig.labMultiplier;
        }
    }
    public static class PowerConsumptionPatcher
    {
        public static void SyncPowerSystems(PowerSystem powerSystem)
        {
            for (int i = 1; i < powerSystem.consumerCursor; i++)
            {
                if (powerSystem.consumerPool[i].id == i)
                    SyncConsumer(ref powerSystem.consumerPool[i], powerSystem.factory);
            }
        }
        public static void SyncConsumer(ref PowerConsumerComponent consumer, PlanetFactory factory)
        {
            int entityId = consumer.entityId;
            if (entityId <= 0) return;
            var itemProto = LDB.items.Select(factory.entityPool[entityId].protoId);
            if (itemProto == null) return;
            var desc = itemProto.prefabDesc;
            if (itemProto.Type == EItemType.Logistics || desc.isStation || desc.isPowerExchanger || itemProto.ID == MultiplierPlugin.BattlefieldAnalysisBaseProtoId)
                return;
            int multi = 1;
            if (desc.isAssembler)
            {
                var recipe = ItemUtil.GetRecipeByProtoId(itemProto.ID);
                multi = PluginConfig.GetMultiplierByRecipe(recipe);
            }
            else if (desc.isLab) multi = PluginConfig.labMultiplier;
            else if (desc.minerType != EMinerType.None) multi = PluginConfig.miningMultiplier.Value;
            else if (desc.isTurret) multi = PluginConfig.turretMultiplier;
            else if (desc.isSilo) multi = PluginConfig.siloMultiplier;
            else if (desc.isFractionator) multi = PluginConfig.fractionatorMultiplier;
            else if (desc.isEjector) multi = PluginConfig.ejectorMultiplier;
            consumer.workEnergyPerTick = (long)(PluginConfig.drawMultiplier.Value * multi * desc.workEnergyPerTick);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.NewConsumerComponent))]
        public static void NewConsumerComponent_Postfix(PowerSystem __instance, int __result)
        {
            if (__result > 0)
                SyncConsumer(ref __instance.consumerPool[__result], __instance.factory);
        }
    }
    public static class PowerGenerationPatcher
    {
        public static void SyncGenerators(PowerSystem powerSystem)
        {
            for (int i = 1; i < powerSystem.genCursor; i++)
            {
                if (powerSystem.genPool[i].id == i)
                    SyncGenerator(ref powerSystem.genPool[i], powerSystem.factory);
            }
            for (int i = 1; i < powerSystem.excCursor; i++)
            {
                if (powerSystem.excPool[i].id == i)
                {
                    var proto = LDB.items.Select(powerSystem.factory.entityPool[powerSystem.excPool[i].entityId].protoId);
                    if (proto != null)
                        powerSystem.excPool[i].energyPerTick = proto.prefabDesc.exchangeEnergyPerTick * PluginConfig.genExchMultiplier;
                }
            }
        }
        public static void SyncGenerator(ref PowerGeneratorComponent gen, PlanetFactory factory)
        {
            var proto = LDB.items.Select(factory.entityPool[gen.entityId].protoId);
            if (proto == null) return;
            if (gen.photovoltaic) gen.genEnergyPerTick = proto.prefabDesc.genEnergyPerTick * PluginConfig.genSolarMultiplier;
            else if (gen.wind) gen.genEnergyPerTick = proto.prefabDesc.genEnergyPerTick * PluginConfig.genWindMultiplier;
            else if (gen.geothermal) gen.genEnergyPerTick = proto.prefabDesc.genEnergyPerTick * PluginConfig.genGeoMultiplier;
            else if (gen.gamma) gen.genEnergyPerTick = proto.prefabDesc.genEnergyPerTick * PluginConfig.gammaMultiplier.Value;
            else if (IsFuelConsumer(gen))
            {
                int multi = 1;
                if ((gen.fuelMask & 1) != 0) multi = PluginConfig.genThermalMultiplier;
                else if ((gen.fuelMask & 2) != 0) multi = PluginConfig.genFusionMultiplier;
                else if ((gen.fuelMask & 4) != 0) multi = PluginConfig.genStarMultiplier;
                gen.genEnergyPerTick = proto.prefabDesc.genEnergyPerTick * multi;
                gen.useFuelPerTick = proto.prefabDesc.useFuelPerTick * multi;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.NewGeneratorComponent))]
        public static void NewGeneratorComponent_Postfix(PowerSystem __instance, int __result)
        {
            if (__result > 0) SyncGenerator(ref __instance.genPool[__result], __instance.factory);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.NewExchangerComponent))]
        public static void NewExchangerComponent_Postfix(PowerSystem __instance, int __result)
        {
            if (__result > 0)
            {
                var proto = LDB.items.Select(__instance.factory.entityPool[__instance.excPool[__result].entityId].protoId);
                if (proto != null)
                    __instance.excPool[__result].energyPerTick = proto.prefabDesc.exchangeEnergyPerTick * PluginConfig.genExchMultiplier;
            }
        }
        private static bool IsFuelConsumer(PowerGeneratorComponent gen)
        {
            var fuelNeed = ItemProto.fuelNeeds[gen.fuelMask];
            return fuelNeed != null && fuelNeed.Length > 0;
        }
    }
    public static class StationPatcher
    {
        [ThreadStatic]
        private static bool _isLooping;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateOutputSlots))]
        public static bool UpdateOutputSlots_Prefix(StationComponent __instance, CargoTraffic traffic, SignData[] signPool, int maxPilerCount, bool active)
        {
            if (_isLooping || !PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return true;
            _isLooping = true;
            try
            {
                int multi = PluginConfig.beltMultiplier;
                for (int i = 0; i < multi; i++)
                {
                    __instance.UpdateOutputSlots(traffic, signPool, maxPilerCount, active);
                }
            }
            finally { _isLooping = false; }
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateInputSlots))]
        public static void UpdateInputSlots_Postfix(StationComponent __instance, CargoTraffic traffic, SignData[] signPool, bool active)
        {
            if (_isLooping) return;
            if (!PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return;
            _isLooping = true;
            try
            {
                int multi = PluginConfig.beltMultiplier;
                for (int i = 0; i < multi - 1; i++)
                    __instance.UpdateInputSlots(traffic, signPool, active);
            }
            finally { _isLooping = false; }
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateOutputSlots))]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateInputSlots))]
        public static IEnumerable<CodeInstruction> StationComponent_UpdateSlots_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Stfld &&
                    codes[i + 1].operand.ToString().Contains("counter"))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.Method(typeof(StationPatcher), nameof(GetSlotCounterValue));
                }
            }
            return codes;
        }
        public static int GetSlotCounterValue()
        {
            return PluginConfig.multiplierEnabled.Value && PluginConfig.beltMultiplier > 1 ? 0 : 1;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateCollection))]
        public static void UpdateCollection_Prefix(StationComponent __instance, ref float collectSpeedRate)
        {
            if (PluginConfig.multiplierEnabled.Value)
            {
                float baseRate = collectSpeedRate < 0 ? 0 : collectSpeedRate;
                float multi = PluginConfig.beltMultiplier;
                float targetRate = baseRate * multi;
                if (__instance.isCollector && __instance.collectionPerTick != null)
                {
                    float maxCollectionPerTick = 0f;
                    for (int i = 0; i < __instance.collectionPerTick.Length; i++)
                    {
                        if (__instance.collectionPerTick[i] > maxCollectionPerTick)
                            maxCollectionPerTick = __instance.collectionPerTick[i];
                    }
                    if (maxCollectionPerTick > 0.0001f)
                    {
                        float limitRate = 1000f / maxCollectionPerTick;
                        if (targetRate > limitRate)
                        {
                            targetRate = Math.Max(baseRate, limitRate);
                        }
                    }
                }
                if (targetRate > 1000000f) targetRate = 1000000f;
                collectSpeedRate = targetRate;
            }
        }
        public static class MoreMegaStructureCompat
        {
            private static FastInvokeHandler _updateOutputSlotsHandler;
            private static FastInvokeHandler _updateInputSlotsHandler;
            private static bool _handlersInitialized = false;
            private static void InitHandlers(object instance)
            {
                if (_handlersInitialized) return;
                var type = instance.GetType();
                var m3 = type.GetMethod("UpdateOutputSlots");
                if (m3 != null) _updateOutputSlotsHandler = MethodInvoker.GetHandler(m3);
                var m4 = type.GetMethod("UpdateInputSlots");
                if (m4 != null) _updateInputSlotsHandler = MethodInvoker.GetHandler(m4);
                _handlersInitialized = true;
            }
            [HarmonyPrefix]
            [HarmonyPatch("MoreMegaStructure.ExchangeStationComponent", "UpdateOutputSlots")]
            public static bool ExchangeStation_UpdateOutputSlots_Prefix(object __instance, CargoTraffic traffic, SignData[] signPool, int maxPilerCount, bool active)
            {
                if (_isLooping || !PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return true;
                _isLooping = true;
                InitHandlers(__instance);
                try
                {
                    if (_updateOutputSlotsHandler != null)
                    {
                        var args = new object[] { traffic, signPool, maxPilerCount, active };
                        for (int i = 0; i < PluginConfig.beltMultiplier; i++) _updateOutputSlotsHandler(__instance, args);
                    }
                }
                finally { _isLooping = false; }
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch("MoreMegaStructure.ExchangeStationComponent", "UpdateInputSlots")]
            public static bool ExchangeStation_UpdateInputSlots_Prefix(object __instance, CargoTraffic traffic, SignData[] signPool, int maxPilerCount, bool active)
            {
                if (_isLooping || !PluginConfig.multiplierEnabled.Value || PluginConfig.beltMultiplier <= 1) return true;
                _isLooping = true;
                InitHandlers(__instance);
                try
                {
                    if (_updateInputSlotsHandler != null)
                    {
                        var args = new object[] { traffic, signPool, maxPilerCount, active };
                        for (int i = 0; i < PluginConfig.beltMultiplier; i++) _updateInputSlotsHandler(__instance, args);
                    }
                }
                finally { _isLooping = false; }
                return false;
            }
            [HarmonyTranspiler]
            [HarmonyPatch("MoreMegaStructure.ExchangeStationComponent", "UpdateOutputSlots")]
            [HarmonyPatch("MoreMegaStructure.ExchangeStationComponent", "UpdateInputSlots")]
            [HarmonyPatch("MoreMegaStructure.ExchangeStationComponent", "UpdateSlots")]
            public static IEnumerable<CodeInstruction> MoreMegaStructure_UpdateSlots_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return StationComponent_UpdateSlots_Transpiler(instructions);
            }
        }
    }
    [HarmonyPatch(typeof(TurretComponent))]
    public static class TurretPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TurretComponent.InternalUpdate))]
        [HarmonyPatch(nameof(TurretComponent.Aim))]
        [HarmonyPatch(nameof(TurretComponent.Shoot))]
        public static void Turret_Prefix(ref float power)
        {
             if (PluginConfig.multiplierEnabled.Value)
                 power *= PluginConfig.turretMultiplier;
        }
    }
}