using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
using UnityEngine;
using CommonAPI;
using CommonAPI.Systems;
namespace HideWarnings
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class HideWarningsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.HideWarnings";
        public const string NAME = "HideWarnings";
        public const string VERSION = "1.0.0";
        public static ConfigEntry<bool> HideVeinDepleted;
        public static ConfigEntry<bool> HideNotEnoughResources;
        public static ConfigEntry<bool> HidePowerShutdown;
        public static ConfigEntry<bool> HidePowerInsufficient;
        public static ConfigEntry<bool> HideSorterBlocked;
        public static ConfigEntry<bool> HideBuildingDamaged;
        public static ConfigEntry<bool> HideDashboardAlerts;
        public static ConfigEntry<bool> HideTechCompletion;
        private static HideWarningsWindow _window;
        private void Awake()
        {
            Log.Init(Logger);
            HideVeinDepleted = Config.Bind("Warnings", "HideVeinDepleted", false, "Hide vein depleted / insufficient resource warnings.");
            HideNotEnoughResources = Config.Bind("Warnings", "HideNotEnoughResources", false, "Hide 'needs resources' warnings on unconstructed buildings.");
            HidePowerShutdown = Config.Bind("Warnings", "HidePowerShutdown", false, "Hide power shutdown warnings.");
            HidePowerInsufficient = Config.Bind("Warnings", "HidePowerInsufficient", false, "Hide power insufficient/low warnings.");
            HideSorterBlocked = Config.Bind("Warnings", "HideSorterBlocked", false, "Hide sorter/inserter blocked warnings.");
            HideBuildingDamaged = Config.Bind("Warnings", "HideBuildingDamaged", false, "Hide building damaged/repair warnings.");
            HideDashboardAlerts = Config.Bind("Warnings", "HideDashboardAlerts", false, "Hide dashboard/statistics plan alerts.");
            HideTechCompletion = Config.Bind("Warnings", "HideTechCompletion", false, "Hide tech research completion popup and banner.");
            RegisterKeyBinds();
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(WarningPatches));
            harmony.PatchAll(typeof(HideWarningsPlugin));
            Log.Info($"{NAME} v{VERSION} loaded!");
        }
        private void RegisterKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleHideWarningsUI"))
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1250,
                    key = new CombineKey((int)KeyCode.Keypad5, 2, ECombineKeyAction.OnceClick, false), 
                    conflictGroup = 2052,
                    name = "ToggleHideWarningsUI",
                    canOverride = true
                });
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("ToggleHideWarningsUI", "Toggle Hide Warnings UI");
#pragma warning restore CS0618
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Postfix()
        {
            if (_window == null)
                _window = new HideWarningsWindow();
        }
        private void Update()
        {
            if (_window != null && CustomKeyBindSystem.GetKeyBind("ToggleHideWarningsUI").keyValue)
                _window.Toggle();
        }
        private void OnGUI()
        {
            if (_window != null) _window.OnGUI();
        }
        public static bool ShouldHideSignal(int signalId)
        {
            switch (signalId)
            {
                case 501: 
                case 502: 
                    return HidePowerInsufficient.Value;
                case 503: 
                    return HidePowerShutdown.Value;
                case 510:
                    return HideSorterBlocked.Value;
                case 512:
                    return HideBuildingDamaged.Value;
                case 513:
                    return HideBuildingDamaged.Value;
                case 405:
                    return HideNotEnoughResources.Value;
                case 518:
                    return HideDashboardAlerts.Value;
                default:
                    if (signalId >= 504 && signalId <= 509)
                        return HideVeinDepleted.Value;
                    return false;
            }
        }
    }
    [HarmonyPatch]
    public static class WarningPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.WarningLogic))]
        public static void WarningLogic_Postfix(WarningSystem __instance)
        {
            if (__instance.warningSignalCount <= 0) return;
            int writeIdx = 0;
            for (int readIdx = 0; readIdx < __instance.warningSignalCount; readIdx++)
            {
                int signalId = __instance.warningSignals[readIdx];
                if (HideWarningsPlugin.ShouldHideSignal(signalId))
                {
                    __instance.warningCounts[signalId] = 0;
                }
                else
                {
                    __instance.warningSignals[writeIdx] = signalId;
                    writeIdx++;
                }
            }
            for (int i = writeIdx; i < __instance.warningSignalCount; i++)
                __instance.warningSignals[i] = 0;
            __instance.warningSignalCount = writeIdx;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGeneralTips), "OnTechUnlocked")]
        public static bool OnTechUnlocked_Prefix()
        {
            return !HideWarningsPlugin.HideTechCompletion.Value;
        }
    }
    public class HideWarningsWindow : WindowBase
    {
        public HideWarningsWindow()
            : base(9930, "Hide Warnings", new Rect(Screen.width / 2f - 180, Screen.height / 2f - 200, 360, 400))
        {
            MinSize = new Vector2(300, 300);
        }
        protected override void DrawWindowHeader()
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1.0f);
            GUILayout.Label("WARNING FILTERS", headerStyle);
            GUILayout.Label("<size=11><color=#aaaaaa>Toggle which warnings to suppress</color></size>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(10);
        }
        protected override void DrawWindowContent()
        {
            DrawToggle("Vein Depleted / Insufficient", HideWarningsPlugin.HideVeinDepleted,
                "Hides warnings for depleted or low veins");
            DrawToggle("Needs Resources (Unconstructed)", HideWarningsPlugin.HideNotEnoughResources,
                "Hides 'needs resources' on unbuilt buildings");
            DrawToggle("Power Insufficient / Low", HideWarningsPlugin.HidePowerInsufficient,
                "Hides insufficient power warnings");
            DrawToggle("Power Shutdown", HideWarningsPlugin.HidePowerShutdown,
                "Hides full power shutdown warnings");
            DrawToggle("Sorter / Inserter Blocked", HideWarningsPlugin.HideSorterBlocked,
                "Hides sorter/inserter jam warnings");
            DrawToggle("Building Damaged / Repair", HideWarningsPlugin.HideBuildingDamaged,
                "Hides building damage and repair warnings");
            DrawToggle("Dashboard / Stat Alerts", HideWarningsPlugin.HideDashboardAlerts,
                "Hides dashboard and statistics plan alerts");
            DrawToggle("Tech Completion Popup", HideWarningsPlugin.HideTechCompletion,
                "Hides the research completion banner and popup");
        }
        private void DrawToggle(string label, ConfigEntry<bool> config, string desc)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            config.Value = GUILayout.Toggle(config.Value, $"  {label}");
            GUILayout.Label($"<size=10><color=#888888>{desc}</color></size>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndVertical();
            GUILayout.Space(2);
        }
        protected override void DrawWindowFooter()
        {
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.1f, 0.8f);
            if (GUILayout.Button("CLOSE")) IsVisible = false;
            GUI.backgroundColor = Color.white;
        }
    }
}
