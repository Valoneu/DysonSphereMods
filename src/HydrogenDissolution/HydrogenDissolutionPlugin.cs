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
    [BepInPlugin("com.Valoneu.HydrogenDissolution", "HydrogenDissolution", "1.0.1")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInProcess("DSPGAME.exe")]
    public class HydrogenDissolutionPlugin : BaseUnityPlugin
    {
        public static ResourceData resources;

        private void Awake()
        {
            Logger.LogInfo("HydrogenDissolutionPlugin is loading...");

            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resources = new ResourceData("com.Valoneu.HydrogenDissolution", "HydrogenDissolution", pluginFolder);
            resources.LoadAssetBundle("assets");
            ProtoRegistry.AddResource(resources);

#pragma warning disable CS0618
            ProtoRegistry.RegisterString("Hydrogen Dissolution", "Hydrogen Dissolution");
#pragma warning restore CS0618

            // 1 Hydrogen <- 100 Hydrogen (1120 id of hydrogen), Grid index - Bottomline, 5th from right
            ProtoRegistry.RegisterRecipe(650, ERecipeType.Chemical, 10, new[] { 1120 }, new[] { 100 }, new[] { 1120 }, new[] { 1 }, "Hydrogen Dissolution", 1121, 1609, "Hydrogen Dissolution", "assets/HydrogenDissolution/icons/icon");

            Logger.LogInfo("HydrogenDissolutionPlugin loaded successfully!");
        }
    }
}

    