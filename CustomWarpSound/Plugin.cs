using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using System.IO;
using System.Reflection;

namespace CustomWarpSound
{
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency(CommonAPIPlugin.LDB_TOOL_GUID)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string keyword = "customwarpsound";

        private void Awake()
        {
            using (ProtoRegistry.StartModLoad(PluginInfo.PLUGIN_GUID))
            {
                var assetBundleFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var resources = new ResourceData(PluginInfo.PLUGIN_GUID, keyword, assetBundleFolder);
                resources.LoadAssetBundle(keyword);
                ProtoRegistry.AddResource(resources);
                ProtoRegistry.EditAudio(112, $"assets/{keyword}/audio/startup", 1, 1, 0, 0);
                ProtoRegistry.EditAudio(113, $"assets/{keyword}/audio/working", 1, 1, 0, 0);
                ProtoRegistry.EditAudio(114, $"assets/{keyword}/audio/slowdown", 1, 1, 0, 0);
            }
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}