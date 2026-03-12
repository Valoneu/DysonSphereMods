using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
public class ScannerConfig
{
    public int SeedStart { get; set; }
    public int SeedEnd { get; set; }
    public int StarCount { get; set; } = 64;
    public float ResourceMultiplier { get; set; } = 1.0f;
    public int Threads { get; set; } = Environment.ProcessorCount;
    public string OutputFolder { get; set; } = "seeds";
    public CombatConfig CombatSettings { get; set; } = new CombatConfig();
}
public class CombatConfig {
    public float Aggressiveness { get; set; } = 1.0f;
    public float InitialLevel { get; set; } = 0.0f;
    public float MaxDensity { get; set; } = 1.0f;
}
public class GalaxyResult
{
    public int Seed { get; set; }
    public int StarCount { get; set; }
    public List<StarResult> Stars { get; set; } = new List<StarResult>();
}
public class StarResult
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Spectr { get; set; }
    public float Luminosity { get; set; }
    public float DysonLuminosity { get; set; }
    public float Mass { get; set; }
    public float Temperature { get; set; }
    public float Radius { get; set; }
    public float DistanceLY { get; set; }
    public float ResourceCoefficient { get; set; }
    public float DysonRadius { get; set; }
    public float[] Position { get; set; }
    public float[] AsteroidBelts { get; set; }
    public int[] Connections { get; set; }
    public DarkFogStarData DarkFog { get; set; }
    public Dictionary<string, long> TotalVeins { get; set; } = new Dictionary<string, long>();
    public List<PlanetResult> Planets { get; set; } = new List<PlanetResult>();
}
public class DarkFogStarData {
    public int MaxHiveCount { get; set; }
    public int InitialHiveCount { get; set; }
    public float SafetyFactor { get; set; }
}
public class PlanetResult
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Theme { get; set; }
    public string OrbitAround { get; set; }
    public float OrbitRadiusAU { get; set; }
    public float OrbitalPeriod { get; set; }
    public float OrbitInclination { get; set; }
    public float SolarRatio { get; set; }
    public float WindRatio { get; set; }
    public string Ocean { get; set; }
    public List<string> Anomalies { get; set; } = new List<string>();
    public Dictionary<string, float> Gas { get; set; } = new Dictionary<string, float>();
    public Dictionary<string, long> Veins { get; set; } = new Dictionary<string, long>();
    public int StartingFogBases { get; set; }
}
class Program
{
    static Dictionary<int, string> themeNames = new Dictionary<int, string> {
        {1,"Mediterranean"}, {2,"Gas Giant"}, {3,"Gas Giant"}, {4,"Ice Giant"}, {5,"Ice Giant"},
        {6,"Volcanic Ash"}, {7,"Gelid Zephyr"}, {8,"Oceanic Jungle"}, {9,"Lava"}, {10,"Ice Field"},
        {11,"Prairies"}, {12,"Red Stone"}, {13,"Volcanic Ash"}, {14,"Oceanic Jungle"}, {15,"Gobi"},
        {16,"Desert"}, {17,"Arid Desert"}, {18,"Ice Field"}, {19,"Cyclonius"}, {20,"Barren Desert"},
        {21,"Gas Giant"}, {22,"Sulfuria"}, {23,"Glacieon"}, {24,"Halitum"}, {25,"Icefrostia"}
    };
    static Dictionary<int, string> itemNames = new Dictionary<int, string> {
        {1000,"Water"}, {1116,"Sulfuric Acid"}, {1120,"Hydrogen"}, {1121,"Deuterium"}, {1011,"Fireice"},
        {1001,"Iron"}, {1002,"Copper"}, {1003,"Silicon"}, {1004,"Titanium"}, {1005,"Stone"}, {1006,"Coal"}
    };
    static Dictionary<EVeinType, string> veinNames = new Dictionary<EVeinType, string> {
        {EVeinType.Iron, "Iron"}, {EVeinType.Copper, "Copper"}, {EVeinType.Silicon, "Silicon"},
        {EVeinType.Titanium, "Titanium"}, {EVeinType.Stone, "Stone"}, {EVeinType.Coal, "Coal"},
        {EVeinType.Oil, "Oil"}, {EVeinType.Fireice, "Fireice"}, {EVeinType.Diamond, "Diamond"},
        {EVeinType.Fractal, "Fractal"}, {EVeinType.Organic, "Organic"}, {EVeinType.Grating, "Grating"},
        {EVeinType.Stalagmite, "Stalagmite"}, {EVeinType.Magnet, "Magnet"}
    };
    static void Main(string[] args)
    {
        string configPath = args.Length > 0 ? args[0] : "scanner_config.json";
        if (!File.Exists(configPath)) return;
        var config = JsonConvert.DeserializeObject<ScannerConfig>(File.ReadAllText(configPath));
        if (!Directory.Exists(config.OutputFolder)) Directory.CreateDirectory(config.OutputFolder);
        LDB.themes.Load(File.ReadAllText("src/SeedScanner/ThemeProtoSet.json"));
        LDB.items.Load(File.ReadAllText("src/SeedScanner/ItemProtoSet.json"));
        Console.WriteLine($"Strategic Scan: seeds {config.SeedStart} to {config.SeedEnd} on {config.Threads} threads...");
        Parallel.For(config.SeedStart, config.SeedEnd + 1, new ParallelOptions { MaxDegreeOfParallelism = config.Threads }, seed =>
        {
            try {
                var gameDesc = new GameDesc {
                    galaxySeed = seed,
                    starCount = config.StarCount,
                    resourceMultiplier = config.ResourceMultiplier,
                    galaxyAlgo = 20210318,
                    combatSettings = new CombatSettings {
                        aggressiveness = config.CombatSettings.Aggressiveness,
                        initialLevel = config.CombatSettings.InitialLevel,
                        maxDensity = config.CombatSettings.MaxDensity
                    }
                };
                var galaxy = UniverseGen.CreateGalaxy(gameDesc);
                var birthStarPos = galaxy.stars[0].uPosition;
                var gRes = new GalaxyResult { Seed = seed, StarCount = galaxy.starCount };
                foreach (var star in galaxy.stars)
                {
                    if (star == null) continue;
                    var starConnections = new List<int>();
                    if (galaxy.graphNodes != null && galaxy.graphNodes[star.index] != null) {
                        foreach (var line in galaxy.graphNodes[star.index].lines) starConnections.Add(line.star.id);
                    }
                    var sRes = new StarResult {
                        Id = star.id,
                        Name = star.id == 1 ? "Birth Star" : star.name,
                        Type = star.type.ToString(),
                        Spectr = star.spectr.ToString(),
                        Luminosity = (float)Math.Round(star.luminosity, 3),
                        DysonLuminosity = (float)Math.Round(Math.Pow(star.luminosity, 0.33), 3),
                        Mass = (float)Math.Round(star.mass, 4),
                        Temperature = (float)Math.Round(star.temperature, 1),
                        Radius = (float)Math.Round(star.radius, 4),
                        ResourceCoefficient = (float)Math.Round(star.resourceCoef, 3),
                        DysonRadius = (float)Math.Round(star.dysonRadius, 3),
                        DistanceLY = (float)Math.Round((star.uPosition - birthStarPos).magnitude / 2400000.0, 2),
                        Position = new float[] { (float)Math.Round(star.position.x, 6), (float)Math.Round(star.position.y, 6), (float)Math.Round(star.position.z, 6) },
                        AsteroidBelts = new float[] { (float)Math.Round(star.asterBelt1Radius, 3), (float)Math.Round(star.asterBelt2Radius, 3) },
                        Connections = starConnections.ToArray(),
                        DarkFog = new DarkFogStarData {
                            MaxHiveCount = star.maxHiveCount,
                            InitialHiveCount = star.initialHiveCount,
                            SafetyFactor = (float)Math.Round(star.safetyFactor, 4)
                        }
                    };
                    if (star.planets != null)
                    {
                        foreach (var planet in star.planets)
                        {
                            if (planet == null) continue;
                            var pRes = new PlanetResult {
                                Id = planet.index + 1,
                                Name = planet.name.EndsWith("号星") ? planet.name.Substring(0, planet.name.Length - 2) : planet.name,
                                Type = planet.type.ToString(),
                                Theme = themeNames.ContainsKey(planet.theme) ? themeNames[planet.theme] : planet.theme.ToString(),
                                OrbitAround = planet.orbitAroundPlanet != null ? (planet.orbitAroundPlanet.index + 1).ToString() : "None",
                                OrbitRadiusAU = (float)Math.Round(planet.orbitRadius, 4),
                                OrbitalPeriod = (float)Math.Round(planet.orbitalPeriod, 2),
                                OrbitInclination = (float)Math.Round(planet.orbitInclination, 2),
                                SolarRatio = (float)Math.Round(planet.luminosity * 100f),
                                WindRatio = (float)Math.Round(planet.windStrength * 100f),
                                Ocean = planet.waterItemId == 1116 ? "Sulfuric Acid" : (planet.waterItemId == 1000 ? "Water" : "None"),
                                StartingFogBases = (int)(new DotNet35Random(planet.seed).NextDouble() * 3 + 1)
                            };
                            if (planet.type == EPlanetType.Gas) {
                                if (planet.gasItems != null) {
                                    for (int i = 0; i < planet.gasItems.Length; i++) {
                                        string iName = itemNames.ContainsKey(planet.gasItems[i]) ? itemNames[planet.gasItems[i]] : planet.gasItems[i].ToString();
                                        pRes.Gas[iName] = (float)Math.Round(planet.gasSpeeds[i], 4);
                                    }
                                }
                            } else {
                                VeinLogic.GenerateVeins(planet);
                                foreach (var kvp in planet.VeinAmounts) {
                                    string vName = veinNames.ContainsKey(kvp.Key) ? veinNames[kvp.Key] : kvp.Key.ToString();
                                    pRes.Veins[vName] = kvp.Value;
                                    if (!sRes.TotalVeins.ContainsKey(vName)) sRes.TotalVeins[vName] = 0;
                                    sRes.TotalVeins[vName] += kvp.Value;
                                }
                            }
                            if (planet.singularity != EPlanetSingularity.None) {
                                foreach (EPlanetSingularity val in Enum.GetValues(typeof(EPlanetSingularity))) {
                                    if (val != EPlanetSingularity.None && (planet.singularity & val) == val) pRes.Anomalies.Add(val.ToString());
                                }
                            }
                            sRes.Planets.Add(pRes);
                        }
                    }
                    gRes.Stars.Add(sRes);
                }
                File.WriteAllText(Path.Combine(config.OutputFolder, $"{seed}.json"), JsonConvert.SerializeObject(gRes, Formatting.Indented));
                if (seed % 10 == 0) Console.WriteLine($"Processed seed {seed}");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error on seed {seed}: {ex.Message}");
            }
        });
        Console.WriteLine("Scan complete!");
    }
}
