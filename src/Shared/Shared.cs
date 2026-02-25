using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DysonSphereMods.Shared
{
    public static class Log
    {
        private static ManualLogSource _logger;

        public static void Init(ManualLogSource logger) 
        {
            _logger = logger;
        }

        public static void Debug(object data) => _logger?.LogDebug(data);
        public static void Info(object data) => _logger?.LogInfo(data);
        public static void Warning(object data) => _logger?.LogWarning(data);
        public static void Error(object data) => _logger?.LogError(data);
        public static void Fatal(object data) => _logger?.LogFatal(data);
        public static void Message(object data) => _logger?.LogMessage(data);

        public static void LogOnce(string msg, ref bool flag, params object[] args)
        {
            if (flag)
                return;
            flag = true;
            try
            {
                 var argVals = args == null ? Array.Empty<string>() : args.Select(arg => arg == null ? "null" : (arg is int || arg is string || arg.GetType().IsPrimitive ? arg.ToString() : JsonUtility.ToJson(arg))).ToArray();
                 Info(string.Format(msg, argVals));     
            }
            catch (Exception ex)
            {
                Warning($"LogOnce failed to format message: {msg}. Exception: {ex}");
            }
        }
    }

    public static class MultiplierService
    {
        private static readonly Dictionary<string, float> _multipliers = new Dictionary<string, float>();       
        private static bool _isDirty;

        public static event Action OnMultipliersChanged;

        public static void SetMultiplier(string key, float value)
        {
            if (!_multipliers.TryGetValue(key, out float current) || Math.Abs(current - value) > 0.0001f)       
            {
                _multipliers[key] = value;
                _isDirty = true;
            }
        }

        public static float GetMultiplier(string key, float defaultValue = 1f)
        {
            return _multipliers.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public static void CommitChanges()
        {
            if (_isDirty)
            {
                _isDirty = false;
                OnMultipliersChanged?.Invoke();
            }
        }
    }

    public static class TickManager
    {
        public static event Action OnSlowTick; // Every 60 ticks (~1s)
        public static event Action OnLazyTick; // Every 600 ticks (~10s)

        private static long _lastSlowTick = -1;
        private static long _lastLazyTick = -1;
        private static bool _patched = false;

        public static void Patch(Harmony harmony)       
        {
            if (_patched) return;
            _patched = true;
            harmony.PatchAll(typeof(TickManager));      
        }

        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        [HarmonyPostfix]
        public static void Init()
        {
            _lastSlowTick = -1;
            _lastLazyTick = -1;
        }

        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.LogicFrame))]
        [HarmonyPostfix]
        public static void GameTick()
        {
            long time = GameMain.gameTick;
            if (time / 60 > _lastSlowTick)
            {
                _lastSlowTick = time / 60;
                OnSlowTick?.Invoke();
            }

            if (time / 600 > _lastLazyTick)
            {
                _lastLazyTick = time / 600;
                OnLazyTick?.Invoke();
            }
        }
    }

    public abstract class WindowBase
    {
        public int WindowId { get; protected set; }     
        public string Title { get; set; }
        public Rect WindowRect;
        public bool IsVisible { get; set; }

        protected Vector2 ScrollPos;

        protected WindowBase(int windowId, string title, Rect defaultRect)
        {
            WindowId = windowId;
            Title = title;
            WindowRect = defaultRect;
        }

        public virtual void OnGUI()
        {
            if (!IsVisible) return;

            WindowRect = GUILayout.Window(WindowId, WindowRect, DrawWindowInternal, Title);

            // Basic screen clamping
            WindowRect.x = Mathf.Clamp(WindowRect.x, -WindowRect.width + 50, Screen.width - 50);
            WindowRect.y = Mathf.Clamp(WindowRect.y, -20, Screen.height - 50);
        }

        private bool _isResizing;
        private Rect _resizeRect = new Rect(0, 0, 15, 15);
        public Vector2 MinSize = new Vector2(300, 200);

        private void DrawWindowInternal(int id)
        {
            DrawWindowHeader();
            ScrollPos = GUILayout.BeginScrollView(ScrollPos);
            DrawWindowContent();
            GUILayout.EndScrollView();
            DrawWindowFooter();

            _resizeRect.x = WindowRect.width - 20;
            _resizeRect.y = WindowRect.height - 20;
            _resizeRect.width = 20;
            _resizeRect.height = 20;

            GUI.Label(_resizeRect, "↘", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, normal = new GUIStyleState() { textColor = new Color(0.6f, 0.6f, 0.6f, 0.8f) } });

            Event e = Event.current;
            bool clickedResize = false;
            if (e.type == EventType.MouseDown && _resizeRect.Contains(e.mousePosition))
            {
                _isResizing = true;
                clickedResize = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                _isResizing = false;
            }
            else if (e.type == EventType.MouseDrag && _isResizing)
            {
                WindowRect.width += e.delta.x;
                WindowRect.height += e.delta.y;
                WindowRect.width = Mathf.Max(MinSize.x, WindowRect.width);
                WindowRect.height = Mathf.Max(MinSize.y, WindowRect.height);
                e.Use();
            }

            if (e.type == EventType.MouseDown && !clickedResize)
            {
                GUIUtility.keyboardControl = 0; // Clear WASD eating focus safely
            }

            GUI.DragWindow();
        }

        protected virtual void DrawWindowHeader() { }   
        protected abstract void DrawWindowContent();    
        protected virtual void DrawWindowFooter() { }   

        public virtual void Toggle()
        {
            IsVisible = !IsVisible;
        }
    }
}
