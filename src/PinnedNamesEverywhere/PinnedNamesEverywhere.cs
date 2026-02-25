using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DysonSphereMods.Shared;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace PinnedNamesEverywhere
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class PinnedNamesEverywherePlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> AlwaysShowPinnedNames;
        public static ConfigEntry<bool> AlwaysShowPinnedDistances;
        public static ConfigEntry<float> PinnedNamesMinimumAlpha;

        private void Awake()
        {
            AlwaysShowPinnedNames = Config.Bind("General", "AlwaysShowPinnedNames", true, "If true, names of pinned stars/planets will always be visible, even when at the edge of the screen or far away.");
            AlwaysShowPinnedDistances = Config.Bind("General", "AlwaysShowPinnedDistances", false, "If true, distances of pinned stars/planets will also be always visible.");
            PinnedNamesMinimumAlpha = Config.Bind("General", "PinnedNamesMinimumAlpha", 0.8f, "Minimum alpha for pinned names and distances (0.0 to 1.0).");

            Log.Init(Logger);
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(PinnedNamesEverywherePlugin));

            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }

        // Cache pinned state to avoid repeated dictionary lookups in GameHistoryData every frame
        // This addresses the technical debt: UISpaceGuideEntry._OnLateUpdate runs for every marker every frame.
        private static readonly ConditionalWeakTable<UISpaceGuideEntry, PinnedStateInfo> _pinnedCache = new ConditionalWeakTable<UISpaceGuideEntry, PinnedStateInfo>();

        private class PinnedStateInfo
        {
            public bool IsPinned;
            public int LastCheckFrame;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISpaceGuideEntry), "_OnLateUpdate")]
        public static void UISpaceGuideEntry__OnLateUpdate_Postfix(UISpaceGuideEntry __instance)
        {
            if (!AlwaysShowPinnedNames.Value) return;

            int currentFrame = Time.frameCount;
            
            // Check pinned state every 60 frames (approx 1s) to reduce CPU load
            if (!_pinnedCache.TryGetValue(__instance, out var state) || currentFrame - state.LastCheckFrame > 60)
            {
                if (state == null)
                {
                    state = new PinnedStateInfo();
                    _pinnedCache.Add(__instance, state);
                }

                state.IsPinned = CheckIfPinned(__instance);
                state.LastCheckFrame = currentFrame;
            }

            if (state.IsPinned)
            {
                // Force name visibility
                if (!__instance.nameText.enabled)
                    __instance.nameText.enabled = true;

                // Force distance visibility based on config
                if (__instance.distText.enabled != AlwaysShowPinnedDistances.Value)
                    __instance.distText.enabled = AlwaysShowPinnedDistances.Value;

                // Force minimum alpha
                float minAlpha = PinnedNamesEverywherePlugin.PinnedNamesMinimumAlpha.Value;
                if (minAlpha > 0f)
                {
                    Color nameColor = __instance.nameText.color;
                    if (nameColor.a < minAlpha)
                    {
                        nameColor.a = minAlpha;
                        __instance.nameText.color = nameColor;
                    }

                    if (__instance.distText.enabled)
                    {
                        Color distColor = __instance.distText.color;
                        if (distColor.a < minAlpha)
                        {
                            distColor.a = minAlpha;
                            __instance.distText.color = distColor;
                        }
                    }
                }
            }
        }

        private static bool CheckIfPinned(UISpaceGuideEntry entry)
        {
            var history = entry.parent?.history;
            if (history == null) return false;

            switch (entry.guideType)
            {
                case ESpaceGuideType.Star:
                    return history.GetStarPin(entry.objId) == EPin.Show;
                case ESpaceGuideType.Planet:
                    return history.GetPlanetPin(entry.objId) == EPin.Show;
                case ESpaceGuideType.DFHive:
                    return history.GetHivePin(entry.objId - 1000000) == EPin.Show;
                case ESpaceGuideType.CosmicMessage:
                case ESpaceGuideType.DFCommunicator:
                    return history.GetMessagePin(entry.objId) == EPin.Show;
                default:
                    return false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UISpaceGuide), "ClipEntryPool")]
        public static void UISpaceGuide_ClipEntryPool_Prefix(UISpaceGuide __instance, ref int _guidecnt, bool all)
        {
            if (all || !AlwaysShowPinnedNames.Value) return;

            var history = GameMain.data?.history;
            if (history == null || __instance.galaxy == null) return;

            var relPos = __instance.relPos;
            var relRot = __instance.relRot;
            var setEntryMethod = typeof(UISpaceGuide).GetMethod("SetEntry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Collect existing objects
            System.Collections.Generic.HashSet<int> existingStars = new System.Collections.Generic.HashSet<int>();
            System.Collections.Generic.HashSet<int> existingPlanets = new System.Collections.Generic.HashSet<int>();

            for (int i = 0; i < _guidecnt; i++)
            {
                if (__instance.entryPool == null || i >= __instance.entryPool.Count) break;
                var entry = __instance.entryPool[i];
                if (entry.guideType == ESpaceGuideType.Star) existingStars.Add(entry.objId);
                else if (entry.guideType == ESpaceGuideType.Planet) existingPlanets.Add(entry.objId);
            }

            // Loop stars
            for (int index1 = 1; index1 <= __instance.galaxy.starCount; ++index1)
            {
                if (history.GetStarPin(index1) == EPin.Show && !existingStars.Contains(index1))
                {
                    StarData starData = __instance.galaxy.StarById(index1);
                    if (starData != null)
                    {
                        Vector3 _rpos = (Vector3)Maths.QInvRotateLF(relRot, starData.uPosition - relPos);

                        object[] args = new object[] { _guidecnt, ESpaceGuideType.Star, index1, 0, _rpos, 0.0f };
                        setEntryMethod?.Invoke(__instance, args);
                        _guidecnt = (int)args[0]; // Update ref
                    }
                }
            }
            
            // Loop planets
            for (int i = 0; i < __instance.galaxy.starCount; i++)
            {
                var star = __instance.galaxy.stars[i];
                if (star == null) continue;
                for (int j = 0; j < star.planetCount; j++)
                {
                    var planet = star.planets[j];
                    if (planet == null || planet.id == 0) continue;
                    if (history.GetPlanetPin(planet.id) == EPin.Show && !existingPlanets.Contains(planet.id))
                    {
                        Vector3 _rpos = (Vector3)Maths.QInvRotateLF(relRot, planet.uPosition - relPos);
                        object[] args = new object[] { _guidecnt, ESpaceGuideType.Planet, planet.id, 0, _rpos, (float)planet.realRadius };
                        setEntryMethod?.Invoke(__instance, args);
                        _guidecnt = (int)args[0];
                    }
                }
            }
        }
    }
}
