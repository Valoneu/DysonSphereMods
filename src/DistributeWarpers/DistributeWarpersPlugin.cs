using BepInEx;
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
        public const string VERSION = "1.0.2";

        public static BepInEx.Configuration.ConfigEntry<bool> ModEnabled;
        public static BepInEx.Configuration.ConfigEntry<int> TargetWarperCount;

        private void Awake()
        {
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable/disable automatic warper distribution");
            TargetWarperCount = Config.Bind("General", "TargetCount", 50, "How many warpers each station should try to maintain in its internal slot (max 50)");

            Log.Init(Logger);
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(StationPatch));
            Logger.LogInfo($"{NAME} v{VERSION} loaded!");
        }

        [HarmonyPatch(typeof(StationComponent))]
        public static class StationPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(StationComponent.InternalTickLocal))]
            public static void InternalTickLocal_Postfix(StationComponent __instance, PlanetFactory factory)
            {
                if (!ModEnabled.Value) return;
                
                // We only run this on one station per planet to avoid redundant checks per tick
                if (__instance.id != 1) return;
                
                var transport = factory.transport;
                if (transport == null) return;

                int warperId = 1210;
                int totalWarpersAvailable = 0;

                // 1. Collect all available warpers from cargo slots on the planet
                for (int i = 1; i < transport.stationCursor; i++)
                {
                    var station = transport.stationPool[i];
                    if (station == null || station.id != i || station.isCollector) continue;

                    for (int j = 0; j < station.storage.Length; j++)
                    {
                        if (station.storage[j].itemId == warperId && station.storage[j].count > 0)
                        {
                            totalWarpersAvailable += station.storage[j].count;
                        }
                    }
                }

                if (totalWarpersAvailable <= 0) return;

                // 2. Distribute to internal warper slots of all stations
                for (int i = 1; i < transport.stationCursor; i++)
                {
                    var station = transport.stationPool[i];
                    if (station == null || station.id != i || station.isCollector || !station.isStellar) continue;

                    int target = System.Math.Min(TargetWarperCount.Value, 50);
                    int needed = target - station.warperCount;
                    if (needed <= 0) continue;

                    int toAdd = System.Math.Min(needed, totalWarpersAvailable);
                    if (toAdd <= 0) continue;

                    // Remove from cargo slots
                    int removed = TakeWarpersFromPlanetCargo(transport, toAdd);
                    station.warperCount += removed;
                    totalWarpersAvailable -= removed;

                    if (totalWarpersAvailable <= 0) break;
                }
            }

            private static int TakeWarpersFromPlanetCargo(PlanetTransport transport, int count)
            {
                int remaining = count;
                int warperId = 1210;

                for (int i = 1; i < transport.stationCursor; i++)
                {
                    var station = transport.stationPool[i];
                    if (station == null || station.id != i || station.isCollector) continue;

                    for (int j = 0; j < station.storage.Length; j++)
                    {
                        if (station.storage[j].itemId == warperId && station.storage[j].count > 0)
                        {
                            int take = System.Math.Min(remaining, station.storage[j].count);
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
}