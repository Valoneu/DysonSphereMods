using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CustomWarpSound
{
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency(CommonAPIPlugin.LDB_TOOL_GUID)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var key = "customwarpsound";
            var resources = new ResourceData(PluginInfo.PLUGIN_GUID, key, pluginfolder);
            resources.LoadAssetBundle(key);
            ProtoRegistry.AddResource(resources);
            ProtoRegistry.EditAudio( 112,  "Assets/Audio/StartUP.mp3", 1, 1, 0, 0 ); // startup
            ProtoRegistry.EditAudio( 113,  "Assets/Audio/Working.wav", 1, 1, 0, 0 ); // running
            ProtoRegistry.EditAudio( 114,  "Assets/Audio/SlowDown.mp3", 1, 1, 0, 0 ); // slowdown
        }
    }
}
