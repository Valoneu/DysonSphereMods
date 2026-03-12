using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
public enum EStarType { MainSeqStar, GiantStar, WhiteDwarf, NeutronStar, BlackHole }
public enum ESpectrType { M, K, G, F, A, B, O, X }
public enum EPlanetType { None, Vocano, Ocean, Desert, Ice, Gas }
[Flags]
public enum EPlanetSingularity { 
    None = 0, TidalLocked = 1, LaySide = 2, ClockwiseRotate = 4, MultipleSatellites = 8, TidalLocked2 = 16, TidalLocked4 = 32
}
public enum EThemeDistribute { Default, Birth, Interstellar }
public enum EAstroType { Planet, Star, Station }
public enum EVeinType : byte
{
  None, Iron, Copper, Silicon, Titanium, Stone, Coal, Oil, Fireice, Diamond, Fractal, Organic, Grating, Stalagmite, Magnet, Max,
  Bamboo = 13, Silicium = 3
}
public struct VectorLF2 {
    public double x, y;
    public VectorLF2(double x, double y) { this.x = x; this.y = y; }
}
public class AstroOrbitData {
    public float orbitRadius;
    public float orbitInclination;
    public float orbitLongitude;
    public float orbitPhase;
    public double orbitalPeriod;
    public UnityEngine.Quaternion orbitRotation;
    public UnityEngine.Vector3 orbitNormal;
}
public class GalaxyData {
    public int seed;
    public int starCount;
    public int habitableCount;
    public StarData[] stars;
    public AstroData[] astrosData = new AstroData[25700];
    public int birthStarId;
    public int birthPlanetId;
    public StarGraphNode[] graphNodes;
    public void UpdatePoses(double time) {}
}
public class StarGraphNode {
    public StarData star;
    public int index;
    public List<StarGraphNode> lines = new List<StarGraphNode>();
    public List<StarGraphNode> conns = new List<StarGraphNode>();
    public UnityEngine.Vector3 pos;
    public StarGraphNode(StarData star) { 
        this.star = star; 
        this.index = star.index;
        this.pos = new UnityEngine.Vector3((float)star.position.x, (float)star.position.y, (float)star.position.z);
    }
}
public class StarData {
    public int id;
    public int astroId => id * 100;
    public int index;
    public float resourceCoef;
    public string name;
    public string overrideName;
    public EStarType type;
    public ESpectrType spectr;
    public float mass;
    public float age;
    public float lifetime;
    public float radius;
    public float temperature;
    public float luminosity;
    public float habitableRadius;
    public float lightZoneRadius;
    public float lightBalanceRadius;
    public float orbitScaler;
    public float physicsRadius;
    public float solarLuminosity;
    public float level;
    public float safetyFactor;
    public int initialHiveCount;
    public int planetCount;
    public VectorLF3 uPosition;
    public VectorLF3 position;
    public PlanetData[] planets;
    public GalaxyData galaxy;
    public int seed;
    public float dysonRadius;
    public float acdiskRadius;
    public float classFactor;
    public int hivePatternLevel;
    public int maxHiveCount;
    public float asterBelt1Radius;
    public float asterBelt2Radius;
    public float asterBelt1OrbitIndex;
    public float asterBelt2OrbitIndex;
    public bool epicHive;
    public AstroOrbitData[] hiveAstroOrbits = new AstroOrbitData[0];
    public UnityEngine.Color color;
}
public struct AstroData {
    public int id;
    public int parentId;
    public EAstroType type;
    public VectorLF3 uPos;
    public VectorLF3 uPosNext;
    public UnityEngine.Quaternion uRot;
    public UnityEngine.Quaternion uRotNext;
    public float uRadius;
}
public class PlanetData {
    public int id;
    public int index;
    public int orbitId;
    public int orbitAround;
    public int orbitIndex;
    public int number;
    public string name;
    public string overrideName;
    public int theme;
    public float radius;
    public float scale = 1f;
    public float orbitRadius;
    public float orbitInclination;
    public float orbitLongitude;
    public double orbitalPeriod;
    public float orbitPhase;
    public double rotationPeriod;
    public float rotationPhase;
    public float obliquity;
    public float rotationOffset;
    public float sunDistance;
    public float habitableBias;
    public float temperatureBias;
    public float luminosity;
    public float gravity;
    public int precision;
    public int segment;
    public int style;
    public bool levelized;
    public bool iceFlag;
    public float[] gasHeatValues;
    public double gasTotalHeat;
    public EPlanetType type;
    public EPlanetSingularity singularity;
    public int seed;
    public int infoSeed;
    public StarData star;
    public GalaxyData galaxy;
    public PlanetData orbitAroundPlanet;
    public UnityEngine.Quaternion runtimeOrbitRotation;
    public UnityEngine.Quaternion runtimeSystemRotation;
    public float landPercent;
    public float realRadius => radius * scale;
    public int algoId;
    public double mod_x;
    public double mod_y;
    public float ionHeight;
    public float windStrength;
    public float waterHeight;
    public int waterItemId;
    public int[] gasItems;
    public float[] gasSpeeds;
    public Dictionary<EVeinType, long> VeinAmounts = new Dictionary<EVeinType, long>();
}
public class ThemeProto {
    public int ID;
    public string Name;
    public string DisplayName;
    public int[] RareVeins = new int[0];
    public int WaterItemId;
    public float[] RareSettings = new float[0];
    public int[] VeinSpot = new int[0];
    public float[] VeinCount = new float[0];
    public float[] VeinOpacity = new float[0];
    public static int[] themeIds = new int[0];
    public EThemeDistribute Distribute;
    public float Temperature;
    public EPlanetType PlanetType;
    public int[] Algos;
    public UnityEngine.Vector2 ModX;
    public UnityEngine.Vector2 ModY;
    public float IonHeight;
    public float Wind;
    public float WaterHeight;
    public bool UseHeightForBuild;
    public bool IceFlag;
    public int[] GasItems;
    public float[] GasSpeeds;
}
public class ItemProto {
    public int ID;
    public string Name;
    public long HeatValue;
}
public static class LDB {
    public static ThemeProtoSet themes = new ThemeProtoSet();
    public static ItemProtoSet items = new ItemProtoSet();
}
public class ThemeProtoSet {
    private Dictionary<int, ThemeProto> data = new Dictionary<int, ThemeProto>();
    public void Load(string json) {
        var root = JObject.Parse(json);
        foreach (var item in root["dataArray"]) {
            var p = item.ToObject<ThemeProto>();
            data[p.ID] = p;
        }
        ThemeProto.themeIds = data.Keys.ToArray();
    }
    public ThemeProto Select(int id) => data.ContainsKey(id) ? data[id] : null;
}
public class ItemProtoSet {
    private Dictionary<int, ItemProto> data = new Dictionary<int, ItemProto>();
    public void Load(string json) {
        var root = JObject.Parse(json);
        foreach (var item in root["dataArray"]) {
            var p = item.ToObject<ItemProto>();
            data[p.ID] = p;
        }
    }
    public ItemProto Select(int id) => data.ContainsKey(id) ? data[id] : new ItemProto();
}
public static class Localization {
    public static string Translate(this string s) => s;
}
public static class Assert {
    public static void True(bool b) {}
    public static void NotNull(object o) {}
    public static void Positive(double a) {}
}
public static class PlanetModelingManager {
    public static void Start() {}
    public static void End() {}
    public static void Update() {}
    public static float gasCoef = 1f;
}
public class GameDesc {
    public int galaxyAlgo = 20210318;
    public int galaxySeed;
    public int starCount;
    public float resourceMultiplier = 1f;
    public CombatSettings combatSettings = new CombatSettings();
    public bool isRareResource;
    public int[] savedThemeIds = null;
    public Version creationVersion = new Version(1, 0);
}
public class CombatSettings {
    public float aggressiveness = 1f;
    public float initialLevel;
    public float initialGrowth = 1f;
    public float initialColonize = 1f;
    public float maxDensity = 1f;
    public float growthSpeedFactor = 1f;
    public float powerThreatFactor = 1f;
    public float battleThreatFactor = 1f;
    public float battleExpFactor = 1f;
}
public static class DotNet35Locale {
    public static string GetText(string s) => s;
}
public static class Maths
{
  public const double PI = 3.1415926535897931;
  public static VectorLF3 QRotateLF(Quaternion q, VectorLF3 v) {
    double num1 = (double) q.x * 2.0; double num2 = (double) q.y * 2.0; double num3 = (double) q.z * 2.0;
    double num4 = (double) q.x * num1; double num5 = (double) q.y * num2; double num6 = (double) q.z * num3;
    double num7 = (double) q.x * num2; double num8 = (double) q.x * num3; double num9 = (double) q.y * num3;
    double num10 = (double) q.w * num1; double num11 = (double) q.w * num2; double num12 = (double) q.w * num3;
    VectorLF3 vectorLf3;
    vectorLf3.x = (1.0 - (num5 + num6)) * v.x + (num7 - num12) * v.y + (num8 + num11) * v.z;
    vectorLf3.y = (num7 + num12) * v.x + (1.0 - (num4 + num6)) * v.y + (num9 - num10) * v.z;
    vectorLf3.z = (num8 - num11) * v.x + (num9 + num10) * v.y + (1.0 - (num4 + num5)) * v.z;
    return vectorLf3;
  }
}
