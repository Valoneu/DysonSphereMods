using System;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
using UnityEngine;

namespace SpaciousStations
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class SpaciousStationsPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.SpaciousStations";
        public const string MOD_NAME = "SpaciousStations";
        public const string MOD_VERSION = "1.0.0";

        public static ConfigEntry<float> DroneMultiplier;
        public static ConfigEntry<float> ShipMultiplier;
        public static ConfigEntry<float> StorageMultiplier;
        public static ConfigEntry<float> ChargeMultiplier;
        public static ConfigEntry<float> EnergyMultiplier;

        private void Awake()
        {
            DroneMultiplier = Config.Bind("General", "DroneMultiplier", 2f, "Multiplies max number of drones in a station.");
            ShipMultiplier = Config.Bind("General", "ShipMultiplier", 2f, "Multiplies max number of ships in a station.");
            StorageMultiplier = Config.Bind("General", "StorageMultiplier", 2f, "Multiplies maximum amount of items in a station.");
            ChargeMultiplier = Config.Bind("General", "ChargeMultiplier", 2f, "Multiplies station's charge power.");
            EnergyMultiplier = Config.Bind("General", "EnergyMultiplier", 2f, "Multiplies station's max energy storage.");

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StationPatch));

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }
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
            ApplyToPrototypes();
        }

        public static void ApplyToPrototypes()
        {
            if (_originalValues.Count == 0)
            {
                foreach (var item in LDB.items.dataArray)
                {
                    if (item.prefabDesc != null && item.prefabDesc.isStation)
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
                }
            }

            foreach (var item in LDB.items.dataArray)
            {
                if (_originalValues.TryGetValue(item.ID, out var original))
                {
                    item.prefabDesc.stationMaxDroneCount = (int)(original.DroneCount * SpaciousStationsPlugin.DroneMultiplier.Value);
                    item.prefabDesc.stationMaxShipCount = (int)(original.ShipCount * SpaciousStationsPlugin.ShipMultiplier.Value);
                    item.prefabDesc.stationMaxItemCount = (int)(original.ItemCount * SpaciousStationsPlugin.StorageMultiplier.Value);
                    item.prefabDesc.stationMaxEnergyAcc = (long)(original.EnergyMax * SpaciousStationsPlugin.EnergyMultiplier.Value);
                    item.prefabDesc.workEnergyPerTick = (long)(original.EnergyPerTick * SpaciousStationsPlugin.ChargeMultiplier.Value);
                }
            }
            Log.Info("Applied multipliers to station prototypes.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            ApplyToExistingStations();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.Init))]
        public static void StationComponent_Init_Prefix(ref int _extraStorage)
        {
            _extraStorage = (int)(_extraStorage * SpaciousStationsPlugin.StorageMultiplier.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.GetAdditionStorage))]
        public static void UIStationStorage_GetAdditionStorage_Postfix(ref int __result)
        {
            __result = (int)(__result * SpaciousStationsPlugin.StorageMultiplier.Value);
        }

        public static void ApplyToExistingStations()
        {
            if (GameMain.data == null) return;
            foreach (var factory in GameMain.data.factories)
            {
                if (factory == null || factory.transport == null) continue;
                foreach (var station in factory.transport.stationPool)
                {
                    if (station == null || station.id <= 0) continue;
                    
                    var itemProto = LDB.items.Select(factory.entityPool[station.entityId].protoId);
                    if (itemProto == null) continue;
                    
                    var desc = itemProto.prefabDesc;
                    
                    // Drone arrays
                    station.PatchDroneArray(desc.stationMaxDroneCount);
                    
                    // Ship arrays
                    if (station.workShipDatas != null && station.workShipDatas.Length != desc.stationMaxShipCount)
                    {
                        int oldCnt = station.workShipDatas.Length;
                        int newCnt = desc.stationMaxShipCount;
                        
                        station.workShipDatas = ResizeArray(station.workShipDatas, newCnt);
                        station.workShipOrders = ResizeArray(station.workShipOrders, newCnt);
                        station.shipRenderers = ResizeArray(station.shipRenderers, newCnt);
                        station.shipUIRenderers = ResizeArray(station.shipUIRenderers, newCnt);
                        station.shipDiskPos = ResizeArray(station.shipDiskPos, newCnt);
                        station.shipDiskRot = ResizeArray(station.shipDiskRot, newCnt);
                        
                        if (station.isStellar && newCnt > oldCnt)
                        {
                            for (int index = 0; index < newCnt; ++index)
                            {
                                station.shipDiskRot[index] = Quaternion.Euler(0.0f, 360f / (float)newCnt * (float)index, 0.0f);
                                station.shipDiskPos[index] = station.shipDiskRot[index] * new Vector3(0.0f, 0.0f, 11.5f);
                            }
                            for (int index = 0; index < newCnt; ++index)
                            {
                                station.shipDiskRot[index] = station.shipDockRot * station.shipDiskRot[index];
                                station.shipDiskPos[index] = station.shipDockPos + station.shipDockRot * station.shipDiskPos[index];
                            }
                        }
                    }
                    
                    // Energy
                    station.energyMax = desc.stationMaxEnergyAcc;
                    if (station.pcId > 0 && factory.powerSystem != null && station.pcId < factory.powerSystem.consumerCursor)
                    {
                        factory.powerSystem.consumerPool[station.pcId].workEnergyPerTick = desc.workEnergyPerTick;
                    }
                    
                    // Storage
                    int extra = GetAdditionStorage(station);
                    if (station.storage != null)
                    {
                        for (int i = 0; i < station.storage.Length; i++)
                        {
                            if (station.storage[i].itemId > 0)
                            {
                                station.storage[i].max = desc.stationMaxItemCount + extra;
                            }
                        }
                    }
                }
            }
        }

        private static T[] ResizeArray<T>(T[] array, int newSize)
        {
            if (array == null) return new T[newSize];
            if (array.Length == newSize) return array;
            T[] newArray = new T[newSize];
            Array.Copy(array, newArray, Math.Min(array.Length, newSize));
            return newArray;
        }

        private static int GetAdditionStorage(StationComponent station)
        {
            if (station == null || GameMain.history == null) return 0;
            int extra = !station.isCollector ? (!station.isVeinCollector ? (!station.isStellar ? GameMain.history.localStationExtraStorage : GameMain.history.remoteStationExtraStorage) : GameMain.history.localStationExtraStorage) : GameMain.history.localStationExtraStorage;
            return (int)(extra * SpaciousStationsPlugin.StorageMultiplier.Value);
        }
    }
}
