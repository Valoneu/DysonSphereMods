using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
using UnityEngine;
using CommonAPI;
using CommonAPI.Systems;
namespace VesselTrails
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class VesselTrailsPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.VesselTrails";
        public const string MOD_NAME = "VesselTrails";
        public const string MOD_VERSION = "1.3.0";
        public static ConfigEntry<bool> ShowTrails;
        public static ConfigEntry<bool> ShowHoverTooltips;
        public static ConfigEntry<float> TrailOpacity;
        public static ConfigEntry<float> TrailThicknessNormal;
        public static ConfigEntry<float> TrailThicknessStarmap;
        public enum ColorMode { Material, Heatmap }
        public static ConfigEntry<ColorMode> TrailColorMode;
        public static ConfigEntry<float> HistoryMinutes;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;
        public static ConfigEntry<float> WindowW;
        public static ConfigEntry<float> WindowH;
        private static VesselTrailRenderer _renderer;
        private static VesselRouteManager _routeManager;
        private static VesselTrailsWindow _window;
        private void Awake()
        {
            ShowTrails = Config.Bind("Visuals", "ShowTrails", true, "Whether to show vessel trails.");
            ShowHoverTooltips = Config.Bind("Visuals", "ShowHoverTooltips", true, "Whether to show tooltips when hovering over trails.");
            TrailOpacity = Config.Bind("Visuals", "TrailOpacity", 0.8f, "Overall trail opacity (0.0 to 1.0).");
            TrailThicknessNormal = Config.Bind("Visuals", "TrailThicknessNormal", 1.0f, "Thickness multiplier for normal view.");
            TrailThicknessStarmap = Config.Bind("Visuals", "TrailThicknessStarmap", 1.0f, "Thickness multiplier for star map.");
            TrailColorMode = Config.Bind("Visuals", "ColorMode", ColorMode.Heatmap, "Coloring mode: Material or Heatmap.");
            HistoryMinutes = Config.Bind("General", "HistoryMinutes", 2f, "Path lifetime in minutes.");
            WindowX = Config.Bind("Internal", "WindowX", 50f, "Window X position.");
            WindowY = Config.Bind("Internal", "WindowY", 50f, "Window Y position.");
            WindowW = Config.Bind("Internal", "WindowW", 500f, "Window width.");
            WindowH = Config.Bind("Internal", "WindowH", 600f, "Window height.");
            RegisterKeyBinds();
            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            TickManager.Patch(harmony);
            harmony.PatchAll(typeof(VesselTrailsPlugin));
            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }
        private void RegisterKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleVesselTrailsUI"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1220,
                    key = new CombineKey((int)KeyCode.Keypad1, 2, ECombineKeyAction.OnceClick, false), 
                    conflictGroup = 2052,
                    name = "ToggleVesselTrailsUI",
                    canOverride = true
                });
            if (!CustomKeyBindSystem.HasKeyBind("ToggleVesselTrailsLines"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1221,
                    key = new CombineKey((int)KeyCode.Keypad3, 2, ECombineKeyAction.OnceClick, false), 
                    conflictGroup = 2052,
                    name = "ToggleVesselTrailsLines",
                    canOverride = true
                });
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("ToggleVesselTrailsUI", "Toggle Vessel Trails UI");
            ProtoRegistry.RegisterString("ToggleVesselTrailsLines", "Toggle Vessel Trails Lines");
#pragma warning restore CS0618
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            if (_routeManager == null) _routeManager = new VesselRouteManager();
            if (_window == null) _window = new VesselTrailsWindow(_routeManager);
            if (_renderer == null)
            {
                var go = new GameObject("VesselTrailRenderer");
                _renderer = go.AddComponent<VesselTrailRenderer>();
                _renderer.Init(_routeManager, _window);
                DontDestroyOnLoad(go);
            }
        }
    }
    public class VesselRouteManager
    {
        public class RoutePath
        {
            public int StarA;
            public int StarB;
            public Dictionary<int, ItemHistory> ItemHistories = new Dictionary<int, ItemHistory>();
            public float TotalVessels => ItemHistories.Values.Sum(h => h.AverageVesselCount);
            public int GetTotalTrips(float hMin) => ItemHistories.Values.Sum(h => h.GetTotalTrips(hMin));
            public void UpdateItem(int itemId, List<int> shipKeys, float historyMinutes, float interval)
            {
                if (!ItemHistories.TryGetValue(itemId, out var hist))
                {
                    hist = new ItemHistory { ItemId = itemId, FirstSeenTime = Time.time };
                    ItemHistories[itemId] = hist;
                }
                hist.RecordSample(shipKeys, interval, historyMinutes);
            }
            public void CleanUp(float historyMinutes)
            {
                float lifetime = Mathf.Max(0.5f, historyMinutes) * 60f;
                var toRemove = ItemHistories.Where(kvp => Time.time - kvp.Value.LastSeenTime > lifetime).Select(kvp => kvp.Key).ToList();
                foreach (var k in toRemove) ItemHistories.Remove(k);
            }
        }
        public class ItemHistory
        {
            public int ItemId;
            public float FirstSeenTime;
            public float LastSeenTime;
            public float AverageVesselCount; 
            private Queue<int> _history = new Queue<int>();
            public List<float> TripStartTimes = new List<float>();
            public HashSet<int> ActiveShipKeys = new HashSet<int>();
            public void RecordSample(List<int> shipKeys, float interval, float historyMinutes)
            {
                int count = shipKeys.Count;
                _history.Enqueue(count);
                float histMin = historyMinutes <= 0 ? 0.1f : historyMinutes;
                int maxSamples = Mathf.Max(1, (int)(histMin * 60f / interval));
                while (_history.Count > maxSamples) _history.Dequeue();
                AverageVesselCount = (float)_history.Sum() / _history.Count;
                LastSeenTime = Time.time;
                foreach (var key in shipKeys)
                {
                    if (!ActiveShipKeys.Contains(key))
                    {
                        TripStartTimes.Add(Time.time);
                        ActiveShipKeys.Add(key);
                    }
                }
                ActiveShipKeys.IntersectWith(shipKeys);
            }
            public int GetTotalTrips(float windowMin)
            {
                float windowSecs = windowMin * 60f;
                if (windowSecs <= 0) windowSecs = 60f; 
                float cutoff = Time.time - windowSecs;
                TripStartTimes.RemoveAll(t => t < cutoff - 120f); 
                return TripStartTimes.Count(t => t >= cutoff);
            }
            public float GetAlpha(float lifetimeSecs)
            {
                float age = Time.time - FirstSeenTime;
                float timeSinceGone = Time.time - LastSeenTime;
                float buildUp = Mathf.Clamp01(age / 10f); 
                float fadeOut = Mathf.Clamp01(1.0f - timeSinceGone / lifetimeSecs);
                return buildUp * fadeOut;
            }
            public Color GetColor(float min, float max)
            {
                if (VesselTrailsPlugin.TrailColorMode.Value == VesselTrailsPlugin.ColorMode.Heatmap)
                {
                    float logMax = Mathf.Log(max + 1f);
                    float logMin = Mathf.Log(min + 1f);
                    float logVal = Mathf.Log(AverageVesselCount + 1f);
                    float range = logMax - logMin;
                    float t = range < 0.01f ? 0f : Mathf.Clamp01((logVal - logMin) / range);
                    if (t < 0.5f) return Color.Lerp(Color.green, Color.yellow, t * 2f);
                    return Color.Lerp(Color.yellow, Color.red, (t - 0.5f) * 2f);
                }
                return GetItemColor(ItemId);
            }
            public static Color GetItemColor(int itemId)
            {
                switch (itemId)
                {
                    case 1001: return new Color(0.4f, 0.6f, 0.9f);
                    case 1002: return new Color(0.9f, 0.5f, 0.2f);
                    case 1003: return new Color(0.2f, 0.8f, 1.0f);
                    case 1120: return new Color(0.1f, 0.9f, 1.0f);
                    case 1121: return new Color(0.2f, 0.2f, 1.0f);
                    case 1122: return new Color(1.0f, 0.1f, 0.1f);
                    case 1210: return new Color(0.1f, 1.0f, 0.3f);
                    case 6006: return Color.white;
                    default: return new Color(0.4f, 0.7f, 1.0f); 
                }
            }
        }
        public Dictionary<(int, int), RoutePath> RoutePaths { get; } = new Dictionary<(int, int), RoutePath>();
        public float GlobalMaxTraffic { get; private set; } = 1f;
        public float GlobalMinTraffic { get; private set; } = 0f;
        public VesselRouteManager()
        {
            TickManager.OnSlowTick += UpdateData;
        }
        private void UpdateData()
        {
            if (GameMain.data == null || GameMain.data.galacticTransport == null) return;
            var transport = GameMain.data.galacticTransport;
            var currentVessels = new Dictionary<(int, int, int), List<int>>();
            for (int i = 1; i < transport.stationCursor; i++)
            {
                var s = transport.stationPool[i];
                if (s == null || s.id <= 0 || s.workShipDatas == null) continue;
                for (int j = 0; j < s.workShipDatas.Length; j++)
                {
                    var ship = s.workShipDatas[j];
                    if (ship.otherGId <= 0) continue;
                    int starA = ship.planetA / 100;
                    int starB = ship.planetB / 100;
                    if (starA == starB || starA <= 0 || starB <= 0) continue;
                    var routeKey = starA < starB ? (starA, starB, ship.itemId) : (starB, starA, ship.itemId);
                    if (!currentVessels.TryGetValue(routeKey, out var list))
                    {
                        list = new List<int>();
                        currentVessels[routeKey] = list;
                    }
                    int shipKey = (s.gid << 16) | (j & 0xFFFF);
                    list.Add(shipKey);
                }
            }
            float historyMin = VesselTrailsPlugin.HistoryMinutes.Value;
            var seenThisFrame = new HashSet<(int, int, int)>();
            foreach (var kvp in currentVessels)
            {
                var key = (kvp.Key.Item1, kvp.Key.Item2);
                if (!RoutePaths.TryGetValue(key, out var path))
                {
                    path = new RoutePath { StarA = kvp.Key.Item1, StarB = kvp.Key.Item2 };
                    RoutePaths[key] = path;
                }
                path.UpdateItem(kvp.Key.Item3, kvp.Value, historyMin, 1.0f);
                seenThisFrame.Add(kvp.Key);
            }
            foreach (var path in RoutePaths.Values)
            {
                foreach (var itemId in path.ItemHistories.Keys)
                {
                    if (!seenThisFrame.Contains((path.StarA, path.StarB, itemId)))
                    {
                        path.ItemHistories[itemId].RecordSample(new List<int>(), 1.0f, historyMin);
                    }
                }
            }
            GlobalMaxTraffic = 0f;
            GlobalMinTraffic = float.MaxValue;
            var toRemove = new List<(int, int)>();
            foreach (var kvp in RoutePaths)
            {
                kvp.Value.CleanUp(historyMin);
                if (kvp.Value.ItemHistories.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }
                foreach (var hist in kvp.Value.ItemHistories.Values)
                {
                    GlobalMaxTraffic = Mathf.Max(GlobalMaxTraffic, hist.AverageVesselCount);
                    GlobalMinTraffic = Mathf.Min(GlobalMinTraffic, hist.AverageVesselCount);
                }
            }
            foreach (var k in toRemove) RoutePaths.Remove(k);
            if (GlobalMinTraffic == float.MaxValue) GlobalMinTraffic = 0f;
        }
    }
    public class VesselTrailRenderer : MonoBehaviour
    {
        private static Material _trailMaterial;
        private VesselRouteManager _manager;
        private VesselTrailsWindow _window;
        private VesselRouteManager.RoutePath _hoveredRoute = null;
        private Vector2 _mousePos;
        private Vector2 _hoverScrollPos;
        public void Init(VesselRouteManager manager, VesselTrailsWindow window)
        {
            _manager = manager;
            _window = window;
        }
        private void Update()
        {
            if (CustomKeyBindSystem.GetKeyBind("ToggleVesselTrailsUI").keyValue)
            {
                _window.Toggle();
            }
            if (CustomKeyBindSystem.GetKeyBind("ToggleVesselTrailsLines").keyValue)
            {
                VesselTrailsPlugin.ShowTrails.Value = !VesselTrailsPlugin.ShowTrails.Value;
            }
        }
        private void LateUpdate()
        {
            if (_manager == null) return;
            var starmap = UIRoot.instance?.uiGame?.starmap;
            bool starmapActive = starmap != null && starmap.active;
            Camera cam = starmapActive ? starmap.screenCamera : Camera.main;
            _hoveredRoute = null;
            float minHoverDist = 0.05f;
            _mousePos = Input.mousePosition;
            Ray mouseRay = cam != null ? cam.ScreenPointToRay(_mousePos) : new Ray();
            int hoverMask = (1 << 0) | (1 << 9) | (1 << 14) | (1 << 15) | (1 << 24) | (1 << 25) | (1 << 31); 
            foreach (var kvp in _manager.RoutePaths)
            {
                if (cam != null)
                {
                    Vector3 pA = GetStarVPos(kvp.Value.StarA, starmapActive);
                    Vector3 pB = GetStarVPos(kvp.Value.StarB, starmapActive);
                    float d = DistanceRayToSegment(mouseRay, pA, pB);
                    float midDist = Vector3.Distance(cam.transform.position, (pA + pB) * 0.5f);
                    float screenD = d / midDist;
                    if (screenD < minHoverDist)
                    {
                        Vector3 dirSeg = pB - pA;
                        float t_hover = Mathf.Clamp01(Vector3.Dot(mouseRay.origin + mouseRay.direction * midDist - pA, dirSeg) / Vector3.Dot(dirSeg, dirSeg));
                        Vector3 closestPointOnSegment = pA + t_hover * dirSeg;
                        float distToPoint = Vector3.Distance(cam.transform.position, closestPointOnSegment);
                        bool occluded = false;
                        if (Physics.Raycast(cam.transform.position, (closestPointOnSegment - cam.transform.position).normalized, out RaycastHit hit, distToPoint, hoverMask))
                        {
                            if (hit.distance < distToPoint * 0.99f) occluded = true;
                        }
                        if (!occluded)
                        {
                            if (starmapActive)
                            {
                                if (starmap != null && starmap.planetUIs != null)
                                {
                                    foreach (var planetUI in starmap.planetUIs)
                                    {
                                        if (planetUI == null || !planetUI.active || planetUI.planetRenderer == null) continue;
                                        Vector3 pPos = planetUI.planetRenderer.transform.position;
                                        float distToPlanet = Vector3.Distance(cam.transform.position, pPos);
                                        if (distToPlanet < distToPoint * 0.99f)
                                        {
                                            float dToRay = Vector3.Cross(mouseRay.direction, pPos - mouseRay.origin).magnitude;
                                            float planetRadius = planetUI.planet.realRadius * 0.00025f * 2.0f;
                                            if (dToRay < planetRadius) { occluded = true; break; }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var uni = GameMain.universeSimulator;
                                if (uni != null && uni.planetSimulators != null)
                                {
                                    foreach (var sim in uni.planetSimulators)
                                    {
                                        if (sim == null || sim.planetData == null) continue;
                                        Vector3 pos = sim.transform.position;
                                        float distToPlanet = Vector3.Distance(cam.transform.position, pos);
                                        if (distToPlanet < distToPoint * 0.99f)
                                        {
                                            float dToRay = Vector3.Cross(mouseRay.direction, pos - mouseRay.origin).magnitude;
                                            float planetRadius = sim.planetData.realRadius * 0.00025f;
                                            if (dToRay < planetRadius * 2.0f) { occluded = true; break; }
                                        }
                                    }
                                }
                            }
                        }
                        if (occluded) continue;
                        minHoverDist = screenD;
                        _hoveredRoute = kvp.Value;
                    }
                }
            }
        }
        private float DistanceRayToSegment(Ray ray, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            Vector3 f = a - ray.origin;
            float a1 = Vector3.Dot(d, d);
            float b1 = Vector3.Dot(d, ray.direction);
            float c1 = Vector3.Dot(ray.direction, ray.direction);
            float d1 = Vector3.Dot(d, f);
            float e1 = Vector3.Dot(ray.direction, f);
            float denom = a1 * c1 - b1 * b1;
            float s = denom < 0.0001f ? 0 : Mathf.Clamp01((b1 * e1 - c1 * d1) / denom);
            float t = (b1 * s + e1) / c1;
            if (t < 0) t = 0;
            return Vector3.Distance(a + s * d, ray.origin + t * ray.direction);
        }
        private void OnGUI()
        {
            if (_window != null) _window.OnGUI();
            if (_hoveredRoute != null && VesselTrailsPlugin.ShowHoverTooltips.Value)
            {
                DrawHoverTooltip();
            }
        }
        private void DrawHoverTooltip()
        {
            string starAName = GameMain.galaxy.StarById(_hoveredRoute.StarA)?.displayName ?? $"Star {_hoveredRoute.StarA}";
            string starBName = GameMain.galaxy.StarById(_hoveredRoute.StarB)?.displayName ?? $"Star {_hoveredRoute.StarB}";
            float histMin = VesselTrailsPlugin.HistoryMinutes.Value;
            string histStr = histMin <= 0 ? "Real-time" : $"Last {histMin:F1}m";
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                richText = true,
                padding = new RectOffset(10, 10, 10, 10)
            };
            style.normal.background = Texture2D.whiteTexture; 
            GUI.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.95f);
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 13
            };
            float headerHeight = 95;
            float itemHeight = 24;
            float tooltipWidth = 350;
            float totalContentHeight = headerHeight + _hoveredRoute.ItemHistories.Count * itemHeight + 10;
            float maxTooltipHeight = Screen.height * 0.7f;
            float finalTooltipHeight = Mathf.Min(totalContentHeight, maxTooltipHeight);
            float x = _mousePos.x + 20;
            float y = Screen.height - _mousePos.y - finalTooltipHeight - 10;
            if (y < 10) y = Screen.height - _mousePos.y + 20;
            if (x + tooltipWidth > Screen.width) x = Screen.width - tooltipWidth - 10;
            if (y + finalTooltipHeight > Screen.height) y = Screen.height - finalTooltipHeight - 10;
            Rect tooltipRect = new Rect(x, y, tooltipWidth, finalTooltipHeight);
            GUILayout.BeginArea(tooltipRect, style);
            GUILayout.Label($"<b>{starAName} <-> {starBName}</b>", labelStyle);
            GUILayout.Label($"<size=11><color=#aaaaaa>{histStr}</color></size>", labelStyle);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=11><i>Item</i></size>", labelStyle, GUILayout.Width(160));
            GUILayout.Label("<size=11><i>Total</i></size>", labelStyle, GUILayout.Width(50));
            GUILayout.Label("<size=11><i>/min</i></size>", labelStyle, GUILayout.Width(50));
            GUILayout.Label("<size=11><i>Load</i></size>", labelStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();
            if (totalContentHeight > maxTooltipHeight) 
            {
                _hoverScrollPos = GUILayout.BeginScrollView(_hoverScrollPos, GUILayout.Height(maxTooltipHeight - headerHeight));
            }
            foreach (var hist in _hoveredRoute.ItemHistories.Values.OrderByDescending(h => h.AverageVesselCount))
            {
                string itemName = LDB.items.Select(hist.ItemId)?.name ?? $"Item {hist.ItemId}";
                int total = hist.GetTotalTrips(histMin);
                float perMin = total / Mathf.Max(1f, histMin);
                GUILayout.BeginHorizontal();
                GUILayout.Label(itemName, labelStyle, GUILayout.Width(160));
                GUILayout.Label($"{total}", labelStyle, GUILayout.Width(50));
                GUILayout.Label($"{perMin:F1}", labelStyle, GUILayout.Width(50));
                GUILayout.Label($"{hist.AverageVesselCount:F1}", labelStyle, GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }
            if (totalContentHeight > maxTooltipHeight) GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.backgroundColor = Color.white;
        }
        private Vector3 GetStarVPos(int starId, bool isStarmap)
        {
            if (isStarmap)
            {
                var starmap = UIRoot.instance?.uiGame?.starmap;
                if (starmap != null && starmap.starUIs != null && starId > 0 && starId <= starmap.starUIs.Length)
                {
                    var starUI = starmap.starUIs[starId - 1];
                    if (starUI != null && starUI.starObject != null) return starUI.starObject.vpos;
                }
            }
            else
            {
                var uni = GameMain.universeSimulator;
                if (uni != null && uni.starSimulators != null && starId > 0 && starId <= uni.starSimulators.Length)
                {
                    var sim = uni.starSimulators[starId - 1];
                    if (sim != null) return sim.transform.position;
                }
            }
            return Vector3.zero;
        }
        private void OnRenderObject()
        {
            if (!VesselTrailsPlugin.ShowTrails.Value || _manager == null || _manager.RoutePaths.Count == 0 || GameMain.data == null) return;
            Camera cam = Camera.current;
            if (cam == null || cam.cameraType != CameraType.Game) return;
            var starmap = UIRoot.instance?.uiGame?.starmap;
            bool starmapActive = starmap != null && starmap.active;
            if (starmapActive) { if (cam != starmap.screenCamera) return; }
            else { if (cam != Camera.main) return; }
            if (_trailMaterial == null)
            {
                _trailMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                _trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); 
                _trailMaterial.SetInt("_ZWrite", 0);
                _trailMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                _trailMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _trailMaterial.renderQueue = 3100;
            }
            Vector3 camPos = cam.transform.position;
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix * Matrix4x4.Translate(camPos);
            _trailMaterial.SetPass(0);
            float baseOpacity = VesselTrailsPlugin.TrailOpacity.Value;
            float lifetime = Mathf.Max(1f, VesselTrailsPlugin.HistoryMinutes.Value * 60f);
            float thicknessMult = starmapActive ? VesselTrailsPlugin.TrailThicknessStarmap.Value : VesselTrailsPlugin.TrailThicknessNormal.Value;
            GL.Begin(GL.QUADS);
            foreach (var route in _manager.RoutePaths.Values)
            {
                Vector3 pA = GetStarVPos(route.StarA, starmapActive);
                Vector3 pB = GetStarVPos(route.StarB, starmapActive);
                if (pA == Vector3.zero || pB == Vector3.zero) continue;
                Vector3 relA = pA - camPos;
                Vector3 relB = pB - camPos;
                Vector3 dir = (pB - pA).normalized;
                Vector3 camToMid = (pA + pB) * 0.5f - camPos;
                float distToCam = camToMid.magnitude;
                float t_dist = Mathf.Clamp01(Vector3.Dot(-relA, relB - relA) / Vector3.Dot(relB - relA, relB - relA));
                float distToEnds = Vector3.Distance(Vector3.zero, relA + t_dist * (relB - relA));
                distToCam = Mathf.Min(distToCam, distToEnds);
                float screenFraction = starmapActive ? 0.00004f : 0.00005f;
                float baseWidth = distToCam * screenFraction * thicknessMult;
                baseWidth = Mathf.Max(baseWidth, starmapActive ? 0.005f : 0.05f);
                Vector3 up = Vector3.Cross(dir, Vector3.up).normalized;
                if (up.sqrMagnitude < 0.0001f) up = Vector3.Cross(dir, Vector3.right).normalized;
                Vector3 side = Vector3.Cross(dir, up).normalized;
                var items = route.ItemHistories.Values.ToList();
                bool isMaterialMode = VesselTrailsPlugin.TrailColorMode.Value == VesselTrailsPlugin.ColorMode.Material;
                if (isMaterialMode)
                {
                    int count = items.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var hist = items[i];
                        float alpha = baseOpacity * hist.GetAlpha(lifetime);
                        if (alpha <= 0.001f) continue;
                        Color color = hist.GetColor(_manager.GlobalMinTraffic, _manager.GlobalMaxTraffic);
                        color.a = alpha;
                        float angle = (float)i / count * Mathf.PI * 2f;
                        Vector3 offset = (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * baseWidth * 1.2f;
                        if (count == 1) offset = Vector3.zero;
                        GL.Color(color);
                        DrawPrism(relA + offset, relB + offset, side * baseWidth, up * baseWidth);
                    }
                }
                else
                {
                    float maxAlpha = 0f;
                    foreach(var itemHist in items) maxAlpha = Mathf.Max(maxAlpha, itemHist.GetAlpha(lifetime));
                    float alpha = baseOpacity * maxAlpha;
                    if (alpha <= 0.001f) continue;
                    float logMax = Mathf.Log(_manager.GlobalMaxTraffic + 1f);
                    float logMin = Mathf.Log(_manager.GlobalMinTraffic + 1f);
                    float logVal = Mathf.Log(route.TotalVessels + 1f);
                    float range = logMax - logMin;
                    float t = range < 0.01f ? 0f : Mathf.Clamp01((logVal - logMin) / range);
                    Color color = (t < 0.5f) ? Color.Lerp(Color.green, Color.yellow, t * 2f) : Color.Lerp(Color.yellow, Color.red, (t - 0.5f) * 2f);
                    color.a = alpha;
                    GL.Color(color);
                    DrawPrism(relA, relB, side * baseWidth, up * baseWidth);
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        private void DrawPrism(Vector3 posA, Vector3 posB, Vector3 w, Vector3 h)
        {
            GL.Vertex(posA + h - w); GL.Vertex(posA + h + w);
            GL.Vertex(posB + h + w); GL.Vertex(posB + h - w);
            GL.Vertex(posA - h - w); GL.Vertex(posA - h + w);
            GL.Vertex(posB - h + w); GL.Vertex(posB - h - w);
            GL.Vertex(posA - h - w); GL.Vertex(posA + h - w);
            GL.Vertex(posB + h - w); GL.Vertex(posB - h - w);
            GL.Vertex(posA - h + w); GL.Vertex(posA + h + w);
            GL.Vertex(posB + h + w); GL.Vertex(posB - h + w);
        }
    }
    public class VesselTrailsWindow : WindowBase
    {
        private readonly VesselRouteManager _manager;
        public VesselTrailsWindow(VesselRouteManager manager) 
            : base(9922, "Vessel Trails Logistics", new Rect(
                VesselTrailsPlugin.WindowX.Value,
                VesselTrailsPlugin.WindowY.Value,
                VesselTrailsPlugin.WindowW.Value,
                VesselTrailsPlugin.WindowH.Value
            ))
        {
            _manager = manager;
        }
        protected override void DrawWindowHeader()
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1.0f);
            GUILayout.BeginVertical();
            GUILayout.Label("SETTINGS", headerStyle);
            VesselTrailsPlugin.ShowTrails.Value = GUILayout.Toggle(VesselTrailsPlugin.ShowTrails.Value, "Show Trails");
            VesselTrailsPlugin.ShowHoverTooltips.Value = GUILayout.Toggle(VesselTrailsPlugin.ShowHoverTooltips.Value, "Show Hover Tooltips");
            GUILayout.BeginHorizontal();
            float opacity = GUILayout.HorizontalSlider(VesselTrailsPlugin.TrailOpacity.Value, 0f, 1f);
            VesselTrailsPlugin.TrailOpacity.Value = Mathf.Round(opacity * 10f) / 10f;
            GUILayout.Label($"Opacity: {VesselTrailsPlugin.TrailOpacity.Value:F1}", GUILayout.Width(110));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            float thicknessN = GUILayout.HorizontalSlider(VesselTrailsPlugin.TrailThicknessNormal.Value, 0.1f, 25f);
            VesselTrailsPlugin.TrailThicknessNormal.Value = Mathf.Round(thicknessN * 10f) / 10f;
            GUILayout.Label($"Thick (Cam): {VesselTrailsPlugin.TrailThicknessNormal.Value:F1}", GUILayout.Width(110));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            float thicknessS = GUILayout.HorizontalSlider(VesselTrailsPlugin.TrailThicknessStarmap.Value, 0.1f, 25f);
            VesselTrailsPlugin.TrailThicknessStarmap.Value = Mathf.Round(thicknessS * 10f) / 10f;
            GUILayout.Label($"Thick (Map): {VesselTrailsPlugin.TrailThicknessStarmap.Value:F1}", GUILayout.Width(110));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            float history = GUILayout.HorizontalSlider(VesselTrailsPlugin.HistoryMinutes.Value, 0f, 60f);
            VesselTrailsPlugin.HistoryMinutes.Value = Mathf.Round(history);
            GUILayout.Label($"History (min): {VesselTrailsPlugin.HistoryMinutes.Value:F0}", GUILayout.Width(110));
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Color Mode: {VesselTrailsPlugin.TrailColorMode.Value}"))
            {
                VesselTrailsPlugin.TrailColorMode.Value = VesselTrailsPlugin.TrailColorMode.Value == VesselTrailsPlugin.ColorMode.Heatmap 
                    ? VesselTrailsPlugin.ColorMode.Material : VesselTrailsPlugin.ColorMode.Heatmap;
            }
            GUILayout.Space(15);
            float hMin = VesselTrailsPlugin.HistoryMinutes.Value;
            int clusterTotalTrips = 0;
            float clusterTotalLoad = 0f;
            foreach (var route in _manager.RoutePaths.Values)
            {
                foreach (var hist in route.ItemHistories.Values)
                {
                    clusterTotalTrips += hist.GetTotalTrips(hMin);
                    clusterTotalLoad += hist.AverageVesselCount;
                }
            }
            float clusterTripsPerMin = clusterTotalTrips / Mathf.Max(1f, hMin);
            GUILayout.Label("CLUSTER TOTALS", headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("All Logistics Vessels", GUILayout.Width(WindowRect.width - 240));
            GUILayout.Label($"<b>{clusterTotalTrips}</b>", GUILayout.Width(50));
            GUILayout.Label($"<b>{clusterTripsPerMin:F1}</b>", GUILayout.Width(50));
            GUILayout.Label($"<b>{clusterTotalLoad:F1}</b>", GUILayout.Width(50));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            string hStr = hMin <= 0 ? "REAL-TIME" : $"LAST {hMin:F1}m";
            GUILayout.Label($"ACTIVE ROUTES ({hStr})", headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Route / Item", GUILayout.Width(WindowRect.width - 240));
            GUILayout.Label("Total", GUILayout.Width(50));
            GUILayout.Label("/min", GUILayout.Width(50));
            GUILayout.Label("Load", GUILayout.Width(50));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        protected override void DrawWindowContent()
        {
            float hMin = VesselTrailsPlugin.HistoryMinutes.Value;
            var sortedRoutes = _manager.RoutePaths.Values.OrderByDescending(r => r.GetTotalTrips(hMin)).ToList();
            GUIStyle rowStyle = new GUIStyle(GUI.skin.box);
            rowStyle.normal.background = Texture2D.whiteTexture;
            rowStyle.padding = new RectOffset(5, 5, 5, 5);
            foreach (var route in sortedRoutes)
            {
                string starAName = GameMain.galaxy.StarById(route.StarA)?.displayName ?? "Unknown";
                string starBName = GameMain.galaxy.StarById(route.StarB)?.displayName ?? "Unknown";
                GUI.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 0.2f);
                GUILayout.BeginVertical(rowStyle);
                GUILayout.Label($"<b>{starAName} -> {starBName}</b>");
                var sortedItems = route.ItemHistories.Values.OrderByDescending(h => h.GetTotalTrips(hMin)).ToList();
                foreach (var hist in sortedItems)
                {
                    string itemName = LDB.items.Select(hist.ItemId)?.name ?? "Unknown";
                    int total = hist.GetTotalTrips(hMin);
                    float perMin = total / Mathf.Max(1f, hMin);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($" <color=#aaaaaa>• {itemName}</color>", GUILayout.Width(WindowRect.width - 240));
                    GUILayout.Label($"{total}", GUILayout.Width(50));
                    GUILayout.Label($"{perMin:F1}", GUILayout.Width(50));
                    GUILayout.Label($"{hist.AverageVesselCount:F1}", GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }
        protected override void DrawWindowFooter()
        {
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.1f, 0.8f);
            if (GUILayout.Button("CLOSE")) IsVisible = false;
            GUI.backgroundColor = Color.white;
        }
        public override void OnGUI()
        {
            if (!IsVisible) return;
            float oldX = WindowRect.x;
            float oldY = WindowRect.y;
            float oldW = WindowRect.width;
            float oldH = WindowRect.height;
            base.OnGUI();
            if (Mathf.Abs(WindowRect.x - oldX) > 0.1f || Mathf.Abs(WindowRect.y - oldY) > 0.1f || 
                Mathf.Abs(WindowRect.width - oldW) > 0.1f || Mathf.Abs(WindowRect.height - oldH) > 0.1f)
            {
                VesselTrailsPlugin.WindowX.Value = WindowRect.x;
                VesselTrailsPlugin.WindowY.Value = WindowRect.y;
                VesselTrailsPlugin.WindowW.Value = WindowRect.width;
                VesselTrailsPlugin.WindowH.Value = WindowRect.height;
            }
        }
    }
}