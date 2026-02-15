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

namespace StacksizeMultiplier
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class StacksizeMultiplierPlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.StacksizeMultiplier";
        public const string MOD_NAME = "StacksizeMultiplier";
        public const string MOD_VERSION = "1.0.0";

        public static ConfigEntry<float> GlobalItemMultiplier;
        public static ConfigEntry<float> GlobalBuildingMultiplier;
        public static ConfigEntry<float> GlobalUsefulMultiplier;
        public static ConfigEntry<string> ItemOverridesRaw;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;
        public static ConfigEntry<float> WindowW;
        public static ConfigEntry<float> WindowH;

        private bool _showWindow = false;
        private Rect _windowRect = new Rect(100, 100, 450, 600);
        private bool _isResizing = false;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;
        private Vector2 _scrollPos;
        private string _searchText = "";
        
        private static Dictionary<int, int> _originalStackSizes = new Dictionary<int, int>();
        private static Dictionary<int, float> _itemOverrides = new Dictionary<int, float>();

        private void Awake()
        {
            GlobalItemMultiplier = Config.Bind("Internal", "GlobalItemMultiplier", 1f, "Global multiplier for non-building items.");
            GlobalBuildingMultiplier = Config.Bind("Internal", "GlobalBuildingMultiplier", 1f, "Global multiplier for buildings/facilities.");
            GlobalUsefulMultiplier = Config.Bind("Internal", "GlobalUsefulMultiplier", 1f, "Global multiplier for drones and vessels.");
            ItemOverridesRaw = Config.Bind("Internal", "ItemOverrides", "", "Stored per-item overrides (ID:Value,ID:Value)");
            WindowX = Config.Bind("UI", "WindowX", 100f, "Window X position.");
            WindowY = Config.Bind("UI", "WindowY", 100f, "Window Y position.");
            WindowW = Config.Bind("UI", "WindowW", 450f, "Window width.");
            WindowH = Config.Bind("UI", "WindowH", 600f, "Window height.");
            
            _windowRect.x = WindowX.Value;
            _windowRect.y = WindowY.Value;
            _windowRect.width = WindowW.Value;
            _windowRect.height = WindowH.Value;

            LoadOverrides();
            RegisterKeyBinds();

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StacksizeMultiplierPlugin));

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }

        private void RegisterKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleStacksizeUI"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1230,
                    key = new CombineKey((int)KeyCode.Keypad2, 2, ECombineKeyAction.OnceClick, false), // 2 = Ctrl
                    conflictGroup = 0,
                    name = "ToggleStacksizeUI",
                    canOverride = true
                });

#pragma warning disable CS0618
            ProtoRegistry.RegisterString("ToggleStacksizeUI", "Toggle Stacksize Multiplier UI");
#pragma warning restore CS0618
        }

        private void LoadOverrides()
        {
            _itemOverrides.Clear();
            string raw = ItemOverridesRaw.Value;
            if (string.IsNullOrEmpty(raw)) return;

            foreach (string entry in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int id) && float.TryParse(parts[1], out float val))
                {
                    _itemOverrides[id] = val;
                }
            }
        }

        private void SaveOverrides()
        {
            string raw = string.Join(",", _itemOverrides.Select(kvp => $"{kvp.Key}:{kvp.Value:F1}"));
            ItemOverridesRaw.Value = raw;
            Config.Save();
        }

        private bool _lastKeyState = false;
        private void Update()
        {
            bool currentKey = CustomKeyBindSystem.GetKeyBind("ToggleStacksizeUI").keyValue;
            if (currentKey && !_lastKeyState)
            {
                Log.Info("ToggleStacksizeUI keybind pressed!");
                _showWindow = !_showWindow;
            }
            _lastKeyState = currentKey;

            if (_isResizing)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    _isResizing = false;
                    WindowW.Value = _windowRect.width;
                    WindowH.Value = _windowRect.height;
                    Config.Save();
                }
                else
                {
                    Vector2 diff = (Vector2)Input.mousePosition - _resizeStartMouse;
                    _windowRect.width = Mathf.Max(400, _resizeStartSize.x + diff.x);
                    _windowRect.height = Mathf.Max(300, _resizeStartSize.y - diff.y);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void VFPreload_InvokeOnLoadWorkEnded_Postfix()
        {
            ApplyMultiplier();
        }

        public static void ApplyMultiplier()
        {
            if (LDB.items == null) return;

            // Capture original stack sizes if not already captured
            if (_originalStackSizes.Count == 0)
            {
                foreach (var item in LDB.items.dataArray)
                {
                    if (item != null)
                        _originalStackSizes[item.ID] = item.StackSize;
                }
                Log.Info($"Captured {_originalStackSizes.Count} original stack sizes.");
            }

            float gItem = GlobalItemMultiplier.Value;
            float gBuilding = GlobalBuildingMultiplier.Value;
            float gUseful = GlobalUsefulMultiplier.Value;
            int count = 0;
            
            foreach (var item in LDB.items.dataArray)
            {
                if (item == null) continue;
                if (!_originalStackSizes.TryGetValue(item.ID, out int baseStack)) continue;

                float defaultMulti;
                if (IsUsefulItem(item))
                {
                    defaultMulti = gUseful;
                }
                else
                {
                    defaultMulti = item.CanBuild ? gBuilding : gItem;
                }

                float multi = _itemOverrides.TryGetValue(item.ID, out float custom) ? custom : defaultMulti;
                
                int newValue = Mathf.RoundToInt(baseStack * multi);
                
                if (newValue < 1) newValue = 1;
                if (newValue > 1000000) newValue = 1000000;

                if (item.StackSize != newValue)
                {
                    item.StackSize = newValue;
                    count++;
                }

                // Update StorageComponent static cache (used by most game systems)
                if (StorageComponent.itemStackCount != null && item.ID < StorageComponent.itemStackCount.Length)
                {
                    StorageComponent.itemStackCount[item.ID] = newValue;
                }
            }
            Log.Info($"Applied multiplier to {count} items.");

            if (GameMain.mainPlayer != null && GameMain.mainPlayer.package != null)
            {
                var package = GameMain.mainPlayer.package;
                // Force existing items in inventory to recognize new stack limits
                for (int i = 0; i < package.size; i++)
                {
                    int itemId = package.grids[i].itemId;
                    if (itemId > 0)
                    {
                        ItemProto p = LDB.items.Select(itemId);
                        if (p != null)
                        {
                            package.grids[i].stackSize = p.StackSize;
                        }
                    }
                }
                package.NotifyStorageChange();
            }

            // Force UI refresh if in-game
            if (UIRoot.instance != null && UIRoot.instance.uiGame != null && UIRoot.instance.uiGame.inventoryWindow != null)
            {
                if (UIRoot.instance.uiGame.inventoryWindow.active)
                {
                    UIRoot.instance.uiGame.inventoryWindow._OnUpdate();
                }
            }
        }

        private static bool IsUsefulItem(ItemProto item)
        {
            // 5001: Logistic Drone, 5002: Logistic Vessel
            // 5101: Attack Drone, 5102: Corvette, 5103: Destroyer
            // Also include precision drones if they have specific IDs
            if (item.ID == 5001 || item.ID == 5002) return true;
            if (item.ID >= 5101 && item.ID <= 5111) return true; // Combat units
            return false;
        }

        private void OnGUI()
        {
            if (!_showWindow) return;
            
            // Only process GUI for the main game camera to prevent flickering and layout crashes
            if (Camera.current != null && Camera.current.cameraType != CameraType.Game) return;

            // Hide UI only if game is truly paused (ESC menu) or in specialized modes where UI shouldn't exist
            if (GameMain.isPaused || UIGame.viewMode == EViewMode.MilkyWay) return;

            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            
            _windowRect = GUI.Window(9933, _windowRect, WindowFunc, "Stacksize Multiplier");
            
            if (Mathf.Abs(_windowRect.x - WindowX.Value) > 0.1f || Mathf.Abs(_windowRect.y - WindowY.Value) > 0.1f)
            {
                WindowX.Value = _windowRect.x;
                WindowY.Value = _windowRect.y;
            }
        }

        private List<ItemProto> _displayItems = new List<ItemProto>();
        private int _displayColumns = 1;
        private string _lastSearch = null;
        private float _lastWidth = 0f;

        private void WindowFunc(int id)
        {
            float itemWidth = 280f;
            
            // Re-calculate layout data ONLY during Layout event to ensure Repaint uses identical count
            if (Event.current.type == EventType.Layout)
            {
                if (_lastSearch != _searchText || Mathf.Abs(_lastWidth - _windowRect.width) > 5f || _displayItems.Count == 0)
                {
                    _lastSearch = _searchText;
                    _lastWidth = _windowRect.width;
                    
                    if (LDB.items != null && LDB.items.dataArray != null)
                    {
                        var allItems = LDB.items.dataArray.Where(i => i != null && i.ID > 0).ToList();
                        string searchLower = _searchText?.ToLower() ?? "";
                        if (!string.IsNullOrEmpty(searchLower))
                        {
                            _displayItems = allItems.Where(i => i.name.ToLower().Contains(searchLower)).ToList();
                        }
                        else
                        {
                            _displayItems = allItems;
                        }
                    }
                    _displayColumns = Mathf.Max(1, Mathf.FloorToInt((_windowRect.width - 40) / itemWidth));
                }
            }

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { richText = true };
            GUIStyle headerStyle = new GUIStyle(labelStyle) { fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1.0f);

            GUILayout.BeginVertical();
            
            // GLOBAL SECTION
            GUILayout.BeginHorizontal();
            GUILayout.Label("GLOBAL SETTINGS", headerStyle);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            if (GUILayout.Button("REFRESH ALL STACKS", GUILayout.Width(150), GUILayout.Height(22)))
            {
                ApplyMultiplier();
            }
            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            GUILayout.EndHorizontal();
            
            // Item Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Items Multiplier: {GlobalItemMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGItem = GUILayout.HorizontalSlider(GlobalItemMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGItem - GlobalItemMultiplier.Value) > 0.01f)
            {
                GlobalItemMultiplier.Value = Mathf.Round(newGItem * 2f) / 2f;
                ApplyMultiplier();
                Config.Save();
            }
            GUILayout.EndHorizontal();

            // Building Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Buildings Multiplier: {GlobalBuildingMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGBuilding = GUILayout.HorizontalSlider(GlobalBuildingMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGBuilding - GlobalBuildingMultiplier.Value) > 0.01f)
            {
                GlobalBuildingMultiplier.Value = Mathf.Round(newGBuilding * 2f) / 2f;
                ApplyMultiplier();
                Config.Save();
            }
            GUILayout.EndHorizontal();

            // Useful Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Useful Items Multiplier: {GlobalUsefulMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGUseful = GUILayout.HorizontalSlider(GlobalUsefulMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGUseful - GlobalUsefulMultiplier.Value) > 0.01f)
            {
                GlobalUsefulMultiplier.Value = Mathf.Round(newGUseful * 2f) / 2f;
                ApplyMultiplier();
                Config.Save();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            
            // SEARCH & LIST SECTION
            GUILayout.Label("INDIVIDUAL OVERRIDES", headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchText = GUILayout.TextField(_searchText);
            if (GUILayout.Button("Clear", GUILayout.Width(50))) _searchText = "";
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.box);
            
            // Use cached items and columns to prevent IMGUI control count mismatch
            for (int i = 0; i < _displayItems.Count; i += _displayColumns)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; i + c < _displayItems.Count && c < _displayColumns; c++)
                {
                    var item = _displayItems[i + c];
                    if (!_originalStackSizes.TryGetValue(item.ID, out int baseStack)) continue;

                    bool hasOverride = _itemOverrides.TryGetValue(item.ID, out float currentVal);
                    bool isUseful = IsUsefulItem(item);
                    float defaultMulti = isUseful ? GlobalUsefulMultiplier.Value : (item.CanBuild ? GlobalBuildingMultiplier.Value : GlobalItemMultiplier.Value);
                    float activeVal = hasOverride ? currentVal : defaultMulti;

                    GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(itemWidth));
                    
                    // Icon
                    if (item.iconSprite?.texture != null)
                    {
                        Rect iconRect = GUILayoutUtility.GetRect(32, 32);
                        GUI.DrawTexture(iconRect, item.iconSprite.texture);
                    }

                    GUILayout.BeginVertical();
                    string typeLabel = isUseful ? "<color=#ffff00>[U]</color>" : (item.CanBuild ? "<color=#00ffaa>[B]</color>" : "<color=#aaaaaa>[I]</color>");
                    GUILayout.Label($"<b>{item.name}</b> {typeLabel}", labelStyle, GUILayout.MaxWidth(itemWidth - 60));
                    GUILayout.Label($"<color=#aaaaaa>{baseStack} -> </color><color=#66b2ff><b>{item.StackSize}</b></color>", labelStyle);
                    
                    GUILayout.BeginHorizontal();
                    float sliderVal = GUILayout.HorizontalSlider(activeVal, 1f, 10f, GUILayout.Width(100));
                    float snapped = Mathf.Round(sliderVal * 2f) / 2f;
                    
                    if (Mathf.Abs(snapped - activeVal) > 0.01f)
                    {
                        _itemOverrides[item.ID] = snapped;
                        ApplyMultiplier();
                        SaveOverrides();
                    }
                    
                    GUILayout.Label($"{activeVal:F1}x", GUILayout.Width(40));
                    
                    if (hasOverride)
                    {
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _itemOverrides.Remove(item.ID);
                            ApplyMultiplier();
                            SaveOverrides();
                        }
                        GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
                    }
                    
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.1f, 0.8f);
            if (GUILayout.Button("CLOSE", GUILayout.Height(30))) _showWindow = false;

            GUILayout.EndVertical();

            // Resize Handle
            Rect resizeRect = new Rect(_windowRect.width - 20, _windowRect.height - 20, 20, 20);
            GUI.Box(resizeRect, "///");
            if (Event.current.type == EventType.MouseDown && resizeRect.Contains(Event.current.mousePosition))
            {
                _isResizing = true;
                _resizeStartMouse = Input.mousePosition;
                _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                Event.current.Use();
            }

            GUI.DragWindow();
        }
    }
}
