using CommonAPI;
using CommonAPI.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DSPFactorySpaceStations
{
    public class StarSpaceStationsState : IStarExtension
    {
        public static int starExtensionId;
        public static void register()
        {
            starExtensionId = CommonAPI.Systems.StarExtensionSystem.registry.Register("space_stations_state", typeof(StarSpaceStationsState));
        }

        public static StarSpaceStationsState byStar(StarData star)
        {
            return CommonAPI.Systems.StarExtensionSystem.GetExtension<StarSpaceStationsState>(star, StarSpaceStationsState.starExtensionId);
        }

        public int id;

        // FIXME: Switch to a CommonAPI Pool once no longer reusing StationComponent and its IDs
        public Dictionary<int, SpaceStationState> spaceStations;

        public void Init(StarData star)
        {
            id = star.id;
            spaceStations = new Dictionary<int, SpaceStationState>();
        }

        public void Free()
        {
            id = 0;
            foreach (var pair in spaceStations)
            {
                pair.Value.Free();
            }
            spaceStations = null;
        }

        public void Import(BinaryReader r)
        {
            id = r.ReadInt32();

            spaceStations = new Dictionary<int, SpaceStationState>();
            var spaceStationsCursor = r.ReadInt32();
            for (int i = 0; i < spaceStationsCursor; i++)
            {
                var spaceStationId = r.ReadInt32();
                if (r.ReadByte() != 1) continue;
                var spaceStationState = new SpaceStationState();
                spaceStationState.Import(r);
                spaceStations[spaceStationId] = spaceStationState;
            }
        }

        public void Export(BinaryWriter w)
        {
            w.Write(id);
            w.Write(spaceStations.Count);
            foreach (var item in spaceStations)
            {
                w.Write(item.Key);
                if (item.Value == null)
                {
                    w.Write((byte)0);
                }
                else
                {
                    w.Write((byte)1);
                    item.Value.Export(w);
                }
            }
        }
    }

    public class SpaceStationState : ISerializeState
    {
        public static SpaceStationState lookup(StarData star, int stationComponentId)
        {
           return StarSpaceStationsState.byStar(star).spaceStations[stationComponentId];
        }

        public int stationComponentId;
        public SpaceStationConstruction construction;

        public void Init(int stationComponentId)
        {
            this.stationComponentId = stationComponentId;
        }

        public void Free()
        {
            stationComponentId = 0;
        }

        public void Import(BinaryReader r)
        {
            stationComponentId = r.ReadInt32();
            r.ReadByte(); // backwards compatibility
            construction = new SpaceStationConstruction();
            construction.Import(r);
        }

        public void Export(BinaryWriter w)
        {
            w.Write(stationComponentId);
            w.Write((byte)1);
            construction.Export(w);
        }
    }

    public struct SpaceStationConstruction : ISerializeState
    {
        public Dictionary<int, int> remainingConstructionItems;

        public void FromRecipe(RecipeProto recipe, int productionRate)
        {
            remainingConstructionItems = new Dictionary<int, int>();

            // Allow no output item's rate to exceed productionRate
            var maxProductionsPerSecond = productionRate;
            for (int i = 0; i < recipe.ResultCounts.Length; i++)
            {
                maxProductionsPerSecond = Math.Min(maxProductionsPerSecond, productionRate / recipe.ResultCounts[i]);
            }
            var secondsPerProduction = recipe.TimeSpend / 60.0;

            double neededSorterMk3, neededBeltMk3, neededFrames, neededTurbines;
            switch (recipe.Type) // To support new types, also update set in UIRecipePickerPatch
            {
                case ERecipeType.Assemble:
                    var assemblerSpeedDivider = LDB.items.Select(2305).prefabDesc.assemblerSpeed / 10000.0;
                    var neededAssemblerMk3 = maxProductionsPerSecond * secondsPerProduction / assemblerSpeedDivider;
                    remainingConstructionItems.Add(2305, roundForLogistics(neededAssemblerMk3));

                    neededSorterMk3 = neededAssemblerMk3 * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 6;
                    neededFrames = neededAssemblerMk3 * 4;
                    neededTurbines = neededAssemblerMk3 / 2;
                    break;
                case ERecipeType.Chemical:
                    var chemicalPlantSpeedDivider = LDB.items.Select(2309).prefabDesc.assemblerSpeed / 10000.0;
                    var neededChemicalPlants = maxProductionsPerSecond * secondsPerProduction / chemicalPlantSpeedDivider;
                    remainingConstructionItems.Add(2309, roundForLogistics(neededChemicalPlants));

                    neededSorterMk3 = neededChemicalPlants * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 10;
                    neededFrames = neededChemicalPlants * 8;
                    neededTurbines = neededChemicalPlants;
                    break;
                case ERecipeType.Particle:
                    var particleColliderSpeedDivider = LDB.items.Select(2310).prefabDesc.assemblerSpeed / 10000.0;
                    var neededParticleColliders = maxProductionsPerSecond * secondsPerProduction / particleColliderSpeedDivider;
                    remainingConstructionItems.Add(2310, roundForLogistics(neededParticleColliders));

                    neededSorterMk3 = neededParticleColliders * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 10;
                    neededFrames = neededParticleColliders * 12;
                    neededTurbines = neededParticleColliders * 1.5;
                    break;
                case ERecipeType.Refine:
                    var refinerySpeedDivider = LDB.items.Select(2308).prefabDesc.assemblerSpeed / 10000.0;
                    var neededRefineries = maxProductionsPerSecond * secondsPerProduction / refinerySpeedDivider;
                    remainingConstructionItems.Add(2308, roundForLogistics(neededRefineries));

                    neededSorterMk3 = neededRefineries * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 8;
                    neededFrames = neededRefineries * 8;
                    neededTurbines = neededRefineries;
                    break;
                // FIXME: Debug making science cubes, right now it seems to cause integer overflow
                /*case ERecipeType.Research:
                    var labSpeedDivider = LDB.items.Select(2901).prefabDesc.assemblerSpeed / 10000.0;
                    var neededLabs = maxProductionsPerSecond * secondsPerProduction / labSpeedDivider;
                    remainingConstructionItems.Add(2901, roundForLogistics(neededLabs));

                    neededSorterMk3 = neededLabs * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 2;
                    neededFrames = neededLabs * 6;
                    neededTurbines = neededLabs / 2;
                    break;*/
                case ERecipeType.Smelt:
                    var planeSmelterSpeedDivider = LDB.items.Select(2315).prefabDesc.assemblerSpeed / 10000.0;
                    var neededPlaneSmelter = maxProductionsPerSecond * secondsPerProduction / planeSmelterSpeedDivider;
                    remainingConstructionItems.Add(2315, roundForLogistics(neededPlaneSmelter));

                    neededSorterMk3 = neededPlaneSmelter * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 6;
                    neededFrames = neededPlaneSmelter * 4;
                    neededTurbines = neededPlaneSmelter / 2;
                    break;
                default:
                    throw new Exception("recipe type unsupported, this should be unreachable thanks to UIRecipePickerPatch");
            }
            remainingConstructionItems.Add(2013, roundForLogistics(neededSorterMk3));
            remainingConstructionItems.Add(2003, roundForLogistics(neededBeltMk3));
            remainingConstructionItems.Add(1125, roundForLogistics(neededFrames));
            remainingConstructionItems.Add(1204, roundForLogistics(neededTurbines));
            Log.Info("Requesting construction items: " + string.Join(", ", remainingConstructionItems));
        }

        // Requesting less than 100 items as the `max` from a logistics station seems to
        // get rounded down to 0. So round-up. Very reasonable given we were already
        // discarding leftover constructions items.
        // FIXME: Move this logic to logistics-specific code
        private int roundForLogistics(double itemCount)
        {
            return (int)(Math.Ceiling(itemCount / 100.0) * 100.0);
        }

        public bool Complete()
        {
            foreach (var item in remainingConstructionItems)
            {
                if (item.Value > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void Free()
        {
            remainingConstructionItems = null;
        }

        public void Import(BinaryReader r)
        {
            remainingConstructionItems = new Dictionary<int, int>();

            var remainingConstructionItemsCursor = r.ReadInt32();
            for (int i = 0; i < remainingConstructionItemsCursor; i++)
            {
                var itemId = r.ReadInt32();
                var count = r.ReadInt32();
                remainingConstructionItems[itemId] = count;
            }

            Log.Info("IMPORTED A CONSTRUCTION");
        }

        public void Export(BinaryWriter w)
        {
            w.Write(remainingConstructionItems.Count);
            foreach (var item in remainingConstructionItems)
            {
                w.Write(item.Key);
                w.Write(item.Value);
            }
        }
    }
}
