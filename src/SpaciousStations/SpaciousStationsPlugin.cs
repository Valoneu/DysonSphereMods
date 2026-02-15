using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
using UnityEngine;
using CommonAPI;
using CommonAPI.Systems;
using xiaoye97;

namespace SpaciousStations
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    public class SpaciousStationsPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.SpaciousStations";
        public const string MOD_NAME = "SpaciousStations";
        public const string MOD_VERSION = "1.0.5";

        public static ConfigEntry<float> DroneMultiplier;
        public static ConfigEntry<float> ShipMultiplier;
        public static ConfigEntry<float> StorageMultiplier;
        public static ConfigEntry<float> ChargeMultiplier;
        public static ConfigEntry<float> EnergyMultiplier;
        public static ConfigEntry<float> InternalLastStorageMultiplier;
        public static ConfigEntry<float> InternalLastChargeMultiplier;

        private void Awake()
        {
            DroneMultiplier = Config.Bind("General", "DroneMultiplier", 2f, "Multiplies max number of drones in a station.");
            ShipMultiplier = Config.Bind("General", "ShipMultiplier", 2f, "Multiplies max number of ships in a station.");
            StorageMultiplier = Config.Bind("General", "StorageMultiplier", 2f, "Multiplies maximum amount of items in a station.");
            ChargeMultiplier = Config.Bind("General", "ChargeMultiplier", 2f, "Multiplies station's charge power.");
            EnergyMultiplier = Config.Bind("General", "EnergyMultiplier", 2f, "Multiplies station's max energy storage.");
            InternalLastStorageMultiplier = Config.Bind("Internal", "LastStorageMultiplier", 1f, "Internal use only.");
            InternalLastChargeMultiplier = Config.Bind("Internal", "LastChargeMultiplier", 1f, "Internal use only.");

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StationPatch));

            LDBTool.EditDataAction += StationPatch.OnEditData;

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }
    }

    public static class StationPatch
    {
        private static Dictionary<int, ProtoValues> _originalValues = new Dictionary<int, ProtoValues>();
        private static bool _prototypesApplied = false;

        private struct ProtoValues
        {
            public int DroneCount;
            public int ShipCount;
            public int ItemCount;
            public long EnergyMax;
            public long EnergyPerTick;
        }

        public static void OnEditData(Proto proto)
        {
            if (proto is ItemProto item) ApplyToItem(item);
            if (proto is ModelProto model) ApplyToModel(model);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            Log.Info("Performing final pass on prototypes...");
            if (LDB.items != null) foreach (var item in LDB.items.dataArray) ApplyToItem(item);
            if (LDB.models != null) foreach (var model in LDB.models.dataArray) ApplyToModel(model);
            _prototypesApplied = true;
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
            
            desc.stationMaxDroneCount = (int)(original.DroneCount * SpaciousStationsPlugin.DroneMultiplier.Value);
            desc.stationMaxShipCount = (int)(original.ShipCount * SpaciousStationsPlugin.ShipMultiplier.Value);
            desc.stationMaxItemCount = (int)(original.ItemCount * SpaciousStationsPlugin.StorageMultiplier.Value);
            desc.stationMaxEnergyAcc = (long)(original.EnergyMax * SpaciousStationsPlugin.EnergyMultiplier.Value);
            if (!desc.isCollectStation)
                desc.workEnergyPerTick = (long)(original.EnergyPerTick * SpaciousStationsPlugin.ChargeMultiplier.Value);
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

            if (__instance.storage != null)
            {
                int vanillaExtra = GetVanillaAdditionStorage(__instance);
                int vanillaMax = original.ItemCount + vanillaExtra;
                int prevMax = (int)(vanillaMax * SpaciousStationsPlugin.InternalLastStorageMultiplier.Value);
                int newMax = desc.stationMaxItemCount + (int)(vanillaExtra * SpaciousStationsPlugin.StorageMultiplier.Value);

                for (int i = 0; i < __instance.storage.Length; i++)
                {
                    if (__instance.storage[i].itemId > 0)
                    {
                        if (__instance.storage[i].max == vanillaMax || __instance.storage[i].max == prevMax || __instance.storage[i].max > newMax || (SpaciousStationsPlugin.InternalLastStorageMultiplier.Value != SpaciousStationsPlugin.StorageMultiplier.Value && __instance.storage[i].max == prevMax))
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

                long currentCharge = __instance.consumerPool[station.pcId].workEnergyPerTick;
                long vanillaMax = original.EnergyPerTick;
                long prevMax = (long)(vanillaMax * SpaciousStationsPlugin.InternalLastChargeMultiplier.Value);
                long newMax = desc.workEnergyPerTick;

                if (currentCharge == vanillaMax || currentCharge == prevMax || currentCharge > newMax || (SpaciousStationsPlugin.InternalLastChargeMultiplier.Value != SpaciousStationsPlugin.ChargeMultiplier.Value && currentCharge == prevMax))
                    __instance.consumerPool[station.pcId].workEnergyPerTick = newMax;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            if (LDB.items != null) foreach (var item in LDB.items.dataArray) ApplyToItem(item);
            
            float currStoreMul = SpaciousStationsPlugin.StorageMultiplier.Value;
            float currChargeMul = SpaciousStationsPlugin.ChargeMultiplier.Value;
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
        public static void UIStationStorage_GetAdditionStorage_Postfix(ref int __result)
        {
            __result = (int)(__result * SpaciousStationsPlugin.StorageMultiplier.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage.GetAdditionStorage))]
        public static void UIControlPanelStationStorage_GetAdditionStorage_Postfix(ref int __result)
        {
            __result = (int)(__result * SpaciousStationsPlugin.StorageMultiplier.Value);
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
                if (codes[i].opcode == OpCodes.Ldfld && (codes[i].operand as FieldInfo == localField || codes[i].operand as FieldInfo == remoteField))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Conv_R4));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StationPatch), nameof(StationPatch.get_StorageMultiplierValue))));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Conv_I4));
                    i += 4;
                }
            }
            return codes;
        }

        public static float get_StorageMultiplierValue() => SpaciousStationsPlugin.StorageMultiplier.Value;

        private static int GetVanillaAdditionStorage(StationComponent station)
        {
            if (station == null || GameMain.history == null) return 0;
            return !station.isCollector ? (!station.isVeinCollector ? (!station.isStellar ? GameMain.history.localStationExtraStorage : GameMain.history.remoteStationExtraStorage) : GameMain.history.localStationExtraStorage) : GameMain.history.localStationExtraStorage;
        }
    }
}
