using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using DysonSphereMods.Shared;
namespace BottleneckUI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class BottleneckUIPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyboardShortcut> toggleKey;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;
        public static ConfigEntry<float> WindowW;
        public static ConfigEntry<float> WindowH;
        public static BottleneckNaviLine navi = new BottleneckNaviLine();
        private BottleneckScanner _scanner;
        private BottleneckWindow _window;
        private Harmony _harmony;
        private void Awake()
        {
            Log.Init(Logger);
            toggleKey = Config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.Keypad4, KeyCode.LeftControl), "Key to toggle the Bottleneck UI");
            WindowX = Config.Bind("UI", "WindowX", 200f, "Window X Position");
            WindowY = Config.Bind("UI", "WindowY", 200f, "Window Y Position");
            WindowW = Config.Bind("UI", "WindowW", 500f, "Window Width");
            WindowH = Config.Bind("UI", "WindowH", 600f, "Window Height");
            _scanner = new BottleneckScanner();
            _window = new BottleneckWindow(_scanner);
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            TickManager.Patch(_harmony);
            TickManager.OnSlowTick += OnSlowTick;
            InitKeyBinds();
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }
        private void OnDestroy()
        {
            TickManager.OnSlowTick -= OnSlowTick;
            _harmony?.UnpatchSelf();
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControlGizmo), "GameTick")]
        public static void PlayerControlGizmo_GameTick_Postfix()
        {
            BottleneckUIPlugin.navi?.GameTick();
        }
        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleBottleneckUI"))
            {
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1215,
                    key = new CombineKey((int)toggleKey.Value.MainKey, 2, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleBottleneckUI",
                    canOverride = true
                });
            }
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("KEYToggleBottleneckUI", "Toggle Bottleneck UI");
#pragma warning restore CS0618
        }
        private void Update()
        {
            if (CustomKeyBindSystem.GetKeyBind("ToggleBottleneckUI").keyValue)
            {
                _window.Toggle();
                if (_window.IsVisible)
                {
                    _scanner.Scan();
                }
            }
            _window?.Update();
        }
        private void OnSlowTick()
        {
            if (_window.IsVisible)
            {
                _scanner.Scan();
            }
        }
        private void OnGUI()
        {
            _window.OnGUI();
        }
    }
    public class BottleneckScanner
    {
        public List<BottleneckInfo> Bottlenecks { get; private set; } = new List<BottleneckInfo>();
        public int TotalScanned { get; private set; }
        public void Scan()
        {
            Bottlenecks.Clear();
            TotalScanned = 0;
            if (GameMain.data == null || GameMain.data.factories == null) return;
            foreach (var factory in GameMain.data.factories)
            {
                if (factory == null) continue;
                string planetName = factory.planet.displayName;
                var factorySystem = factory.factorySystem;
                ScanAssemblers(factory, planetName);
                ScanLabs(factory, planetName);
                ScanFractionators(factory, planetName);
                ScanMiners(factory, planetName);
                ScanEjectors(factory, planetName);
                ScanSilos(factory, planetName);
            }
        }
        private void ScanAssemblers(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.assemblerCursor; i++)
            {
                ref var assembler = ref factorySystem.assemblerPool[i];
                if (assembler.id != i || assembler.entityId == 0) continue;
                TotalScanned++;
                if (!assembler.replicating)
                {
                    string status = BottleneckHeuristics.GetAssemblerStatus(ref assembler, factory);
                    if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full"))
                    {
                        Bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = BottleneckHeuristics.GetMachineName(factory, assembler.entityId),
                            factory = factory,
                            entityId = assembler.entityId,
                            status = status,
                            protoId = factory.entityPool[assembler.entityId].protoId
                        });
                    }
                }
            }
        }
        private void ScanLabs(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.labCursor; i++)
            {
                ref var lab = ref factorySystem.labPool[i];
                if (lab.id != i || lab.entityId == 0) continue;
                if (!lab.researchMode && lab.recipeId == 0) continue;
                TotalScanned++;
                if (!lab.replicating)
                {
                    string status = BottleneckHeuristics.GetLabStatus(ref lab, factory);
                    if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full"))
                    {
                        Bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = BottleneckHeuristics.GetMachineName(factory, lab.entityId),
                            factory = factory,
                            entityId = lab.entityId,
                            status = status,
                            protoId = factory.entityPool[lab.entityId].protoId
                        });
                    }
                }
            }
        }
        private void ScanFractionators(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.fractionatorCursor; i++)
            {
                ref var frac = ref factorySystem.fractionatorPool[i];
                if (frac.id != i || frac.entityId == 0 || frac.fluidId == 0) continue;
                TotalScanned++;
                if (!frac.isWorking)
                {
                    string status = BottleneckHeuristics.GetFracStatus(ref frac, factory);
                    if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full"))
                    {
                        Bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = BottleneckHeuristics.GetMachineName(factory, frac.entityId),
                            factory = factory,
                            entityId = frac.entityId,
                            status = status,
                            protoId = factory.entityPool[frac.entityId].protoId
                        });
                    }
                }
            }
        }
        private void ScanMiners(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.minerCursor; i++)
            {
                ref var miner = ref factorySystem.minerPool[i];
                if (miner.id != i || miner.entityId == 0) continue;
                TotalScanned++;
                string status = BottleneckHeuristics.GetMinerStatus(ref miner, factory);
                if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full") && !status.Contains("No Node"))
                {
                    Bottlenecks.Add(new BottleneckInfo
                    {
                        planetName = planetName,
                        machineName = BottleneckHeuristics.GetMachineName(factory, miner.entityId),
                        factory = factory,
                        entityId = miner.entityId,
                        status = status,
                        protoId = factory.entityPool[miner.entityId].protoId
                    });
                }
            }
        }
        private void ScanEjectors(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.ejectorCursor; i++)
            {
                ref var ejector = ref factorySystem.ejectorPool[i];
                if (ejector.id != i || ejector.entityId == 0) continue;
                TotalScanned++;
                string status = BottleneckHeuristics.GetEjectorStatus(ref ejector, factory);
                if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full") && status != "No Orbit Set" && !status.Contains("No Node"))
                {
                    Bottlenecks.Add(new BottleneckInfo
                    {
                        planetName = planetName,
                        machineName = BottleneckHeuristics.GetMachineName(factory, ejector.entityId),
                        factory = factory,
                        entityId = ejector.entityId,
                        status = status,
                        protoId = factory.entityPool[ejector.entityId].protoId
                    });
                }
            }
        }
        private void ScanSilos(PlanetFactory factory, string planetName)
        {
            var factorySystem = factory.factorySystem;
            for (int i = 1; i < factorySystem.siloCursor; i++)
            {
                ref var silo = ref factorySystem.siloPool[i];
                if (silo.id != i || silo.entityId == 0) continue;
                TotalScanned++;
                string status = BottleneckHeuristics.GetSiloStatus(ref silo, factory);
                if (status != null && status != "Working" && status != "Idle" && !status.Contains("Output Full") && !status.Contains("No Node"))
                {
                    Bottlenecks.Add(new BottleneckInfo
                    {
                        planetName = planetName,
                        machineName = BottleneckHeuristics.GetMachineName(factory, silo.entityId),
                        factory = factory,
                        entityId = silo.entityId,
                        status = status,
                        protoId = factory.entityPool[silo.entityId].protoId
                    });
                }
            }
        }
    }
    public struct BottleneckInfo
    {
        public string planetName;
        public string machineName;
        public PlanetFactory factory;
        public int entityId;
        public string status;
        public int protoId;
    }
    public static class BottleneckHeuristics
    {
        public static string GetMachineName(PlanetFactory factory, int entityId)
        {
            if (entityId <= 0 || entityId >= factory.entityCursor) return "Unknown";
            int protoId = factory.entityPool[entityId].protoId;
            var item = LDB.items.Select(protoId);
            return item != null ? item.Name.Translate() : "Unknown Machine";
        }
        public static string GetPowerStatus(int pcId, PlanetFactory factory)
        {
            if (pcId <= 0) return "No Power Connection";
            if (pcId >= factory.powerSystem.consumerCursor) return "Power Error";
            var consumer = factory.powerSystem.consumerPool[pcId];
            if (consumer.requiredEnergy <= 0) return null;
            float serveRatio = 1f;
            if (consumer.networkId > 0 && consumer.networkId < factory.powerSystem.networkServes.Length)
                serveRatio = factory.powerSystem.networkServes[consumer.networkId];
            if (serveRatio <= 0.001f) return "No Power";
            if (serveRatio < 0.98f)
            {
                return $"Low Power ({serveRatio:P0})";
            }
            return null;
        }
        public static string GetAssemblerStatus(ref AssemblerComponent assembler, PlanetFactory factory)
        {
            if (assembler.recipeId == 0) return "No Recipe";
            string power = GetPowerStatus(assembler.pcId, factory);
            if (power != null) return power;
            var recipe = LDB.recipes.Select(assembler.recipeId);
            if (recipe != null)
            {
                for (int j = 0; j < recipe.Items.Length; j++)
                {
                    int reqId = recipe.Items[j];
                    if (reqId > 0 && assembler.served[j] < recipe.ItemCounts[j])
                        return "Missing " + LDB.items.Select(reqId).Name.Translate();
                }
                for (int j = 0; j < recipe.Results.Length; j++)
                {
                    int prodId = recipe.Results[j];
                    if (prodId <= 0) continue;
                    int limit = 0;
                    switch (assembler.recipeType)
                    {
                        case ERecipeType.Smelt: limit = 100; break;
                        case ERecipeType.Assemble: limit = recipe.ResultCounts[j] * 9; break;
                        default: limit = recipe.ResultCounts[j] * 19; break;
                    }
                    if (assembler.produced[j] >= limit)
                        return "Output Full: " + LDB.items.Select(prodId).Name.Translate();
                }
            }
            return "Idle";
        }
        public static string GetLabStatus(ref LabComponent lab, PlanetFactory factory)
        {
            string power = GetPowerStatus(lab.pcId, factory);
            if (power != null) return power;
            if (lab.researchMode)
            {
                for (int j = 0; j < 6; j++)
                {
                    int matrixId = LabComponent.matrixIds[j];
                    if (lab.matrixPoints[j] > 0 && lab.matrixServed[j] < lab.matrixPoints[j])
                    {
                        return "Missing " + LDB.items.Select(matrixId).Name.Translate();
                    }
                }
            }
            else
            {
                if (lab.recipeId == 0) return "No Recipe";
                var recipe = LDB.recipes.Select(lab.recipeId);
                if (recipe != null)
                {
                    for (int j = 0; j < recipe.Items.Length; j++)
                    {
                        int reqId = recipe.Items[j];
                        if (reqId > 0 && lab.served[j] < recipe.ItemCounts[j])
                            return "Missing " + LDB.items.Select(reqId).Name.Translate();
                    }
                    for (int j = 0; j < recipe.Results.Length; j++)
                    {
                        int prodId = recipe.Results[j];
                        if (prodId <= 0) continue;
                        int limit = 10 * ((lab.speedOverride + 9999) / 10000);
                        if (lab.produced[j] >= limit)
                            return "Output Full: " + LDB.items.Select(prodId).Name.Translate();
                    }
                }
            }
            return "Idle";
        }
        public static string GetFracStatus(ref FractionatorComponent frac, PlanetFactory factory)
        {
            string power = GetPowerStatus(frac.pcId, factory);
            if (power != null) return power;
            if (frac.fluidInputCount <= 0)
                return "Missing Input";
            if (frac.productOutputCount >= frac.productOutputMax)
                return "Output Full (Product)";
            if (frac.fluidOutputCount >= frac.fluidOutputMax)
                return "Output Full (Fluid)";
            return "Idle";
        }
        public static string GetMinerStatus(ref MinerComponent miner, PlanetFactory factory)
        {
            string power = GetPowerStatus(miner.pcId, factory);
            if (power != null) return power;
            if (miner.type != EMinerType.Water && miner.veinCount == 0)
                return "No Veins";
            if (miner.productCount >= 50)
                return "Output Full";
            return "Working"; 
        }
        public static string GetEjectorStatus(ref EjectorComponent ejector, PlanetFactory factory)
        {
            string power = GetPowerStatus(ejector.pcId, factory);
            if (power != null) return power;
            if (ejector.bulletCount <= 0)
                return "Missing Solar Sails";
            if (ejector.orbitId == 0)
                return "No Orbit Set";
            return "Working";
        }
        public static string GetSiloStatus(ref SiloComponent silo, PlanetFactory factory)
        {
            string power = GetPowerStatus(silo.pcId, factory);
            if (power != null) return power;
            if (silo.bulletCount <= 0)
                return "Missing Rockets";
            if (!silo.hasNode)
                return "No Node Available";
            return "Working";
        }
    }
    public class BottleneckWindow : WindowBase
    {
        private readonly BottleneckScanner _scanner;
        public BottleneckWindow(BottleneckScanner scanner) 
            : base(1215, "Bottleneck UI (Galactic)", new Rect(BottleneckUIPlugin.WindowX.Value, BottleneckUIPlugin.WindowY.Value, BottleneckUIPlugin.WindowW.Value, BottleneckUIPlugin.WindowH.Value))
        {
            _scanner = scanner;
        }
        protected override void DrawWindowHeader()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual Scan")) _scanner.Scan();
            if (GUILayout.Button("Close")) IsVisible = false;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Total Machines (All Planets): {_scanner.TotalScanned} | Bottlenecks: {_scanner.Bottlenecks.Count}");
        }
        private Dictionary<string, bool> _planetFolds = new Dictionary<string, bool>();
        private Dictionary<string, Dictionary<int, bool>> _protoFolds = new Dictionary<string, Dictionary<int, bool>>();
        protected override void DrawWindowContent()
        {
            if (_scanner.Bottlenecks.Count == 0)
            {
                GUILayout.Label("All machines across all planets are working normally!", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 20, 0) });
            }
            else
            {
                var groupedByPlanet = System.Linq.Enumerable.GroupBy(_scanner.Bottlenecks, b => b.planetName);
                foreach (var planetGroup in groupedByPlanet)
                {
                    if (!_planetFolds.ContainsKey(planetGroup.Key)) _planetFolds[planetGroup.Key] = false;
                    if (!_protoFolds.ContainsKey(planetGroup.Key)) _protoFolds[planetGroup.Key] = new Dictionary<int, bool>();
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    if (GUILayout.Button(_planetFolds[planetGroup.Key] ? "▼" : "▶", new GUIStyle(GUI.skin.label) { fixedWidth = 20 }))
                    {
                        _planetFolds[planetGroup.Key] = !_planetFolds[planetGroup.Key];
                    }
                    GUILayout.Label($"<b><size=16>{planetGroup.Key}</size></b>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft });
                    GUILayout.EndHorizontal();
                    if (_planetFolds[planetGroup.Key])
                    {
                        var groupedByProto = System.Linq.Enumerable.GroupBy(planetGroup, b => b.protoId);
                        foreach (var protoGroup in groupedByProto)
                        {
                            var first = System.Linq.Enumerable.First(protoGroup);
                            int protoId = first.protoId;
                            if (!_protoFolds[planetGroup.Key].ContainsKey(protoId)) _protoFolds[planetGroup.Key][protoId] = false;
                            Texture2D icon = LDB.items.Select(protoId)?.iconSprite?.texture;
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            GUILayout.BeginVertical();
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(_protoFolds[planetGroup.Key][protoId] ? "▼" : "▶", new GUIStyle(GUI.skin.label) { fixedWidth = 20 }))
                            {
                                _protoFolds[planetGroup.Key][protoId] = !_protoFolds[planetGroup.Key][protoId];
                            }
                            if (icon != null) GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                            GUILayout.Label($"<b>{first.machineName} ({System.Linq.Enumerable.Count(protoGroup)})</b>", new GUIStyle(GUI.skin.label) { richText = true });
                            GUILayout.EndHorizontal();
                            if (_protoFolds[planetGroup.Key][protoId])
                            {
                                foreach (var info in protoGroup)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Space(20);
                                    if (GUILayout.Button("►", GUILayout.Width(25)))
                                    {
                                        FocusOnBuilding(info.factory, info.entityId);
                                    }
                                    Color originalColor = GUI.contentColor;
                                    if (info.status.Contains("No Power") || info.status.Contains("Missing") || info.status.Contains("No Veins"))
                                        GUI.contentColor = new Color(1f, 0.4f, 0.4f);
                                    else if (info.status.Contains("Low Power") || info.status.Contains("No Recipe") || info.status.Contains("No Orbit") || info.status.Contains("No Node"))
                                        GUI.contentColor = new Color(1f, 0.8f, 0.4f);
                                    else if (info.status.Contains("Output Full"))
                                        GUI.contentColor = new Color(0.4f, 1f, 1f);
                                    GUILayout.Label(info.status);
                                    GUI.contentColor = originalColor;
                                    GUILayout.EndHorizontal();
                                }
                            }
                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
        private void FocusOnBuilding(PlanetFactory targetFactory, int entityId)
        {
            if (entityId <= 0 || targetFactory == null) return;
            if (entityId >= targetFactory.entityCursor || targetFactory.entityPool[entityId].id != entityId)
            {
                Log.Warning($"Invalid entityId: {entityId} for factory on {targetFactory.planet.displayName}");
                return;
            }
            BottleneckUIPlugin.navi.Disable(true);
            BottleneckUIPlugin.navi.planetId = targetFactory.planetId;
            BottleneckUIPlugin.navi.entityId = entityId;
            VectorLF3 pos = targetFactory.entityPool[entityId].pos;
            Vector3 targetPos = pos;
            BottleneckUIPlugin.navi.endPoint = targetPos + targetPos.normalized * 8f;
        }
        public override void OnGUI()
        {
            if (Camera.current != null && Camera.current.cameraType != CameraType.Game) return;
            if (GameMain.isPaused || UIGame.viewMode == EViewMode.MilkyWay) return;
            if (!IsVisible) return;
            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            base.OnGUI();
            GUI.backgroundColor = Color.white;
            if (Mathf.Abs(WindowRect.x - BottleneckUIPlugin.WindowX.Value) > 0.1f || 
                Mathf.Abs(WindowRect.y - BottleneckUIPlugin.WindowY.Value) > 0.1f || 
                Mathf.Abs(WindowRect.width - BottleneckUIPlugin.WindowW.Value) > 0.1f || 
                Mathf.Abs(WindowRect.height - BottleneckUIPlugin.WindowH.Value) > 0.1f)
            {
                BottleneckUIPlugin.WindowX.Value = WindowRect.x;
                BottleneckUIPlugin.WindowY.Value = WindowRect.y;
                BottleneckUIPlugin.WindowW.Value = WindowRect.width;
                BottleneckUIPlugin.WindowH.Value = WindowRect.height;
            }
        }
        public void Update()
        {
            if (GameMain.isPaused || UIGame.viewMode == EViewMode.MilkyWay) 
            {
                BottleneckUIPlugin.navi.Disable();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BottleneckUIPlugin.navi.Disable();
            }
            BottleneckUIPlugin.navi.Draw();
        }
    }
    public class BottleneckNaviLine
    {
        public Vector3 endPoint;
        public int planetId;
        public int entityId;
        public LineGizmo lineGizmo;
        public void GameTick()
        {
            if (planetId <= 0) return;
            Draw();
        }
        public void Draw()
        {
            if (planetId <= 0) return;
            if (GameMain.localPlanet != null && GameMain.localPlanet.id == planetId)
            {
                if (lineGizmo == null)
                    Enable();
                else
                    lineGizmo.Open();
                Vector3 playerPos = GameMain.mainPlayer.position;
                Vector3 startPos = playerPos + playerPos.normalized * 4f;
                if (Time.frameCount % 30 == 0)
                {
                    if ((startPos - endPoint).sqrMagnitude < 2000f)
                    {
                        Disable(true);
                        return;
                    }
                }
                if (lineGizmo != null)
                {
                    lineGizmo.startPoint = startPos;
                    lineGizmo.endPoint = endPoint;
                }
            }
            else
            {
                if (lineGizmo != null)
                    Disable();
            }
        }
        public void Enable()
        {
            if (lineGizmo != null) return;
            lineGizmo = LineGizmo.Create(1, Vector3.zero, Vector3.zero);
            lineGizmo.autoRefresh = true;
            lineGizmo.multiplier = 5f;
            lineGizmo.alphaMultiplier = 0.6f;
            lineGizmo.width = 3f;
            lineGizmo.color = Configs.builtin.gizmoColors[4];
            lineGizmo.spherical = true;
            lineGizmo.Open();
            lineGizmo.gameObject.SetActive(true);
        }
        public void Disable(bool reset = false)
        {
            if (lineGizmo != null)
            {
                lineGizmo.Close();
                lineGizmo = null;
            }
            if (!reset) return;
            planetId = 0;
            entityId = 0;
        }
    }
}