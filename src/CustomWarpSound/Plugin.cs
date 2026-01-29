using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using System.IO;
using System.Reflection;

namespace CustomWarpSound
{
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [BepInPlugin("com.Valoneu.CustomWarpSound", "CustomWarpSound", "1.0.10")]
    public class Plugin : BaseUnityPlugin
    {
        private const string keyword = "customwarpsound";

        private void Awake()
        {
            using (ProtoRegistry.StartModLoad("com.Valoneu.CustomWarpSound"))
            {
                var assetBundleFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var resources = new ResourceData("com.Valoneu.CustomWarpSound", keyword, assetBundleFolder);
                resources.LoadAssetBundle(keyword);
                ProtoRegistry.AddResource(resources);
                ProtoRegistry.EditAudio(112, $"assets/{keyword}/audio/startup", 1, 1, 0, 0);
                ProtoRegistry.EditAudio(113, $"assets/{keyword}/audio/working", 1, 1, 0, 0);
                ProtoRegistry.EditAudio(114, $"assets/{keyword}/audio/slowdown", 1, 1, 0, 0);
            }
            Logger.LogInfo($"Plugin com.Valoneu.CustomWarpSound is loaded!");
        }
    }
}