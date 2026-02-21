using System;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using DysonSphereMods.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace PinnedNamesEverywhere
{
    [BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class PinnedNamesEverywherePlugin : BaseUnityPlugin
    {
        public const string MOD_GUID = "com.Valoneu.PinnedNamesEverywhere";
        public const string MOD_NAME = "PinnedNamesEverywhere";
        public const string MOD_VERSION = "1.0.0";

        public static ConfigEntry<bool> AlwaysShowPinnedNames;
        public static ConfigEntry<bool> AlwaysShowPinnedDistances;
        public static ConfigEntry<float> PinnedNamesMinimumAlpha;

        private void Awake()
        {
            AlwaysShowPinnedNames = Config.Bind("General", "AlwaysShowPinnedNames", true, "If true, names of pinned stars/planets will always be visible, even when at the edge of the screen or far away.");
            AlwaysShowPinnedDistances = Config.Bind("General", "AlwaysShowPinnedDistances", false, "If true, distances of pinned stars/planets will also be always visible.");
            PinnedNamesMinimumAlpha = Config.Bind("General", "PinnedNamesMinimumAlpha", 0.8f, "Minimum alpha for pinned names and distances (0.0 to 1.0).");

            Log.Init(Logger);
            var harmony = new Harmony(MOD_GUID);
            harmony.PatchAll(typeof(PinnedNamesEverywherePlugin));

            Log.Info($"{MOD_NAME} v{MOD_VERSION} loaded!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISpaceGuide), "_OnLateUpdate")]
        public static void UISpaceGuide__OnLateUpdate_Postfix(UISpaceGuide __instance)
        {
            if (!AlwaysShowPinnedNames.Value) return;

            GameData gameData = __instance.gameData;
            if (gameData == null || gameData.history == null) return;

            var traverse = Traverse.Create(__instance);
            VectorLF3 relPos = traverse.Field("relPos").GetValue<VectorLF3>();
            Quaternion relRot = traverse.Field("relRot").GetValue<Quaternion>();
            int astroId1 = gameData.localStar == null ? 0 : gameData.localStar.astroId;
            VectorLF3 camUPos = relPos + Maths.QRotateLF(relRot, (VectorLF3)__instance.gameCamera.transform.position);

            var entries = __instance.entryPool;
            int count = traverse.Field("entryOpenedCount").GetValue<int>();
            UISpaceGuideEntry entryPrefab = traverse.Field("entryPrefab").GetValue<UISpaceGuideEntry>();

            for (int i = 1; i <= __instance.galaxy.starCount; i++)
            {
                StarData star = __instance.galaxy.StarById(i);
                if (star == null) continue;

                if (gameData.history.GetStarPin(i) == EPin.Show)
                {
                    bool alreadyExists = false;
                    for (int j = 0; j < count; j++)
                    {
                        if (entries[j].guideType == ESpaceGuideType.Star && entries[j].objId == i)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        Vector3 rpos = (Vector3)Maths.QInvRotateLF(relRot, star.uPosition - relPos);

                        // Check visibility (occlusion by planets/stars)
                        bool visible = traverse.Method("CheckVisible", new object[] { astroId1, i * 100, star.uPosition, camUPos }).GetValue<bool>();
                        if (visible)
                        {
                            if (entries.Count <= count)
                            {
                                UISpaceGuideEntry newEntry = UnityEngine.Object.Instantiate<UISpaceGuideEntry>(entryPrefab, entryPrefab.transform.parent);
                                newEntry._Create();
                                newEntry._Init(__instance.data);
                                entries.Add(newEntry);
                            }
                            UISpaceGuideEntry entry = entries[count];
                            entry._Open();
                            entry.Set(ESpaceGuideType.Star, i, 0, rpos, star.viewRadius - 120f);
                            count++;
                        }
                    }
                }
            }
            traverse.Field("entryOpenedCount").SetValue(count);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISpaceGuideEntry), "_OnLateUpdate")]
        public static void UISpaceGuideEntry__OnLateUpdate_Postfix(UISpaceGuideEntry __instance)
        {
            if (!AlwaysShowPinnedNames.Value) return;

            bool isPinned = false;
            switch (__instance.guideType)
            {
                case ESpaceGuideType.Star:
                    isPinned = __instance.parent.history.GetStarPin(__instance.objId) == EPin.Show;
                    break;
                case ESpaceGuideType.Planet:
                    isPinned = __instance.parent.history.GetPlanetPin(__instance.objId) == EPin.Show;
                    break;
                case ESpaceGuideType.DFHive:
                    isPinned = __instance.parent.history.GetHivePin(__instance.objId - 1000000) == EPin.Show;
                    break;
                case ESpaceGuideType.CosmicMessage:
                case ESpaceGuideType.DFCommunicator:
                    isPinned = __instance.parent.history.GetMessagePin(__instance.objId) == EPin.Show;
                    break;
            }

            if (isPinned)
            {
                // Force name visibility
                __instance.nameText.enabled = true;

                // Force distance visibility based on config
                __instance.distText.enabled = AlwaysShowPinnedDistances.Value;

                // Force minimum alpha
                float minAlpha = PinnedNamesMinimumAlpha.Value;
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
    }
}
