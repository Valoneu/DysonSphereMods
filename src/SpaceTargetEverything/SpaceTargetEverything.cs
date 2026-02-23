using System;
using HarmonyLib;
using BepInEx;
using DysonSphereMods.Shared;

namespace SpaceTargetEverything
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class SpaceTargetEverythingPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.SpaceTargetEverything";
        public const string MOD_NAME = "SpaceTargetEverything";
        public const string MOD_VERSION = "1.0.1";

        private void Awake()
        {
            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            
            TickManager.Patch(harmony);
            TickManager.OnSlowTick += SpaceTargetEverythingPatcher.OnSlowTick;
            TickManager.OnLazyTick += SpaceTargetEverythingPatcher.OnLazyTick;

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded and patched!");
        }
    }

    public static class SpaceTargetEverythingPatcher
    {
        private static int _targetedThisLazyTick = 0;

        public static void OnSlowTick()
        {
            GameData gameData = GameMain.data;
            if (gameData == null || gameData.spaceSector == null) return;

            SpaceSector sector = gameData.spaceSector;
            if (sector.combatSpaceSystem == null || sector.combatSpaceSystem.fleets == null) return;

            var fleets = sector.combatSpaceSystem.fleets;
            int targeted = 0;

            for (int i = 1; i < fleets.cursor; i++)
            {
                if (fleets.buffer[i].id != i) continue;

                // Owner -1 is Player Mecha. Owner > 0 is a building.
                if (fleets.buffer[i].owner == 0) continue;

                // Check if target is none
                if (fleets.buffer[i].target.type == ETargetType.None) 
                {
                    int craftId = fleets.buffer[i].craftId;
                    if (craftId <= 0 || craftId >= sector.craftCursor) continue;

                    ref CraftData craft = ref sector.craftPool[craftId];
                    if (craft.id != craftId) continue;

                    // If orbiting a planet (astroId >= 1000000)
                    if (craft.astroId >= 1000000)
                    {
                        PlanetData planet = sector.galaxy?.PlanetById(craft.astroId - 1000000);
                        if (planet != null && planet.factory != null && planet.factory.enemySystem != null)
                        {
                            EnemyDFGroundSystem enemySystem = planet.factory.enemySystem;
                            if (enemySystem.units.buffer != null)
                            {
                                int foundEnemyId = 0;
                                for (int j = 1; j < enemySystem.units.cursor; j++)
                                {
                                    if (enemySystem.units.buffer[j].id != 0)
                                    {
                                        foundEnemyId = enemySystem.units.buffer[j].id;
                                        break;
                                    }
                                }

                                if (foundEnemyId != 0)
                                {
                                    fleets.buffer[i].target.type = ETargetType.Enemy;
                                    fleets.buffer[i].target.id = foundEnemyId;
                                    fleets.buffer[i].target.astroId = craft.astroId;
                                    targeted++;
                                }
                            }
                        }
                    }
                }
            }

            _targetedThisLazyTick += targeted;
        }

        public static void OnLazyTick()
        {
            if (_targetedThisLazyTick > 0)
            {
                Log.Info($"Auto-targeted {_targetedThisLazyTick} idle space fleets.");
                _targetedThisLazyTick = 0;
            }
        }
    }
}
