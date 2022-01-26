using System;
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace CustomWarpSound
{
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency(CommonAPIPlugin.LDB_TOOL_GUID)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private bool _initAudio;
        private bool _playAudio;
        private long _lastCheckedAudio;
        private bool _loadedAudio;
        private GameObject _audioTestGo;
        private AudioSource _audioSrc;
        private string _savedAudioProto;
        public static ManualLogSource LOG;
        private const string keyword = "customwarpsound";

        private void Awake()
        {
            LOG = Logger;
            using (ProtoRegistry.StartModLoad(PluginInfo.PLUGIN_GUID))
            {
                var assetBundleFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var resources = new ResourceData(PluginInfo.PLUGIN_GUID, keyword, assetBundleFolder);
                resources.LoadAssetBundle(keyword);
                ProtoRegistry.AddResource(resources);
                
                var allAssetNames = resources.bundle.GetAllAssetNames();
                var assetNames = string.Join(", ", allAssetNames);
                Logger.LogInfo($"asset names: {assetNames}");
            }

            ProtoRegistry.EditAudio(112, $"assets/{keyword}/audio/startup.mp3", 1, 1, 0, 0); // startup
            _initAudio = true;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }


        private void Update()
        {
            if (DSPGame.IsMenuDemo || GameMain.isPaused)
            {
                return;
            }


            if (VFInput.alt && Input.GetKeyDown(KeyCode.T))
            {
                UIRealtimeTip.Popup($"playing sound 112", false);
                VFAudio.Create("warp-begin", GameMain.mainPlayer.transform, Vector3.zero, true);
            }
        }

        private void OnDestroy()
        {
            AssetLoader.UnloadBundle();

            if (_audioSrc != null && _audioSrc.gameObject != null)
            {
                Destroy(_audioSrc.gameObject);
                _audioSrc = null;
            }

            if (_audioTestGo != null && _audioTestGo.gameObject != null)
            {
                Destroy(_audioTestGo.gameObject);
                _audioTestGo = null;
            }

            try
            {
                var audioProto = LDB.audios.Select(112);
                var savedAudioProto = JsonUtility.FromJson<AudioProto>(_savedAudioProto);

                if (LDB.audios.dataIndices != null)
                {
                    UIRealtimeTip.PopupAhead($"Restoring warp sound from {audioProto.ClipPath} to {savedAudioProto.ClipPath}", false);
                    LDB.audios[LDB.audios.dataIndices[122]] = savedAudioProto;
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"failed to restore warp audio proto {e.Message}");
            }
        }
    }
}