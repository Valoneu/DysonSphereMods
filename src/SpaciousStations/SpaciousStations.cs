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
        public const string MOD_VERSION = "1.2.0";

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

        public static ConfigEntry<int> DroneTaskInterval;
        public static ConfigEntry<int> ShipTaskInterval;

        public static ConfigEntry<float> InternalLastStorageMultiplier;
        public static ConfigEntry<float> InternalLastChargeMultiplier;
        public static ConfigEntry<float> InternalLastPLSStorageMultiplier;
        public static ConfigEntry<float> InternalLastPLSChargeMultiplier;
        public static ConfigEntry<float> InternalLastEXCStorageMultiplier;
        public static ConfigEntry<float> InternalLastEXCChargeMultiplier;

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

            DroneTaskInterval = Config.Bind("General", "DroneTaskInterval", 20, "The interval between drone dispatches. Lower is faster. Vanilla default is 20 (3 dispatches per second). Setting this to 1 will dispatch drones every tick (60 per second).");
            ShipTaskInterval = Config.Bind("General", "ShipTaskInterval", 10, "The interval between vessel dispatches for high priority items. Lower is faster. Vanilla default is 10 (6 dispatches per second). Setting this to 1 will dispatch vessels every tick (60 per second). Note: other priority items use 3x and 6x this interval.");

            InternalLastStorageMultiplier = Config.Bind("Internal", "LastStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastChargeMultiplier = Config.Bind("Internal", "LastChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastPLSStorageMultiplier = Config.Bind("Internal", "LastPLSStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastPLSChargeMultiplier = Config.Bind("Internal", "LastPLSChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastEXCStorageMultiplier = Config.Bind("Internal", "LastEXCStorageMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));
            InternalLastEXCChargeMultiplier = Config.Bind("Internal", "LastEXCChargeMultiplier", 1f, new ConfigDescription("DO NOT CHANGE. Used internally to track migration between sessions.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false }));

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

            // MoreMegaStructure Exchange Station has a specific name or ID, but typically mod items use IDs > 9000 or specific names.
            // The safest check is the name "MegaStructure" in the type or prefab, but since it's an ItemProto, it has a name.
            bool isExchangeStation = item != null && (
                item.ID >= 9400 ||
                (item.name != null && (item.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                item.Name == "星际组装厂" || item.Name == "物资交换器" || item.Name == "Interstellar Assembly" || item.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
            );
            // For now, checking name or ID is the only way if we don't have direct access to Megastructures types. Let's use name heuristic.
            // A more robust check: MoreMegaStructure uses ID 9494, 9495, etc. Let's check for ID range if we know it, or just rely on Name. 
            // In FactoryOverclock it checks `isPowerExchanger` but for "ExchangeStationComponent" it's different.
            // Actually, "ExchangeStationComponent" is not an exchanger, it's a station. Megastructure's exchange station is a station with a specific Name or ID.
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
            __instance.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            if (LDB.items != null) foreach (var item in LDB.items.dataArray) ApplyToItem(item);
            
            // Fix all existing stations - Import postfix may have missed them because
            // _originalValues wasn't populated yet when Import ran during load.
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

                        // Fix drone array and energy
                        station.PatchDroneArray(desc.stationMaxDroneCount);
                        station.energyMax = desc.stationMaxEnergyAcc;
                        station.droneTaskInterval = SpaciousStationsPlugin.DroneTaskInterval.Value;

                        // Fix storage limits — only rescale if multiplier changed
                        if (station.storage != null)
                        {
                            bool isExchange = itemProto != null && (
                                itemProto.ID >= 9400 ||
                                (itemProto.name != null && (itemProto.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                itemProto.Name == "星际组装厂" || itemProto.Name == "物资交换器" || itemProto.Name == "Interstellar Assembly" || itemProto.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
                            );

                            float storageMul = isExchange ? MultiplierService.GetMultiplier("Station_EXC_Storage") :
                                               desc.isStellarStation ? MultiplierService.GetMultiplier("Station_ILS_Storage") : MultiplierService.GetMultiplier("Station_PLS_Storage");
                            float lastMul = isExchange ? SpaciousStationsPlugin.InternalLastEXCStorageMultiplier.Value :
                                            desc.isStellarStation ? SpaciousStationsPlugin.InternalLastStorageMultiplier.Value : SpaciousStationsPlugin.InternalLastPLSStorageMultiplier.Value;
                            
                            int vanillaExtra = GetVanillaAdditionStorage(station);
                            int vanillaMax = original.ItemCount + vanillaExtra;
                            int newMax = desc.stationMaxItemCount + (int)(vanillaExtra * storageMul);
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

                        // Fix charge power — only rescale if multiplier changed
                        if (!desc.isCollectStation && station.pcId > 0 && factory.powerSystem != null
                            && station.pcId < factory.powerSystem.consumerCursor)
                        {
                            bool isExchange = itemProto != null && (
                                itemProto.ID >= 9400 ||
                                (itemProto.name != null && (itemProto.name.IndexOf("Exchange Logistic Station", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.name.IndexOf("Matter", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.name.IndexOf("星际组装厂", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                                itemProto.Name == "星际组装厂" || itemProto.Name == "物资交换器" || itemProto.Name == "Interstellar Assembly" || itemProto.Name.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) >= 0 || itemProto.Name.IndexOf("组装", StringComparison.OrdinalIgnoreCase) >= 0
                            );

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
            
            Log.Info("GameMain.Begin: All station limits synced.");
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
                            
                            // The slider passes in a value from 0 to 400,000 for Exchange Station.
                            // SetStationStorage clamps it: `if (itemCountMax > num1 + num2) itemCountMax = num1 + num2;`
                            // If we update `itemCountMax` to cap ONLY at our `customMax`, we can just skip the vanilla clamp!
                            // But wait, we can't skip the clamp without a transpiler.
                            // Let's just redefine `itemCountMax` to whatever the slider passed in, up to `customMax`.
                            // Then, in a postfix, we'll re-apply the value!
                            
                            // Let's create a local field to store the requested max.
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

        // --- Closest Drone Dispatch: sort localPairs by distance after they are built ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.RematchLocalPairs))]
        public static void RematchLocalPairs_Postfix(StationComponent __instance, StationComponent[] stationPool)
        {
            if (__instance.localPairs == null || __instance.localPairCount <= 1) return;

            var dock = __instance.droneDock;
            Array.Sort(__instance.localPairs, 0, __instance.localPairCount, new LocalPairDistanceComparer(__instance.id, dock, stationPool));
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
    }
}
