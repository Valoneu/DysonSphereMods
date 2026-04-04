using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using DysonSphereMods.Shared;
namespace CopyPasteStations
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class CopyPasteStationsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.CopyPasteStations";
        public const string NAME = "CopyPasteStations";
        public const string VERSION = "1.0.2";
        private void Awake()
        {
            Log.Init(Logger);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(CopyPastePatches));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
    }
    [HarmonyPatch]
    public static class CopyPastePatches
    {
        private static List<SlotConfig> _clipboard;
        private static bool _clipboardIsStellar;
        private static int _clipboardDroneCount;
        private static int _clipboardShipCount;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CopyBuildingSetting))]
        public static void CopyBuildingSetting_Postfix(PlanetFactory __instance, int objectId)
        {
            var station = GetStation(__instance, objectId);
            if (station == null) return;
            _clipboardIsStellar = station.isStellar;
            _clipboardDroneCount = station.idleDroneCount + station.workDroneCount;
            _clipboardShipCount = station.idleShipCount + station.workShipCount;
            _clipboard = new List<SlotConfig>();
            for (int i = 0; i < station.storage.Length; i++)
            {
                var store = station.storage[i];
                _clipboard.Add(new SlotConfig
                {
                    ItemId = store.itemId,
                    LocalLogic = store.localLogic,
                    RemoteLogic = store.remoteLogic,
                    Max = store.max,
                    KeepMode = store.keepMode
                });
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PasteBuildingSetting))]
        public static void PasteBuildingSetting_Postfix(PlanetFactory __instance, int objectId)
        {
            if (_clipboard == null) return;
            var station = GetStation(__instance, objectId);
            if (station == null) return;
            if (_clipboardIsStellar && (station.isCollector || station.isVeinCollector)) return;
            var player = GameMain.mainPlayer;
            int slotCount = Math.Min(_clipboard.Count, station.storage.Length);
            for (int i = 0; i < slotCount; i++)
            {
                var slot = _clipboard[i];
                if (station.storage[i].itemId != slot.ItemId && station.storage[i].count > 0)
                {
                    int itemId = station.storage[i].itemId;
                    int count = station.storage[i].count;
                    int inc = station.storage[i].inc;
                    if (player != null)
                    {
                        int added = player.package.AddItem(itemId, count, inc, out int remainInc);
                        count -= added;
                    }
                    if (count > 0)
                    {
                        GameMain.data.trashSystem.AddTrash(itemId, count, inc, station.entityId);
                    }
                }
                station.storage[i].itemId = slot.ItemId;
                station.storage[i].localLogic = slot.LocalLogic;
                station.storage[i].remoteLogic = slot.RemoteLogic;
                station.storage[i].max = slot.Max;
                station.storage[i].keepMode = slot.KeepMode;
            }
            station.UpdateNeeds();
            if (player == null) return;
            int droneItemId = 5001; 
            int shipItemId = 5002; 
            int currentDrones = station.idleDroneCount + station.workDroneCount;
            int wantedDrones = _clipboardDroneCount;
            if (currentDrones < wantedDrones)
            {
                int need = wantedDrones - currentDrones;
                int have = player.package.GetItemCount(droneItemId);
                int give = Math.Min(need, have);
                if (give > 0)
                {
                    player.package.TakeTailItems(ref droneItemId, ref give, out int inc, false);
                    station.idleDroneCount += give;
                }
            }
            if (station.isStellar && _clipboardIsStellar)
            {
                int currentShips = station.idleShipCount + station.workShipCount;
                int wantedShips = _clipboardShipCount;
                if (currentShips < wantedShips)
                {
                    int need = wantedShips - currentShips;
                    int have = player.package.GetItemCount(shipItemId);
                    int give = Math.Min(need, have);
                    if (give > 0)
                    {
                        player.package.TakeTailItems(ref shipItemId, ref give, out int inc, false);
                        station.idleShipCount += give;
                    }
                }
            }
            var stationWindow = UIRoot.instance?.uiGame?.stationWindow;
            if (stationWindow != null && stationWindow.active)
                stationWindow.OnStationIdChange();
        }
        private static StationComponent GetStation(PlanetFactory factory, int objectId)
        {
            if (factory?.transport == null || objectId <= 0) return null;
            if (objectId >= factory.entityPool.Length) return null;
            int stationId = factory.entityPool[objectId].stationId;
            if (stationId <= 0 || stationId >= factory.transport.stationCursor) return null;
            var station = factory.transport.stationPool[stationId];
            if (station == null || station.id != stationId) return null;
            return station;
        }
    }
    public class SlotConfig
    {
        public int ItemId;
        public ELogisticStorage LocalLogic;
        public ELogisticStorage RemoteLogic;
        public int Max;
        public int KeepMode;
    }
}
