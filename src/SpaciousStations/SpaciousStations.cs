using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
using UnityEngine;
using CommonAPI;
using CommonAPI.Systems;

namespace SpaciousStations
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    public class SpaciousStationsPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.SpaciousStations";
        public const string MOD_NAME = "SpaciousStations";
        public const string MOD_VERSION = "1.1.0";

        public static ConfigEntry<float> PLS_DroneMultiplier;
        public static ConfigEntry<float> PLS_ShipMultiplier;
        public static ConfigEntry<float> PLS_StorageMultiplier;
        public static ConfigEntry<float> PLS_ChargeMultiplier;
        public static ConfigEntry<float> PLS_EnergyMultiplier;

        public static ConfigEntry<float> ILS_DroneMultiplier;
        public static ConfigEntry<float> ILS_ShipMultiplier;
        public static ConfigEntry<float> ILS_StorageMultiplier;
        public static ConfigEntry<float> ILS_ChargeMultiplier;
        public static ConfigEntry<float> ILS_EnergyMultiplier;

        public static ConfigEntry<int> DroneTaskInterval;
        public static ConfigEntry<int> ShipTaskInterval;
        public static ConfigEntry<int> ILS_ShipReleasePerTick;

        public static ConfigEntry<float> InternalLastStorageMultiplier;
        public static ConfigEntry<float> InternalLastChargeMultiplier;

        private void Awake()
        {
            PLS_DroneMultiplier = Config.Bind("Planetary Logistics Station", "DroneMultiplier", 2f, "Multiplies max number of drones in a PLS.");
            PLS_ShipMultiplier = Config.Bind("Planetary Logistics Station", "ShipMultiplier", 2f, "Multiplies max number of ships in a PLS.");
            PLS_StorageMultiplier = Config.Bind("Planetary Logistics Station", "StorageMultiplier", 2f, "Multiplies maximum amount of items in a PLS.");
            PLS_ChargeMultiplier = Config.Bind("Planetary Logistics Station", "ChargeMultiplier", 2f, "Multiplies station's charge power for PLS.");
            PLS_EnergyMultiplier = Config.Bind("Planetary Logistics Station", "EnergyMultiplier", 2f, "Multiplies station's max energy storage for PLS.");

            ILS_DroneMultiplier = Config.Bind("Interstellar Logistics Station", "DroneMultiplier", 2f, "Multiplies max number of drones in an ILS.");
            ILS_ShipMultiplier = Config.Bind("Interstellar Logistics Station", "ShipMultiplier", 2f, "Multiplies max number of ships in an ILS.");
            ILS_StorageMultiplier = Config.Bind("Interstellar Logistics Station", "StorageMultiplier", 2f, "Multiplies maximum amount of items in an ILS.");
            ILS_ChargeMultiplier = Config.Bind("Interstellar Logistics Station", "ChargeMultiplier", 2f, "Multiplies station's charge power for ILS.");
            ILS_EnergyMultiplier = Config.Bind("Interstellar Logistics Station", "EnergyMultiplier", 2f, "Multiplies station's max energy storage for ILS.");
            ILS_ShipReleasePerTick = Config.Bind("Interstellar Logistics Station", "ShipReleasePerTick", 1, "Maximum number of ships that can be dispatched from a single ILS in a single tick (when it's their turn to dispatch). Vanilla is 1.");

            DroneTaskInterval = Config.Bind("General", "DroneTaskInterval", 20, "The interval between drone dispatches. Lower is faster. Vanilla default is 20 (3 dispatches per second). Setting this to 1 will dispatch drones every tick (60 per second).");
            ShipTaskInterval = Config.Bind("General", "ShipTaskInterval", 10, "The interval between vessel dispatches for high priority items. Lower is faster. Vanilla default is 10 (6 dispatches per second). Setting this to 1 will dispatch vessels every tick (60 per second). Note: other priority items use 3x and 6x this interval.");

            InternalLastStorageMultiplier = Config.Bind("Internal", "LastStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastChargeMultiplier = Config.Bind("Internal", "LastChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));

            Log.Init(Logger);
            SyncConfigToService();

            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StationPatch));

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }

        private void SyncConfigToService()
        {
            MultiplierService.SetMultiplier("Station_PLS_Drone", PLS_DroneMultiplier.Value);
            MultiplierService.SetMultiplier("Station_PLS_Ship", PLS_ShipMultiplier.Value);
            MultiplierService.SetMultiplier("Station_PLS_Storage", PLS_StorageMultiplier.Value);
            MultiplierService.SetMultiplier("Station_PLS_Charge", PLS_ChargeMultiplier.Value);
            MultiplierService.SetMultiplier("Station_PLS_Energy", PLS_EnergyMultiplier.Value);

            MultiplierService.SetMultiplier("Station_ILS_Drone", ILS_DroneMultiplier.Value);
            MultiplierService.SetMultiplier("Station_ILS_Ship", ILS_ShipMultiplier.Value);
            MultiplierService.SetMultiplier("Station_ILS_Storage", ILS_StorageMultiplier.Value);
            MultiplierService.SetMultiplier("Station_ILS_Charge", ILS_ChargeMultiplier.Value);
            MultiplierService.SetMultiplier("Station_ILS_Energy", ILS_EnergyMultiplier.Value);
            
            MultiplierService.CommitChanges();
        }

        // Backward compatibility / simplified access
        public static float GetStorageMultiplier(bool isStellar) => isStellar ? ILS_StorageMultiplier.Value : PLS_StorageMultiplier.Value;
    }

    /// <summary>
    /// Class to control visibility in Configuration Manager
    /// </summary>
    public class ConfigurationManagerAttributes
    {
        public bool? Browsable;
        public bool? IsAdvanced;
    }

    public static class StationPatch
    {
        private static Dictionary<int, ProtoValues> _originalValues = new Dictionary<int, ProtoValues>();

        private struct ProtoValues
        {
            public int DroneCount;
            public int ShipCount;
            public int ItemCount;
            public long EnergyMax;
            public long EnergyPerTick;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            Log.Info("Performing final pass on prototypes for SpaciousStations...");
            if (LDB.items != null)
            {
                foreach (var item in LDB.items.dataArray)
                {
                    if (item == null || item.prefabDesc == null || !item.prefabDesc.isStation) continue;
                    ApplyToItem(item);
                }
            }
            if (LDB.models != null)
            {
                foreach (var model in LDB.models.dataArray)
                {
                    if (model == null || model.prefabDesc == null || !model.prefabDesc.isStation) continue;
                    ApplyToModel(model);
                }
            }
        }

        private static void ApplyToItem(ItemProto item)
        {
            if (item == null || item.prefabDesc == null || !item.prefabDesc.isStation) return;
            
            if (!_originalValues.ContainsKey(item.ID))
            {
                _originalValues[item.ID] = new ProtoValues
                {
                    DroneCount = item.prefabDesc.stationMaxDroneCount,
                    ShipCount = item.prefabDesc.stationMaxShipCount,
                    ItemCount = item.prefabDesc.stationMaxItemCount,
                    EnergyMax = item.prefabDesc.stationMaxEnergyAcc,
                    EnergyPerTick = item.prefabDesc.workEnergyPerTick
                };
            }
            
            ApplyToDesc(item.prefabDesc, _originalValues[item.ID]);
        }

        private static void ApplyToModel(ModelProto model)
        {
            if (model == null || model.prefabDesc == null || !model.prefabDesc.isStation) return;
            
            ItemProto item = null;
            if (LDB.items != null)
            {
                foreach (var i in LDB.items.dataArray) 
                { 
                    if (i != null && i.ModelIndex == model.ID) { item = i; break; } 
                }
            }
            
            if (item != null)
            {
                if (!_originalValues.ContainsKey(item.ID))
                {
                    _originalValues[item.ID] = new ProtoValues
                    {
                        DroneCount = item.prefabDesc.stationMaxDroneCount,
                        ShipCount = item.prefabDesc.stationMaxShipCount,
                        ItemCount = item.prefabDesc.stationMaxItemCount,
                        EnergyMax = item.prefabDesc.stationMaxEnergyAcc,
                        EnergyPerTick = item.prefabDesc.workEnergyPerTick
                    };
                }
                ApplyToDesc(model.prefabDesc, _originalValues[item.ID]);
            }
        }

        private static void ApplyToDesc(PrefabDesc desc, ProtoValues original)
        {
            if (desc == null) return;
            
            float droneMul, shipMul, storageMul, energyMul, chargeMul;
            if (desc.isStellarStation)
            {
                droneMul = MultiplierService.GetMultiplier("Station_ILS_Drone", 1f);
                shipMul = MultiplierService.GetMultiplier("Station_ILS_Ship", 1f);
                storageMul = MultiplierService.GetMultiplier("Station_ILS_Storage", 1f);
                energyMul = MultiplierService.GetMultiplier("Station_ILS_Energy", 1f);
                chargeMul = MultiplierService.GetMultiplier("Station_ILS_Charge", 1f);
            }
            else
            {
                droneMul = MultiplierService.GetMultiplier("Station_PLS_Drone", 1f);
                shipMul = MultiplierService.GetMultiplier("Station_PLS_Ship", 1f);
                storageMul = MultiplierService.GetMultiplier("Station_PLS_Storage", 1f);
                energyMul = MultiplierService.GetMultiplier("Station_PLS_Energy", 1f);
                chargeMul = MultiplierService.GetMultiplier("Station_PLS_Charge", 1f);
            }

            desc.stationMaxDroneCount = (int)(original.DroneCount * droneMul);
            desc.stationMaxShipCount = (int)(original.ShipCount * shipMul);
            desc.stationMaxItemCount = (int)(original.ItemCount * storageMul);
            desc.stationMaxEnergyAcc = (long)(original.EnergyMax * energyMul);
            if (!desc.isCollectStation)
                desc.workEnergyPerTick = (long)(original.EnergyPerTick * chargeMul);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
        public static void StationComponent_InternalTickLocal_Prefix(StationComponent __instance)
        {
            __instance.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineFramingDispatchTime))]
        public static bool StationComponent_DetermineFramingDispatchTime_Prefix(long time, int priorityIndex, ref bool __result)
        {
            int interval = SpaciousStationsPlugin.ShipTaskInterval.Value;
            if (priorityIndex == 1)
                __result = time % (long)interval == 0L;
            else if (priorityIndex == 2 || priorityIndex == 3)
                __result = time % (long)(interval * 3) == 0L;
            else
                __result = time % (long)(interval * 6) == 0L;
            return false;
        }

        private static int _shipsReleasedThisTick = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        public static void StationComponent_DetermineDispatch_Prefix()
        {
            _shipsReleasedThisTick = 0;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        public static IEnumerable<CodeInstruction> DetermineDispatch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var supplyMethod = AccessTools.Method(typeof(StationComponent), "DispatchSupplyShip");
            var demandMethod = AccessTools.Method(typeof(StationComponent), "DispatchDemandShip");

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && (codes[i].operand as MethodInfo == supplyMethod || codes[i].operand as MethodInfo == demandMethod))
                {
                    for (int j = i + 1; j < i + 20 && j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Br)
                        {
                            var label = codes[j].operand;
                            codes[j].opcode = OpCodes.Call;
                            codes[j].operand = AccessTools.Method(typeof(StationPatch), nameof(StationPatch.ShouldBreakShipDispatch));
                            codes.Insert(j + 1, new CodeInstruction(OpCodes.Brtrue, label));
                            
                            i = j + 1;
                            break;
                        }
                    }
                }
            }
            return codes;
        }

        public static bool ShouldBreakShipDispatch()
        {
            _shipsReleasedThisTick++;
            return _shipsReleasedThisTick >= SpaciousStationsPlugin.ILS_ShipReleasePerTick.Value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.Import))]
        public static void StationComponent_Import_Postfix(StationComponent __instance)
        {
            if (__instance == null || __instance.id <= 0 || __instance.entityId <= 0 || GameMain.data == null) return;
            
            PlanetFactory factory = null;
            if (GameMain.data.factories != null)
            {
                foreach (var f in GameMain.data.factories)
                {
                    if (f != null && f.planetId == __instance.planetId) { factory = f; break; }
                }
            }
            if (factory == null) return;

            int protoId = factory.entityPool[__instance.entityId].protoId;
            var itemProto = LDB.items.Select(protoId);
            if (itemProto == null || !_originalValues.TryGetValue(itemProto.ID, out var original)) return;

            var desc = itemProto.prefabDesc;
            if (desc == null) return;

            __instance.PatchDroneArray(desc.stationMaxDroneCount);
            __instance.energyMax = desc.stationMaxEnergyAcc;
            __instance.energyPerTick = desc.workEnergyPerTick;
            __instance.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;

            if (__instance.storage != null)
            {
                float storageMul = desc.isStellarStation ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                int vanillaExtra = GetVanillaAdditionStorage(__instance);
                int vanillaMax = original.ItemCount + vanillaExtra;
                int prevMax = (int)(vanillaMax * SpaciousStationsPlugin.InternalLastStorageMultiplier.Value);
                int newMax = desc.stationMaxItemCount + (int)(vanillaExtra * storageMul);

                for (int i = 0; i < __instance.storage.Length; i++)
                {
                    if (__instance.storage[i].itemId > 0)
                    {
                        if (__instance.storage[i].max == vanillaMax || __instance.storage[i].max == prevMax || __instance.storage[i].max > newMax || (SpaciousStationsPlugin.InternalLastStorageMultiplier.Value != storageMul && __instance.storage[i].max == prevMax))
                            __instance.storage[i].max = newMax;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.Import))]
        public static void PowerSystem_Import_Postfix(PowerSystem __instance)
        {
            if (__instance == null || __instance.factory == null || __instance.factory.transport == null) return;

            foreach (var station in __instance.factory.transport.stationPool)
            {
                if (station == null || station.id <= 0 || station.entityId <= 0 || station.pcId <= 0) continue;
                if (station.pcId >= __instance.consumerCursor) continue;

                int protoId = __instance.factory.entityPool[station.entityId].protoId;
                var itemProto = LDB.items.Select(protoId);
                if (itemProto == null || !_originalValues.TryGetValue(itemProto.ID, out var original)) continue;

                var desc = itemProto.prefabDesc;
                if (desc == null || desc.isCollectStation) continue;

                float chargeMul = desc.isStellarStation ? MultiplierService.GetMultiplier("Station_ILS_Charge") : MultiplierService.GetMultiplier("Station_PLS_Charge");
                long currentCharge = __instance.consumerPool[station.pcId].workEnergyPerTick;
                long vanillaMax = original.EnergyPerTick;
                long prevMax = (long)(vanillaMax * SpaciousStationsPlugin.InternalLastChargeMultiplier.Value);
                long newMax = desc.workEnergyPerTick;

                if (currentCharge == vanillaMax || currentCharge == prevMax || currentCharge > newMax || (SpaciousStationsPlugin.InternalLastChargeMultiplier.Value != chargeMul && currentCharge == prevMax))
                    __instance.consumerPool[station.pcId].workEnergyPerTick = newMax;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            if (LDB.items != null) foreach (var item in LDB.items.dataArray) ApplyToItem(item);
            
            float currStoreMul = MultiplierService.GetMultiplier("Station_ILS_Storage");
            float currChargeMul = MultiplierService.GetMultiplier("Station_ILS_Charge");
            SpaciousStationsPlugin.InternalLastStorageMultiplier.Value = currStoreMul;
            SpaciousStationsPlugin.InternalLastChargeMultiplier.Value = currChargeMul;
            
            Log.Info("GameMain.Begin: Station limits verified.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        public static void UIStationWindow_UIUpdate_Postfix(UIStationWindow __instance)
        {
            if (__instance.stationId > 0 && __instance.transport != null && __instance.maxChargePowerSlider != null)
            {
                var station = __instance.transport.stationPool[__instance.stationId];
                if (station != null && station.entityId > 0 && __instance.factory != null)
                {
                    ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[station.entityId].protoId);
                    if (itemProto != null && itemProto.prefabDesc != null)
                    {
                        long maxFromProto = itemProto.prefabDesc.workEnergyPerTick * 5L;
                        __instance.maxChargePowerSlider.maxValue = (float)(maxFromProto / 50000L);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "_OnOpen")]
        [HarmonyPatch(typeof(UIControlPanelStationInspector), "OnStationIdChange")]
        public static void UIControlPanelStationInspector_UIUpdate_Postfix(UIControlPanelStationInspector __instance)
        {
            if (__instance.station != null && __instance.maxChargePowerSlider != null)
            {
                ItemProto itemProto = null;
                if (__instance.station.entityId > 0 && __instance.factory != null)
                {
                    itemProto = LDB.items.Select(__instance.factory.entityPool[__instance.station.entityId].protoId);
                }

                if (itemProto != null && itemProto.prefabDesc != null)
                {
                    long maxFromProto = itemProto.prefabDesc.workEnergyPerTick * 5L;
                    __instance.maxChargePowerSlider.maxValue = (float)(maxFromProto / 50000L);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.GetAdditionStorage))]
        public static void UIStationStorage_GetAdditionStorage_Postfix(UIStationStorage __instance, ref int __result)
        {
            if (__instance.station != null)
            {
                float storageMul = __instance.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                __result = (int)(__result * storageMul);
            }
            else
            {
                __result = (int)(__result * MultiplierService.GetMultiplier("Station_ILS_Storage"));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.GetAdditionStorage))]
        public static void UIControlPanelStationStorage_GetAdditionStorage_Postfix(UIControlPanelStationStorage __instance, ref int __result)
        {
             if (__instance.masterInspector != null && __instance.masterInspector.station != null)
             {
                 float storageMul = __instance.masterInspector.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                 __result = (int)(__result * storageMul);
             }
             else
             {
                 __result = (int)(__result * MultiplierService.GetMultiplier("Station_ILS_Storage"));
             }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.NewStationComponent))]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityFastFillIn))]
        public static IEnumerable<CodeInstruction> ExtraStorage_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ExtraStorage_Transpiler_Worker(instructions);
        }

        private static IEnumerable<CodeInstruction> ExtraStorage_Transpiler_Worker(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var localField = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.localStationExtraStorage));
            var remoteField = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.remoteStationExtraStorage));

            if (localField == null || remoteField == null) return codes;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand as FieldInfo == localField)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StationPatch), nameof(StationPatch.get_PLS_StorageMultiplierValue))));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_I4));
                    i += 4;
                }
                else if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand as FieldInfo == remoteField)
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StationPatch), nameof(StationPatch.get_ILS_StorageMultiplierValue))));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_I4));
                    i += 4;
                }
            }
            return codes;
        }

        public static float get_PLS_StorageMultiplierValue() => MultiplierService.GetMultiplier("Station_PLS_Storage");
        public static float get_ILS_StorageMultiplierValue() => MultiplierService.GetMultiplier("Station_ILS_Storage");

        private static int GetVanillaAdditionStorage(StationComponent station)
        {
            if (station == null || GameMain.history == null) return 0;
            return !station.isCollector ? (!station.isVeinCollector ? (!station.isStellar ? GameMain.history.localStationExtraStorage : GameMain.history.remoteStationExtraStorage) : GameMain.history.localStationExtraStorage) : GameMain.history.localStationExtraStorage;
        }
    }
}
