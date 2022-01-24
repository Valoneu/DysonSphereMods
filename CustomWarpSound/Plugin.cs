using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
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
            ProtoRegistry.EditAudio( 112,  "StartUP.mp3", 1, 1, 0, 0 ); // startup
            ProtoRegistry.EditAudio( 113,  "Working.mp3", 1, 1, 0, 0 ); // running
            ProtoRegistry.EditAudio( 114,  "SlowDown.wav", 1, 1, 0, 0 ); // slowdown
        }
    }
}
