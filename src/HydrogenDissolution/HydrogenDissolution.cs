using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using DysonSphereMods.Shared;
using System.IO;
using System.Reflection;

namespace HydrogenDissolution
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(LocalizationModule))]
    [BepInProcess("DSPGAME.exe")]
    public class HydrogenDissolutionPlugin : BaseUnityPlugin
    {
        public static ResourceData resources;

        private void Awake()
        {
            Log.Init(Logger);
            Log.Info($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} is loading...");

            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resources = new ResourceData(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, pluginFolder);
            resources.LoadAssetBundle("assets");
            ProtoRegistry.AddResource(resources);

            RecipeDefinitions.Register();

            Log.Info($"{MyPluginInfo.PLUGIN_NAME} loaded successfully!");
        }
    }

    public static class RecipeDefinitions
    {
        public static void Register()
        {
            RegisterStrings();

            // 1 Hydrogen <- 100 Hydrogen (1120 id of hydrogen)
            ProtoRegistry.RegisterRecipe(
                650,
                ERecipeType.Chemical,
                10,
                new[] { 1120 },
                new[] { 100 },
                new[] { 1120 },
                new[] { 1 },
                "Hydrogen Dissolution",
                1121,
                1609,
                "Hydrogen Dissolution",
                "assets/HydrogenDissolution/icons/icon"
            );
        }

        private static void RegisterStrings()
        {
            LocalizationModule.RegisterTranslation("Hydrogen Dissolution", "Hydrogen Dissolution");
        }
    }
}
