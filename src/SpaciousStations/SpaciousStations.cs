using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
        public const string MOD_VERSION = "1.2.5";
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
        public static ConfigEntry<int> ILS_ShipReleasePerTick;
        public static ConfigEntry<float> EXC_DroneMultiplier;
        public static ConfigEntry<float> EXC_ShipMultiplier;
        public static ConfigEntry<float> EXC_StorageMultiplier;
        public static ConfigEntry<float> EXC_ChargeMultiplier;
        public static ConfigEntry<float> EXC_EnergyMultiplier;
        public static ConfigEntry<float> EXC_InternalsMultiplier;
        public static ConfigEntry<int> DroneTaskInterval;
        public static ConfigEntry<int> ShipTaskInterval;
        public static ConfigEntry<float> InternalLastStorageMultiplier;
        public static ConfigEntry<float> InternalLastChargeMultiplier;
        public static ConfigEntry<float> InternalLastPLSStorageMultiplier;
        public static ConfigEntry<float> InternalLastPLSChargeMultiplier;
        public static ConfigEntry<float> InternalLastEXCStorageMultiplier;
        public static ConfigEntry<float> InternalLastEXCChargeMultiplier;
        public static ConfigEntry<float> DroneCarryMultiplier;
        public static ConfigEntry<float> ShipCarryMultiplier;
        public static ConfigEntry<float> CourierCarryMultiplier;
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
            EXC_DroneMultiplier = Config.Bind("Megastructures Exchange Station", "DroneMultiplier", 2f, "Multiplies max number of drones in an Exchange Station.");
            EXC_ShipMultiplier = Config.Bind("Megastructures Exchange Station", "ShipMultiplier", 2f, "Multiplies max number of ships in an Exchange Station.");
            EXC_StorageMultiplier = Config.Bind("Megastructures Exchange Station", "StorageMultiplier", 2f, "Multiplies maximum amount of items in an Exchange Station.");
            EXC_ChargeMultiplier = Config.Bind("Megastructures Exchange Station", "ChargeMultiplier", 2f, "Multiplies station's charge power for Exchange Station.");
            EXC_EnergyMultiplier = Config.Bind("Megastructures Exchange Station", "EnergyMultiplier", 2f, "Multiplies station's max energy storage for Exchange Station.");
            EXC_InternalsMultiplier = Config.Bind("Megastructures Exchange Station", "InternalsMultiplier", 10f, "Multiplies the internal storage capacity of the Interstellar Assembly (Assembly Nexus). Vanilla is 99,999.");
            DroneTaskInterval = Config.Bind("General", "DroneTaskInterval", 20, "The interval between drone dispatches. Lower is faster. Vanilla default is 20 (3 dispatches per second). Setting this to 1 will dispatch drones every tick (60 per second).");
            ShipTaskInterval = Config.Bind("General", "ShipTaskInterval", 10, "The interval between vessel dispatches for high priority items. Lower is faster. Vanilla default is 10 (6 dispatches per second). Setting this to 1 will dispatch vessels every tick (60 per second). Note: other priority items use 3x and 6x this interval.");
            InternalLastStorageMultiplier = Config.Bind("Internal", "LastStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastChargeMultiplier = Config.Bind("Internal", "LastChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastPLSStorageMultiplier = Config.Bind("Internal", "LastPLSStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastPLSChargeMultiplier = Config.Bind("Internal", "LastPLSChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastEXCStorageMultiplier = Config.Bind("Internal", "LastEXCStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastEXCChargeMultiplier = Config.Bind("Internal", "LastEXCChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            DroneCarryMultiplier = Config.Bind("General", "DroneCarryMultiplier", 1f, "Multiplies the carrying capacity of logistics drones.");
            ShipCarryMultiplier = Config.Bind("General", "ShipCarryMultiplier", 1f, "Multiplies the carrying capacity of logistics vessels.");
            CourierCarryMultiplier = Config.Bind("General", "CourierCarryMultiplier", 1f, "Multiplies the carrying capacity of logistics couriers.");
            PLS_DroneMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            PLS_ShipMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            PLS_StorageMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            PLS_ChargeMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            PLS_EnergyMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            ILS_DroneMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            ILS_ShipMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            ILS_StorageMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            ILS_ChargeMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            ILS_EnergyMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            EXC_DroneMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            EXC_ShipMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            EXC_StorageMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            EXC_ChargeMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
            EXC_EnergyMultiplier.SettingChanged += (s, e) => { SyncConfigToService(); StationPatch.UpdateAllStations(); };
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
            MultiplierService.SetMultiplier("Station_EXC_Drone", EXC_DroneMultiplier.Value);
            MultiplierService.SetMultiplier("Station_EXC_Ship", EXC_ShipMultiplier.Value);
            MultiplierService.SetMultiplier("Station_EXC_Storage", EXC_StorageMultiplier.Value);
            MultiplierService.SetMultiplier("Station_EXC_Charge", EXC_ChargeMultiplier.Value);
            MultiplierService.SetMultiplier("Station_EXC_Energy", EXC_EnergyMultiplier.Value);
            MultiplierService.SetMultiplier("Carry_Drone", DroneCarryMultiplier.Value);
            MultiplierService.SetMultiplier("Carry_Ship", ShipCarryMultiplier.Value);
            MultiplierService.SetMultiplier("Carry_Courier", CourierCarryMultiplier.Value);
            MultiplierService.CommitChanges();
        }
        public static float GetStorageMultiplier(bool isStellar) => isStellar ? ILS_StorageMultiplier.Value : PLS_StorageMultiplier.Value;
        public static int GetMultipliedDroneCarry(int vanillaValue) => (int)(vanillaValue * DroneCarryMultiplier.Value);
        public static int GetMultipliedShipCarry(int vanillaValue) => (int)(vanillaValue * ShipCarryMultiplier.Value);
        public static int GetMultipliedCourierCarry(int vanillaValue) => (int)(vanillaValue * CourierCarryMultiplier.Value);
    }
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
            ApplyToDesc(item.prefabDesc, _originalValues[item.ID], item);
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
                ApplyToDesc(model.prefabDesc, _originalValues[item.ID], item);
            }
        }
        private static void ApplyToDesc(PrefabDesc desc, ProtoValues original, ItemProto item = null)
        {
            if (desc == null) return;
            float droneMul, shipMul, storageMul, energyMul, chargeMul;
            bool isExchangeStation = item != null && (
                item.ID >= 9400 ||
                (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
            );
            if (isExchangeStation)
            {
                droneMul = MultiplierService.GetMultiplier("Station_EXC_Drone", 1f);
                shipMul = MultiplierService.GetMultiplier("Station_EXC_Ship", 1f);
                storageMul = MultiplierService.GetMultiplier("Station_EXC_Storage", 1f);
                energyMul = MultiplierService.GetMultiplier("Station_EXC_Energy", 1f);
                chargeMul = MultiplierService.GetMultiplier("Station_EXC_Charge", 1f);
            }
            else if (desc.isStellarStation)
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
            if (isExchangeStation)
                desc.stationMaxShipCount = (int)(original.ShipCount * shipMul);
            else if (desc.isStellarStation)
                desc.stationMaxShipCount = Math.Min(50, (int)(original.ShipCount * shipMul));
            else
                desc.stationMaxShipCount = original.ShipCount;
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
        public static void StationComponent_DetermineDispatch_Safety_Prefix(StationComponent __instance)
        {
            _shipsReleasedThisTick = 0;
            int total = __instance.idleShipCount + __instance.workShipCount;
            if (total > __instance.workShipDatas.Length)
            {
                __instance.PatchShipArray(total + 10);
            }
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
            if (itemProto.IsExchangeStation() || (desc.isStellarStation && desc.stationMaxShipCount > 10))
                __instance.PatchShipArray(desc.stationMaxShipCount);
            __instance.energyMax = desc.stationMaxEnergyAcc;
            __instance.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            UpdateAllStations();
        }
        public static void UpdateAllStations()
        {
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
            if (GameMain.data?.factories != null)
            {
                foreach (var factory in GameMain.data.factories)
                {
                    if (factory?.transport?.stationPool == null) continue;
                    foreach (var station in factory.transport.stationPool)
                    {
                        if (station == null || station.id <= 0 || station.entityId <= 0) continue;
                        int protoId = factory.entityPool[station.entityId].protoId;
                        var itemProto = LDB.items.Select(protoId);
                        if (itemProto?.prefabDesc == null || !_originalValues.TryGetValue(itemProto.ID, out var original)) continue;
                        var desc = itemProto.prefabDesc;
                        station.PatchDroneArray(desc.stationMaxDroneCount);
                        if (itemProto.IsExchangeStation() || (desc.isStellarStation && desc.stationMaxShipCount > 10))
                            station.PatchShipArray(desc.stationMaxShipCount);
                        station.energyMax = desc.stationMaxEnergyAcc;
                        station.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;
                        if (station.storage != null)
                        {
                            bool isExchange = itemProto.IsExchangeStation();
                            float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                               desc.isStellarStation ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                            float lastMul = isExchange ? SpaciousStationsPlugin.InternalLastEXCStorageMultiplier.Value :
                                            desc.isStellarStation ? SpaciousStationsPlugin.InternalLastStorageMultiplier.Value : SpaciousStationsPlugin.InternalLastPLSStorageMultiplier.Value;
                            int vanillaExtra = GetVanillaAdditionStorage(station);
                            int vanillaMax = original.ItemCount + vanillaExtra;
                            int newMax = (int)(original.ItemCount * storageMul) + (int)(vanillaExtra * storageMul);
                            bool multiplierChanged = Math.Abs(lastMul - storageMul) > 0.001f;
                            for (int i = 0; i < station.storage.Length; i++)
                            {
                                if (station.storage[i].itemId > 0)
                                {
                                    if (multiplierChanged)
                                    {
                                        int oldMax = (int)(vanillaMax * lastMul);
                                        if (oldMax > 0)
                                            station.storage[i].max = Math.Max(1, (int)((float)station.storage[i].max / oldMax * newMax));
                                        else
                                            station.storage[i].max = newMax;
                                    }
                                    else if (station.storage[i].max > newMax)
                                    {
                                        station.storage[i].max = newMax;
                                    }
                                }
                            }
                        }
                        if (!desc.isCollectStation && station.pcId > 0 && factory.powerSystem != null
                            && station.pcId < factory.powerSystem.consumerCursor)
                        {
                            bool isExchange = itemProto.IsExchangeStation();
                            float chargeMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Charge") :
                                              desc.isStellarStation ? MultiplierService.GetMultiplier("Station_ILS_Charge") : MultiplierService.GetMultiplier("Station_PLS_Charge");
                            float lastChgMul = isExchange ? SpaciousStationsPlugin.InternalLastEXCChargeMultiplier.Value :
                                               desc.isStellarStation ? SpaciousStationsPlugin.InternalLastChargeMultiplier.Value : SpaciousStationsPlugin.InternalLastPLSChargeMultiplier.Value;
                            bool chargeChanged = Math.Abs(lastChgMul - chargeMul) > 0.001f;
                            long currentChargePwr = factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick;
                            long currentChargeStn = station.energyPerTick;
                            long maxAllowed = desc.workEnergyPerTick * 5L;
                            if (chargeChanged && lastChgMul > 0.001f)
                            {
                                float ratio = chargeMul / lastChgMul;
                                factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = Math.Max(1L, (long)(currentChargePwr * ratio));
                                station.energyPerTick = Math.Max(1L, (long)(currentChargeStn * ratio));
                            }
                            if (factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick > maxAllowed)
                                factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = maxAllowed;
                            if (station.energyPerTick > maxAllowed)
                                station.energyPerTick = maxAllowed;
                        }
                    }
                }
            }
            SpaciousStationsPlugin.InternalLastStorageMultiplier.Value = MultiplierService.GetMultiplier("Station_ILS_Storage");
            SpaciousStationsPlugin.InternalLastChargeMultiplier.Value = MultiplierService.GetMultiplier("Station_ILS_Charge");
            SpaciousStationsPlugin.InternalLastPLSStorageMultiplier.Value = MultiplierService.GetMultiplier("Station_PLS_Storage");
            SpaciousStationsPlugin.InternalLastPLSChargeMultiplier.Value = MultiplierService.GetMultiplier("Station_PLS_Charge");
            SpaciousStationsPlugin.InternalLastEXCStorageMultiplier.Value = MultiplierService.GetMultiplier("Station_EXC_Storage");
            SpaciousStationsPlugin.InternalLastEXCChargeMultiplier.Value = MultiplierService.GetMultiplier("Station_EXC_Charge");
            Log.Info("SpaciousStations: All station limits updated.");
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.UnlockTechFunction))]
        public static void GameHistoryData_UnlockTechFunction_Postfix()
        {
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
            if (__instance.station != null && __instance.stationWindow != null && __instance.stationWindow.factory != null)
            {
                ItemProto item = LDB.items.Select(__instance.stationWindow.factory.entityPool[__instance.station.entityId].protoId);
                bool isExchange = item != null && (
                    item.ID >= 9400 ||
                    (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
                );
                float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                   __instance.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                __result = (int)(__result * storageMul);
            }
            else if (__instance.station != null)
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
             if (__instance.masterInspector != null && __instance.masterInspector.station != null && __instance.masterInspector.factory != null)
             {
                 ItemProto item = LDB.items.Select(__instance.masterInspector.factory.entityPool[__instance.masterInspector.station.entityId].protoId);
                 bool isExchange = item != null && (
                     item.ID >= 9400 ||
                     (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                     item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
                 );
                 float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                    __instance.masterInspector.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                 __result = (int)(__result * storageMul);
             }
             else if (__instance.masterInspector != null && __instance.masterInspector.station != null)
             {
                 float storageMul = __instance.masterInspector.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                 __result = (int)(__result * storageMul);
             }
             else
             {
                 __result = (int)(__result * MultiplierService.GetMultiplier("Station_ILS_Storage"));
             }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.RefreshValues))]
        public static void UIStationStorage_RefreshValues_Postfix(UIStationStorage __instance)
        {
            if (__instance.station != null && __instance.stationWindow != null && __instance.stationWindow.factory != null && __instance.maxSlider != null)
            {
                ItemProto item = LDB.items.Select(__instance.stationWindow.factory.entityPool[__instance.station.entityId].protoId);
                if (item != null && item.prefabDesc != null)
                {
                    bool isExchange = item.ID >= 9400 ||
                                      (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                      item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0;
                    float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                       __instance.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                    int vanillaExtra = __instance.station.isCollector ? GameMain.history.localStationExtraStorage :
                                       __instance.station.isVeinCollector ? GameMain.history.localStationExtraStorage :
                                       __instance.station.isStellar ? GameMain.history.remoteStationExtraStorage :
                                       GameMain.history.localStationExtraStorage;
                    if (_originalValues.TryGetValue(item.ID, out var original))
                    {
                        int newMax = (int)(original.ItemCount * storageMul) + (int)(vanillaExtra * storageMul);
                        __instance.maxSlider.maxValue = newMax / 100f;
                    }
                }
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.RefreshValues))]
        public static void UIControlPanelStationStorage_RefreshValues_Postfix(UIControlPanelStationStorage __instance)
        {
            if (__instance.station != null && __instance.masterInspector != null && __instance.masterInspector.factory != null && __instance.maxSlider != null)
            {
                ItemProto item = LDB.items.Select(__instance.masterInspector.factory.entityPool[__instance.station.entityId].protoId);
                if (item != null && item.prefabDesc != null)
                {
                    bool isExchange = item.ID >= 9400 ||
                                      (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                      item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0;
                    float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                       __instance.station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                    int vanillaExtra = __instance.station.isCollector ? GameMain.history.localStationExtraStorage :
                                       __instance.station.isVeinCollector ? GameMain.history.localStationExtraStorage :
                                       __instance.station.isStellar ? GameMain.history.remoteStationExtraStorage :
                                       GameMain.history.localStationExtraStorage;
                    if (_originalValues.TryGetValue(item.ID, out var original))
                    {
                        int newMax = (int)(original.ItemCount * storageMul) + (int)(vanillaExtra * storageMul);
                        __instance.maxSlider.maxValue = newMax / 100f;
                    }
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        public static void PlanetTransport_SetStationStorage_Prefix(PlanetTransport __instance, int stationId, ref int itemCountMax)
        {
            if (stationId > 0 && stationId < __instance.stationCursor)
            {
                StationComponent station = __instance.stationPool[stationId];
                if (station != null && station.entityId > 0 && __instance.factory != null)
                {
                    ItemProto item = LDB.items.Select(__instance.factory.entityPool[station.entityId].protoId);
                    if (item != null && item.prefabDesc != null)
                    {
                        bool isExchange = item.ID >= 9400 ||
                                          (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                          item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0;
                        float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                           station.isStellar ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                        int vanillaExtra = station.isCollector ? GameMain.history.localStationExtraStorage :
                                           station.isVeinCollector ? GameMain.history.localStationExtraStorage :
                                           station.isStellar ? GameMain.history.remoteStationExtraStorage :
                                           GameMain.history.localStationExtraStorage;
                        if (_originalValues.TryGetValue(item.ID, out var original))
                        {
                            int customMax = (int)(original.ItemCount * storageMul) + (int)(vanillaExtra * storageMul);
                            _lastRequestedMax[stationId] = Mathf.Min(itemCountMax, customMax);
                        }
                    }
                }
            }
        }
        private static Dictionary<int, int> _lastRequestedMax = new Dictionary<int, int>();
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.SetStationStorage))]
        public static void PlanetTransport_SetStationStorage_Postfix(PlanetTransport __instance, int stationId, int storageIdx, int itemId, int itemCountMax)
        {
            if (_lastRequestedMax.TryGetValue(stationId, out int wantedMax))
            {
                if (storageIdx >= 0 && storageIdx < __instance.stationPool[stationId].storage.Length)
                {
                    if (__instance.stationPool[stationId].storage[storageIdx].itemId == itemId)
                    {
                        __instance.stationPool[stationId].storage[storageIdx].max = wantedMax;
                    }
                }
                _lastRequestedMax.Remove(stationId);
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RematchLocalPairs))]
        public static void RematchLocalPairs_Postfix(StationComponent __instance, StationComponent[] stationPool)
        {
            if (__instance.localPairs == null || __instance.localPairCount <= 1) return;
            var dock = __instance.droneDock;
            Array.Sort(__instance.localPairs, 0, __instance.localPairCount, new LocalPairDistanceComparer(__instance.id, dock, stationPool));
        }
        private static ConditionalWeakTable<StationComponent, ExtraShipState> _extraShipStates = new ConditionalWeakTable<StationComponent, ExtraShipState>();
        private class ExtraShipState
        {
            public bool[] IdleShips;
            public bool[] WorkingShips;
            public ExtraShipState(int capacity)
            {
                IdleShips = new bool[capacity];
                WorkingShips = new bool[capacity];
            }
        }
        public static bool IsExchangeStation(this ItemProto item)
        {
            if (item == null) return false;
            return item.ID >= 9400 ||
                   (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                   item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static ExtraShipState GetExtraShipState(StationComponent station)
        {
            if (station == null) return null;
            if (!_extraShipStates.TryGetValue(station, out var state))
            {
                state = new ExtraShipState(station.workShipDatas.Length);
                _extraShipStates.Add(station, state);
                for (int i = 0; i < Math.Min(64, state.IdleShips.Length); i++)
                {
                    state.IdleShips[i] = (station.idleShipIndices & (1UL << i)) != 0;
                    state.WorkingShips[i] = (station.workShipIndices & (1UL << i)) != 0;
                }
            }
            if (state.IdleShips.Length < station.workShipDatas.Length)
            {
                int newLen = station.workShipDatas.Length;
                bool[] newIdle = new bool[newLen];
                bool[] newWork = new bool[newLen];
                Array.Copy(state.IdleShips, newIdle, state.IdleShips.Length);
                Array.Copy(state.WorkingShips, newWork, state.WorkingShips.Length);
                for (int i = state.IdleShips.Length; i < newLen; i++) newIdle[i] = true;
                state.IdleShips = newIdle;
                state.WorkingShips = newWork;
            }
            return state;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.IdleShipGetToWork))]
        public static bool StationComponent_IdleShipGetToWork_Prefix(StationComponent __instance, int index)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                state.IdleShips[index] = false;
                state.WorkingShips[index] = true;
            }
            if (index < 64)
            {
                __instance.idleShipIndices &= ~(1UL << index);
                __instance.workShipIndices |= (1UL << index);
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.WorkShipBackToIdle))]
        public static bool StationComponent_WorkShipBackToIdle_Prefix(StationComponent __instance, int index)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                state.IdleShips[index] = true;
                state.WorkingShips[index] = false;
            }
            if (index < 64)
            {
                __instance.idleShipIndices |= (1UL << index);
                __instance.workShipIndices &= ~(1UL << index);
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.AddIdleShip))]
        public static bool StationComponent_AddIdleShip_Prefix(StationComponent __instance, int index)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                state.IdleShips[index] = true;
                state.WorkingShips[index] = false;
            }
            if (index < 64)
            {
                __instance.idleShipIndices |= (1UL << index);
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RemoveIdleShip))]
        public static bool StationComponent_RemoveIdleShip_Prefix(StationComponent __instance, int index)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                state.IdleShips[index] = false;
                state.WorkingShips[index] = false;
            }
            if (index < 64)
            {
                __instance.idleShipIndices &= ~(1UL << index);
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.HasWorkShipIndex))]
        public static bool StationComponent_HasWorkShipIndex_Prefix(StationComponent __instance, int index, ref bool __result)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.WorkingShips.Length)
            {
                __result = state.WorkingShips[index];
                return false;
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.HasIdleShipIndex))]
        public static bool StationComponent_HasIdleShipIndex_Prefix(StationComponent __instance, int index, ref bool __result)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                __result = state.IdleShips[index];
                return false;
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.HasShipIndex))]
        public static bool StationComponent_HasShipIndex_Prefix(StationComponent __instance, int index, ref bool __result)
        {
            var state = GetExtraShipState(__instance);
            if (state != null && index >= 0 && index < state.IdleShips.Length)
            {
                __result = state.IdleShips[index] || state.WorkingShips[index];
                return false;
            }
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.ShipRenderersOnTick))]
        public static bool StationComponent_ShipRenderersOnTick_Prefix(StationComponent __instance, AstroData[] astroPoses, ref VectorLF3 rPos, ref Quaternion rRot)
        {
            var state = GetExtraShipState(__instance);
            if (state == null) return true;
            int num1 = 0;
            int num2 = 0;
            int length = __instance.workShipDatas.Length;
            for (int i = 0; i < length; i++) if (state.IdleShips[i]) num1++;
            int number = __instance.idleShipCount - num1;
            if (number > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (!state.IdleShips[i] && !state.WorkingShips[i])
                    {
                        StationComponent_AddIdleShip_Prefix(__instance, i);
                        number--;
                        if (number == 0) break;
                    }
                }
            }
            else if (number < 0)
            {
                for (int i = length - 1; i >= 0; i--)
                {
                    if (state.IdleShips[i])
                    {
                        StationComponent_RemoveIdleShip_Prefix(__instance, i);
                        number++;
                        if (number == 0) break;
                    }
                }
            }
            ref VectorLF3 uPos = ref astroPoses[__instance.planetId].uPos;
            ref Quaternion uRot = ref astroPoses[__instance.planetId].uRot;
            VectorLF3 lookPos = new VectorLF3(0, 0, 0);
            Vector3 uVel = new Vector3(0, 0, 0);
            Quaternion qRot = new Quaternion(0, 0, 0, 1);
            for (int i = 0; i < length; i++) {
                if (i >= __instance.shipRenderers.Length) break;
                ref ShipRenderingData sr = ref __instance.shipRenderers[i];
                ref ShipUIRenderingData sui = ref __instance.shipUIRenderers[i];
                if (state.IdleShips[i]) {
                    sr.gid = __instance.gid;
                    StationComponent.lpos2upos_ref(ref uPos, ref uRot, ref __instance.shipDiskPos[i], ref lookPos);
                    Maths.QMultiply_ref(ref uRot, ref __instance.shipDiskRot[i], out qRot);
                    sr.SetPose(ref lookPos, ref qRot, ref rPos, ref rRot, ref uVel, 0);
                    num2 = i + 1;
                    sr.anim = Vector4.zero;
                    sui.gid = 0;
                } else if (state.WorkingShips[i]) {
                    sr.gid = __instance.gid;
                    num2 = i + 1;
                    sui.gid = __instance.gid;
                } else {
                    sr.gid = 0;
                    sr.anim = Vector4.zero;
                    sui.gid = 0;
                }
            }
            __instance.renderShipCount = num2;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.QueryIdleShip))]
        public static bool StationComponent_QueryIdleShip_Prefix(StationComponent __instance, int qIdx, ref int __result)
        {
            var state = GetExtraShipState(__instance);
            if (state == null) return true;
            int len = __instance.workShipDatas.Length;
            for (int i = 0; i < len; i++)
            {
                int num = (qIdx + i) % len;
                if (num < state.IdleShips.Length && state.IdleShips[num])
                {
                    __result = num;
                    return false;
                }
            }
            __result = -1;
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.Import))]
        public static void StationComponent_Import_ShipFix_Postfix(StationComponent __instance)
        {
            if (__instance == null) return;
            var state = GetExtraShipState(__instance);
            if (state == null) return;
            for (int i = 0; i < state.IdleShips.Length; i++) state.IdleShips[i] = true;
            for (int i = 0; i < __instance.workShipCount; i++)
            {
                int shipIdx = __instance.workShipDatas[i].shipIndex;
                if (shipIdx >= 0 && shipIdx < state.IdleShips.Length)
                {
                    state.IdleShips[shipIdx] = false;
                }
            }
        }
        private struct LocalPairDistanceComparer : IComparer<SupplyDemandPair>
        {
            private readonly int _myId;
            private readonly Vector3 _myDock;
            private readonly StationComponent[] _pool;
            public LocalPairDistanceComparer(int myId, Vector3 myDock, StationComponent[] pool)
            {
                _myId = myId;
                _myDock = myDock;
                _pool = pool;
            }
            public int Compare(SupplyDemandPair a, SupplyDemandPair b)
            {
                float distA = GetDistSq(a);
                float distB = GetDistSq(b);
                return distA.CompareTo(distB);
            }
            private float GetDistSq(SupplyDemandPair pair)
            {
                int otherId = pair.supplyId == _myId ? pair.demandId : pair.supplyId;
                if (otherId <= 0 || otherId >= _pool.Length || _pool[otherId] == null)
                    return float.MaxValue;
                var otherDock = _pool[otherId].droneDock;
                float dx = _myDock.x - otherDock.x;
                float dy = _myDock.y - otherDock.y;
                float dz = _myDock.z - otherDock.z;
                return dx * dx + dy * dy + dz * dz;
            }
        }
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.RefreshStationTraffic))]
        [HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.GameTick))]
        [HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RefreshTraffic))]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText))]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
        [HarmonyPatch(typeof(UIPlayerDeliveryPanel), "_OnUpdate")]
        [HarmonyPatch(typeof(DispenserComponent), nameof(DispenserComponent.InternalTick))]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
        public static IEnumerable<CodeInstruction> CapacityTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var droneField = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.logisticDroneCarries));
            var shipField = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.logisticShipCarries));
            var courierField = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.logisticCourierCarries));
            for (int i = 0; i < codes.Count; i++)
            {
                var opcode = codes[i].opcode;
                if (opcode == OpCodes.Ldfld)
                {
                    var field = codes[i].operand as FieldInfo;
                    if (field == droneField)
                    {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpaciousStationsPlugin), nameof(SpaciousStationsPlugin.GetMultipliedDroneCarry))));
                        i++;
                    }
                    else if (field == shipField)
                    {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpaciousStationsPlugin), nameof(SpaciousStationsPlugin.GetMultipliedShipCarry))));
                        i++;
                    }
                    else if (field == courierField)
                    {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpaciousStationsPlugin), nameof(SpaciousStationsPlugin.GetMultipliedCourierCarry))));
                        i++;
                    }
                }
            }
            return codes;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "DispatchSupplyShip")]
        public static bool DispatchSupplyShip_Prefix(ref int carryCnt, StationComponent other, ref SupplyDemandPair pair)
        {
            int room = other.storage[pair.demandIndex].remoteDemandCount;
            if (carryCnt > room) carryCnt = room;
            return carryCnt > 0;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "DispatchDemandShip")]
        public static bool DispatchDemandShip_Prefix(StationComponent __instance, ref int shipCarries, StationComponent other, ref SupplyDemandPair pair)
        {
            int room = __instance.storage[pair.demandIndex].remoteDemandCount;
            int supply = other.storage[pair.supplyIndex].remoteSupplyCount;
            int cap = Math.Min(room, supply);
            if (shipCarries > cap) shipCarries = cap;
            return shipCarries > 0;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemProto), nameof(ItemProto.GetPropValue))]
        public static void ItemProto_GetPropValue_Postfix(ItemProto __instance, int index, ref string __result)
        {
            if (GameMain.history == null || index < 0 || index >= __instance.DescFields.Length) return;
            int propId = __instance.DescFields[index];
            if (propId == 25) 
            {
                __result = SpaciousStationsPlugin.GetMultipliedDroneCarry(GameMain.history.logisticDroneCarries).ToString();
            }
            else if (propId == 26) 
            {
                __result = SpaciousStationsPlugin.GetMultipliedShipCarry(GameMain.history.logisticShipCarries).ToString();
            }
        }
        }
    public static class StationExtensions
    {
        public static void PatchDroneArray(this StationComponent station, int newCount)
        {
            if (station == null || station.workDroneDatas == null) return;
            if (station.workDroneDatas.Length < newCount)
            {
                int oldLen = station.workDroneDatas.Length;
                Array.Resize(ref station.workDroneDatas, newCount);
                Array.Resize(ref station.workDroneOrders, newCount);
                Array.Resize(ref station.droneDispatchStatus, newCount);
                for (int i = oldLen; i < newCount; i++)
                {
                    station.workDroneDatas[i] = default;
                    station.workDroneOrders[i] = default;
                    station.droneDispatchStatus[i] = 1;
                }
            }
        }
        public static bool IsExchangeStation(this StationComponent station)
        {
            if (station == null || station.planetId <= 0) return false;
            var factory = GameMain.galaxy?.PlanetById(station.planetId)?.factory;
            if (factory == null || station.entityId <= 0 || station.entityId >= factory.entityPool.Length) return false;
            int protoId = factory.entityPool[station.entityId].protoId;
            return LDB.items.Select(protoId).IsExchangeStation();
        }
        public static void PatchShipArray(this StationComponent station, int newCount)
        {
            if (station == null || station.workShipDatas == null) return;
            if (station.workShipDatas.Length < newCount)
            {
                int oldLen = station.workShipDatas.Length;
                Array.Resize(ref station.workShipDatas, newCount);
                Array.Resize(ref station.workShipOrders, newCount);
                Array.Resize(ref station.shipRenderers, newCount);
                Array.Resize(ref station.shipUIRenderers, newCount);
                Array.Resize(ref station.shipDiskPos, newCount);
                Array.Resize(ref station.shipDiskRot, newCount);
                for (int i = oldLen; i < newCount; i++)
                {
                    station.workShipDatas[i] = default;
                    station.workShipOrders[i] = default;
                    station.shipRenderers[i] = default;
                    station.shipUIRenderers[i] = default;
                    if (oldLen > 0)
                    {
                        station.shipDiskPos[i] = station.shipDiskPos[i % oldLen];
                        station.shipDiskRot[i] = station.shipDiskRot[i % oldLen];
                    }
                    else
                    {
                        station.shipDiskPos[i] = Vector3.zero;
                        station.shipDiskRot[i] = Quaternion.identity;
                    }
                }
            }
        }
    }
    [HarmonyPatch]
    public static class MMS_StarAssembly_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare() => AccessTools.TypeByName("MoreMegaStructure.StarAssembly") != null;
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod() => AccessTools.Method("MoreMegaStructure.StarAssembly:InternalUpdate");
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            float mult = SpaciousStationsPlugin.EXC_InternalsMultiplier.Value;
            int newMax = (int)(99999 * mult);
            int newSoft = (int)(10000 * mult);
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_I4 && (int)ins.operand == 99999)
                {
                    ins.operand = newMax;
                }
                else if (ins.opcode == OpCodes.Ldc_I4 && (int)ins.operand == 10000)
                {
                    ins.operand = newSoft;
                }
                yield return ins;
            }
        }
    }
}
