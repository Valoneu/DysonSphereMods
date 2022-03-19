using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using System.IO;
using System.Reflection;

namespace HydrogenDissolution
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInProcess("DSPGAME.exe")]

    public class HydrogenDissolution : BaseUnityPlugin
    {
        public static ResourceData resources;
        internal void Awake()
        {
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Logger.LogInfo($"Plugin: {PluginInfo.PLUGIN_GUID} {PluginInfo.PLUGIN_VERSION} is loaded!");

            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resources = new ResourceData(PluginInfo.PLUGIN_GUID, "HydrogenDissolution", pluginfolder);
            resources.LoadAssetBundle("assets");
            ProtoRegistry.AddResource(resources);
            ProtoRegistry.RegisterString("Hydrogen Dissolution", "Hydrogen Dissolution");

            // 1 Hydrogen <- 100 Hydrogen (1120 id of hydrogen), Grid index - Bottomline, 5th from right
            RecipeProto recipe = ProtoRegistry.RegisterRecipe(650, ERecipeType.Chemical, 10, new[] { 1120 }, new[] { 100 }, new[] { 1120 }, new[] { 1 }, "Hydrogen Dissolution", 1121,  1609 ,"Hydrogen Dissolution" , "assets/HydrogenDissolution/icons/icon");
        }
    }
}

    