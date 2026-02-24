using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;

namespace DistributeWarpers
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class DistributeWarpersPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.DistributeWarpers";
        public const string NAME = "DistributeWarpers";
        public const string VERSION = "1.1.0";

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<int> TargetWarperCount;
        public static ConfigEntry<int> CheckInterval;

        private void Awake()
        {
            Log.Init(Logger);
            InitConfig(Config);
            
            var harmony = new Harmony(GUID);
            TickManager.Patch(harmony);
            DistributeWarpersPatcher.Init();
            
            Log.Info($"{NAME} v{VERSION} loaded and refactored!");
        }

        private void InitConfig(ConfigFile confFile)
        {
            ModEnabled = confFile.Bind("General", "Enabled", true, "Enable/disable automatic warper distribution");
            TargetWarperCount = confFile.Bind("General", "TargetCount", 50, "How many warpers each station should try to maintain in its internal slot (max 50)");
            CheckInterval = confFile.Bind("General", "CheckInterval", 120, "How often (in ticks) to check and distribute warpers on a planet (60 ticks = 1 second)");
        }
    }

    public static class DistributeWarpersPatcher
    {
        public static void Init()
        {
            TickManager.OnSlowTick += OnSlowTick;
        }

        private static void OnSlowTick()
        {
            if (!DistributeWarpersPlugin.ModEnabled.Value) return;
            if (GameMain.data == null || GameMain.data.factories == null) return;

            foreach (var factory in GameMain.data.factories)
            {
                if (factory?.transport != null)
                {
                    DistributeWarpersOnPlanet(factory.transport);
                }
            }
        }

        private static void DistributeWarpersOnPlanet(PlanetTransport transport)
        {
            if (transport == null || transport.factory == null) return;

            const int warperId = 1210;
            int totalWarpersInCargo = 0;

            // 1. Collect all available warpers from cargo slots on the planet
            for (int i = 1; i < transport.stationCursor; i++)
            {
                var station = transport.stationPool[i];
                if (station == null || station.id != i || station.isCollector) continue;

                for (int j = 0; j < station.storage.Length; j++)
                {
                    if (station.storage[j].itemId == warperId && station.storage[j].count > 0)
                    {
                        totalWarpersInCargo += station.storage[j].count;
                    }
                }
            }

            if (totalWarpersInCargo <= 0) return;

            // 2. Distribute to internal warper slots of all stations
            for (int i = 1; i < transport.stationCursor; i++)
            {
                var station = transport.stationPool[i];
                if (station == null || station.id != i || station.isCollector || !station.isStellar) continue;

                int target = Math.Min(DistributeWarpersPlugin.TargetWarperCount.Value, 50);
                int currentWarpers = station.warperCount;
                int needed = target - currentWarpers;
                
                if (needed <= 0) continue;

                int toAdd = Math.Min(needed, totalWarpersInCargo);
                if (toAdd <= 0) continue;

                // Remove from cargo slots
                int removed = TakeWarpersFromPlanetCargo(transport, toAdd);
                station.warperCount += removed;
                totalWarpersInCargo -= removed;

                if (totalWarpersInCargo <= 0) break;
            }
        }

        private static int TakeWarpersFromPlanetCargo(PlanetTransport transport, int count)
        {
            int remaining = count;
            const int warperId = 1210;

            for (int i = 1; i < transport.stationCursor; i++)
            {
                var station = transport.stationPool[i];
                if (station == null || station.id != i || station.isCollector) continue;

                for (int j = 0; j < station.storage.Length; j++)
                {
                    if (station.storage[j].itemId == warperId && station.storage[j].count > 0)
                    {
                        int take = Math.Min(remaining, station.storage[j].count);
                        station.storage[j].count -= take;
                        remaining -= take;
                        if (remaining <= 0) return count;
                    }
                }
            }
            return count - remaining;
        }
    }
}
