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

        private StacksizeMultiplierWindow _window;
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

            LoadOverrides();
            RegisterKeyBinds();

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(StacksizeMultiplierPlugin));

            _window = new StacksizeMultiplierWindow(
                9933,
                "Stacksize Multiplier",
                new Rect(WindowX.Value, WindowY.Value, WindowW.Value, WindowH.Value),
                ApplyMultiplier,
                SaveOverrides,
                _originalStackSizes,
                _itemOverrides,
                IsUsefulItem
            );

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

        public static void SaveOverrides()
        {
            string raw = string.Join(",", _itemOverrides.Select(kvp => $"{kvp.Key}:{kvp.Value:F1}"));
            ItemOverridesRaw.Value = raw;
            if (GlobalItemMultiplier != null && GlobalItemMultiplier.ConfigFile != null)
                GlobalItemMultiplier.ConfigFile.Save();
        }

        private bool _lastKeyState = false;
        private void Update()
        {
            bool currentKey = CustomKeyBindSystem.GetKeyBind("ToggleStacksizeUI").keyValue;
            if (currentKey && !_lastKeyState)
            {
                Log.Info("ToggleStacksizeUI keybind pressed!");
                _window.Toggle();
            }
            _lastKeyState = currentKey;
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

                // Update StorageComponent static cache
                if (StorageComponent.itemStackCount != null && item.ID < StorageComponent.itemStackCount.Length)
                {
                    StorageComponent.itemStackCount[item.ID] = newValue;
                }
            }
            Log.Info($"Applied multiplier to {count} items.");

            if (GameMain.mainPlayer != null && GameMain.mainPlayer.package != null)
            {
                var package = GameMain.mainPlayer.package;
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

        public static bool IsUsefulItem(ItemProto item)
        {
            if (item.ID == 5001 || item.ID == 5002) return true;
            if (item.ID >= 5101 && item.ID <= 5111) return true;
            return false;
        }

        private void OnGUI()
        {
            _window.OnGUI();
        }
    }

    public class StacksizeMultiplierWindow : WindowBase
    {
        private string _searchText = "";
        private string _lastSearch = null;
        private List<ItemProto> _displayItems = new List<ItemProto>();
        private int _displayColumns = 1;
        private float _lastWidth = 0f;
        
        private readonly Action _applyMultiplier;
        private readonly Action _saveOverrides;
        private readonly Dictionary<int, int> _originalStackSizes;
        private readonly Dictionary<int, float> _itemOverrides;
        private readonly Func<ItemProto, bool> _isUsefulItem;

        public StacksizeMultiplierWindow(
            int windowId, 
            string title, 
            Rect defaultRect, 
            Action applyMultiplier, 
            Action saveOverrides,
            Dictionary<int, int> originalStackSizes,
            Dictionary<int, float> itemOverrides,
            Func<ItemProto, bool> isUsefulItem) 
            : base(windowId, title, defaultRect)
        {
            _applyMultiplier = applyMultiplier;
            _saveOverrides = saveOverrides;
            _originalStackSizes = originalStackSizes;
            _itemOverrides = itemOverrides;
            _isUsefulItem = isUsefulItem;
        }

        protected override void DrawWindowContent()
        {
            float itemWidth = 280f;
            
            // Re-calculate layout data
            if (Event.current.type == EventType.Layout)
            {
                if (_lastSearch != _searchText || Mathf.Abs(_lastWidth - WindowRect.width) > 5f || _displayItems.Count == 0)
                {
                    _lastSearch = _searchText;
                    _lastWidth = WindowRect.width;
                    
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
                    _displayColumns = Mathf.Max(1, Mathf.FloorToInt((WindowRect.width - 40) / itemWidth));
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
                _applyMultiplier?.Invoke();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            // Item Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Items Multiplier: {StacksizeMultiplierPlugin.GlobalItemMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGItem = GUILayout.HorizontalSlider(StacksizeMultiplierPlugin.GlobalItemMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGItem - StacksizeMultiplierPlugin.GlobalItemMultiplier.Value) > 0.01f)
            {
                StacksizeMultiplierPlugin.GlobalItemMultiplier.Value = Mathf.Round(newGItem * 2f) / 2f;
                _applyMultiplier?.Invoke();
                StacksizeMultiplierPlugin.GlobalItemMultiplier.ConfigFile.Save();
            }
            GUILayout.EndHorizontal();

            // Building Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Buildings Multiplier: {StacksizeMultiplierPlugin.GlobalBuildingMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGBuilding = GUILayout.HorizontalSlider(StacksizeMultiplierPlugin.GlobalBuildingMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGBuilding - StacksizeMultiplierPlugin.GlobalBuildingMultiplier.Value) > 0.01f)
            {
                StacksizeMultiplierPlugin.GlobalBuildingMultiplier.Value = Mathf.Round(newGBuilding * 2f) / 2f;
                _applyMultiplier?.Invoke();
                StacksizeMultiplierPlugin.GlobalBuildingMultiplier.ConfigFile.Save();
            }
            GUILayout.EndHorizontal();

            // Useful Global
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Useful Items Multiplier: {StacksizeMultiplierPlugin.GlobalUsefulMultiplier.Value:F1}x", GUILayout.Width(160));
            float newGUseful = GUILayout.HorizontalSlider(StacksizeMultiplierPlugin.GlobalUsefulMultiplier.Value, 1f, 10f);
            if (Mathf.Abs(newGUseful - StacksizeMultiplierPlugin.GlobalUsefulMultiplier.Value) > 0.01f)
            {
                StacksizeMultiplierPlugin.GlobalUsefulMultiplier.Value = Mathf.Round(newGUseful * 2f) / 2f;
                _applyMultiplier?.Invoke();
                StacksizeMultiplierPlugin.GlobalUsefulMultiplier.ConfigFile.Save();
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
            
            // Use cached items and columns
            for (int i = 0; i < _displayItems.Count; i += _displayColumns)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; i + c < _displayItems.Count && c < _displayColumns; c++)
                {
                    var item = _displayItems[i + c];
                    if (!_originalStackSizes.TryGetValue(item.ID, out int baseStack)) continue;

                    bool hasOverride = _itemOverrides.TryGetValue(item.ID, out float currentVal);
                    bool isUseful = _isUsefulItem(item);
                    float defaultMulti = isUseful ? StacksizeMultiplierPlugin.GlobalUsefulMultiplier.Value : (item.CanBuild ? StacksizeMultiplierPlugin.GlobalBuildingMultiplier.Value : StacksizeMultiplierPlugin.GlobalItemMultiplier.Value);
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
                        _applyMultiplier?.Invoke();
                        _saveOverrides?.Invoke();
                    }
                    
                    GUILayout.Label($"{activeVal:F1}x", GUILayout.Width(40));
                    
                    if (hasOverride)
                    {
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _itemOverrides.Remove(item.ID);
                            _applyMultiplier?.Invoke();
                            _saveOverrides?.Invoke();
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        protected override void DrawWindowFooter()
        {
            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.1f, 0.8f);
            if (GUILayout.Button("CLOSE", GUILayout.Height(30))) IsVisible = false;
            GUI.backgroundColor = Color.white;
        }

        public override void OnGUI()
        {
            if (!IsVisible) return;

            // Only process GUI for the main game camera
            if (Camera.current != null && Camera.current.cameraType != CameraType.Game) return;

            // Hide UI only if game is truly paused (ESC menu) or in specialized modes
            if (GameMain.isPaused || UIGame.viewMode == EViewMode.MilkyWay) return;

            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
            
            // Standard GUILayout.Window call via base.OnGUI()
            base.OnGUI();

            // Save window position
            if (Mathf.Abs(WindowRect.x - StacksizeMultiplierPlugin.WindowX.Value) > 0.1f || Mathf.Abs(WindowRect.y - StacksizeMultiplierPlugin.WindowY.Value) > 0.1f)
            {
                StacksizeMultiplierPlugin.WindowX.Value = WindowRect.x;
                StacksizeMultiplierPlugin.WindowY.Value = WindowRect.y;
                StacksizeMultiplierPlugin.WindowW.Value = WindowRect.width;
                StacksizeMultiplierPlugin.WindowH.Value = WindowRect.height;
                StacksizeMultiplierPlugin.WindowX.ConfigFile.Save();
            }
        }
    }
}
