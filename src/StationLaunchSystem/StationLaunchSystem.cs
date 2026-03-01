using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
using UnityEngine;
namespace StationLaunchSystem
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class StationLaunchSystemPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.StationLaunchSystem";
        public const string NAME = "StationLaunchSystem";
        public const string VERSION = "1.0.2";
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<int> RocketsPerTick;
        public static ConfigEntry<int> SailsPerTick;
        private void Awake()
        {
            Log.Init(Logger);
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable/disable automatic launching from depot stations.");
            RocketsPerTick = Config.Bind("General", "RocketsPerTick", 5, "Max rockets per tick per station.");
            SailsPerTick = Config.Bind("General", "SailsPerTick", 20, "Max solar sails per tick per station.");
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GameBeginPatch));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
    }
    [HarmonyPatch]
    public static class GameBeginPatch
    {
        private static bool _subscribed = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void OnGameBegin()
        {
            try {
                if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= LaunchLogic.OnFactoryFrameEnd; _subscribed = false; }
                GameMain.logic.onFactoryFrameEnd += LaunchLogic.OnFactoryFrameEnd;
                _subscribed = true;
                Log.Info("[StationLaunch] Subscribed to onFactoryFrameEnd");
            } catch (Exception ex) { Log.Info($"[StationLaunch] ERROR subscribing: {ex.Message}"); }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            try { if (_subscribed) { GameMain.logic.onFactoryFrameEnd -= LaunchLogic.OnFactoryFrameEnd; _subscribed = false; } } catch { }
        }
    }
    public static class LaunchLogic
    {
        private static readonly int[] ROCKET_ITEM_IDS = { 1503, 9488, 9489, 9490, 9491, 9492, 9510 };
        private static readonly int[] SAIL_ITEM_IDS = { 1501, 6006 };
        private static bool IsRocketItem(int id) {
            for (int i = 0; i < ROCKET_ITEM_IDS.Length; i++) if (ROCKET_ITEM_IDS[i] == id) return true;
            return false;
        }
        private static bool IsSailItem(int id) {
            for (int i = 0; i < SAIL_ITEM_IDS.Length; i++) if (SAIL_ITEM_IDS[i] == id) return true;
            return false;
        }
        private struct ConstructibleShell {
            public DysonShell shell;
            public float absY;
        }
        private static System.Collections.Generic.Dictionary<int, (long tick, System.Collections.Generic.List<ConstructibleShell> shells)> _shellCaches 
            = new System.Collections.Generic.Dictionary<int, (long tick, System.Collections.Generic.List<ConstructibleShell> shells)>();
        private static System.Collections.Generic.List<ConstructibleShell> GetShellCache(DysonSphere sphere, long tick)
        {
            if (_shellCaches.TryGetValue(sphere.starData.index, out var entry) && tick - entry.tick < 120) {
                return entry.shells;
            }
            var list = new System.Collections.Generic.List<ConstructibleShell>();
            if (sphere.layersIdBased == null) return list;
            for (int i = 1; i < sphere.layersIdBased.Length; i++) {
                var layer = sphere.layersIdBased[i];
                if (layer == null || layer.id != i) continue;
                for (int si = 1; si < layer.shellCursor; si++) {
                    var shell = layer.shellPool[si];
                    if (shell == null || shell.id != si || !IsShellReady(shell)) continue;
                    if (shell.nodes == null || shell.nodes.Count == 0 || shell.nodecps == null) continue;
                    bool fullyConstructed = true;
                    for (int ni = 0; ni < shell.nodes.Count; ni++) {
                        int maxCp = (shell.vertsqOffset[ni + 1] - shell.vertsqOffset[ni]) * shell.cpPerVertex;
                        if (shell.nodecps[ni] < maxCp) { fullyConstructed = false; break; }
                    }
                    if (fullyConstructed) continue;
                    float sumY = 0;
                    if (shell.nodes != null && shell.nodes.Count > 0) {
                        foreach (var node in shell.nodes) sumY += Math.Abs(node.pos.y);
                        list.Add(new ConstructibleShell { shell = shell, absY = sumY / shell.nodes.Count });
                    }
                }
            }
            list.Sort((a, b) => a.absY.CompareTo(b.absY));
            _shellCaches[sphere.starData.index] = (tick, list);
            return list;
        }
        public static void OnFactoryFrameEnd()
        {
            try {
                if (!StationLaunchSystemPlugin.ModEnabled.Value) return;
                var data = GameMain.data;
                if (data == null) return;
                long tick = GameMain.gameTick;
                if (tick % 10 != 0) return; 
                for (int fi = 0; fi < data.factoryCount; fi++) {
                    var factory = data.factories[fi];
                    if (factory?.transport == null || factory.planet == null) continue;
                    int starIndex = factory.planet.star?.index ?? -1;
                    if (starIndex < 0 || starIndex >= data.dysonSpheres.Length) continue;
                    var sphere = data.dysonSpheres[starIndex];
                    if (sphere == null) continue;
                    int[] consumeReg = GameMain.statistics?.production?.factoryStatPool?[factory.index]?.consumeRegister;
                    LaunchRockets(factory, sphere, consumeReg, tick);
                    LaunchSails(factory, sphere, consumeReg, tick);
                }
            } catch (Exception ex) { Log.Info($"[StationLaunch] ERROR: {ex.Message}"); }
        }
        private static void LaunchRockets(PlanetFactory factory, DysonSphere sphere, int[] consumeReg, long tick)
        {
            int maxPerStation = StationLaunchSystemPlugin.RocketsPerTick.Value * 10; 
            if (maxPerStation <= 0 || sphere.GetAutoNodeCount() <= 0) return;
            for (int si = 1; si < factory.transport.stationCursor; si++) {
                var station = factory.transport.stationPool[si];
                if (station == null || station.id != si || !station.isStellar || station.isCollector) continue;
                for (int slot = 0; slot < station.storage.Length; slot++) {
                    if (station.storage[slot].localLogic != ELogisticStorage.None) continue;
                    if (!IsRocketItem(station.storage[slot].itemId) || station.storage[slot].count <= 0) continue;
                    int rocketItemId = station.storage[slot].itemId;
                    int toUse = Math.Min(maxPerStation, station.storage[slot].count);
                    int used = 0;
                    for (int r = 0; r < toUse; r++) {
                        if (sphere.GetAutoNodeCount() <= 0) break;
                        DysonNode node = sphere.GetAutoDysonNode(si * 17 + r);
                        if (node == null) break;
                        sphere.OrderConstructSp(node);
                        sphere.ConstructSp(node);
                        used++;
                    }
                    if (used > 0) {
                        station.storage[slot].count -= used;
                        if (consumeReg != null) { lock (consumeReg) consumeReg[rocketItemId] += used; }
                    }
                }
            }
        }
        private static bool IsShellReady(DysonShell shell)
        {
            if (shell == null || shell.nodes == null || shell.frames == null) return false;
            foreach (var node in shell.nodes) if (node.sp < node.spMax) return false;
            foreach (var frame in shell.frames) if (frame.spA + frame.spB < frame.spMax) return false;
            return true;
        }
        private static void LaunchSails(PlanetFactory factory, DysonSphere sphere, int[] consumeReg, long tick)
        {
            int maxPerStation = StationLaunchSystemPlugin.SailsPerTick.Value * 10; 
            if (maxPerStation <= 0) return;
            var shells = GetShellCache(sphere, tick);
            if (shells.Count == 0) return;
            int sailItemId = -1;
            int[] sphereProdReg = sphere.productRegister;
            for (int si = 1; si < factory.transport.stationCursor; si++) {
                var station = factory.transport.stationPool[si];
                if (station == null || station.id != si || !station.isStellar || station.isCollector) continue;
                int stationUsed = 0;
                for (int slot = 0; slot < station.storage.Length; slot++) {
                    if (station.storage[slot].localLogic != ELogisticStorage.None) continue;
                    if (!IsSailItem(station.storage[slot].itemId) || station.storage[slot].count <= 0) continue;
                    sailItemId = station.storage[slot].itemId;
                    int toTake = Math.Min(maxPerStation - stationUsed, station.storage[slot].count);
                    if (toTake <= 0) continue;
                    int actualTaken = 0;
                    foreach (var shellInfo in shells) {
                        var shell = shellInfo.shell;
                        for (int ni = 0; ni < shell.nodes.Count; ni++) {
                            int maxCp = (shell.vertsqOffset[ni + 1] - shell.vertsqOffset[ni]) * shell.cpPerVertex;
                            while (shell.nodecps[ni] < maxCp) {
                                if (actualTaken >= toTake) break;
                                shell.Construct(ni, true);
                                actualTaken++;
                                if (sphereProdReg != null) { lock (sphereProdReg) sphereProdReg[11903]++; }
                            }
                            if (actualTaken >= toTake) break;
                        }
                        if (actualTaken >= toTake) break;
                    }
                    if (actualTaken > 0) {
                        station.storage[slot].count -= actualTaken;
                        stationUsed += actualTaken;
                        if (consumeReg != null) { lock (consumeReg) consumeReg[sailItemId] += actualTaken; }
                    }
                    if (stationUsed >= maxPerStation) break;
                }
            }
        }
    }
}
