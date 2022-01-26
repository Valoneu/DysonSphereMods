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

        private void Awake()
        {
            LOG = Logger;
            AssetLoader.InitAssets("customwarpsound");
            var allAssetNames = AssetLoader.AssetBundle.GetAllAssetNames();
            var assetNames = string.Join(", ", allAssetNames);
            Logger.LogInfo($"asset names: {assetNames}");
            var audioProto = LDB.audios.Select(112);
            _savedAudioProto = JsonUtility.ToJson(audioProto, true);
            Logger.LogInfo($"audio proto before looks like: {_savedAudioProto}");

            ProtoRegistry.EditAudio(112, "assets/audio/startup.mp3", 1, 1, 0, 0); // startup
            _initAudio = true;
            Logger.LogInfo($"audio proto now looks like: {JsonUtility.ToJson(audioProto, true)}");

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (DSPGame.IsMenuDemo || GameMain.isPaused)
            {
                return;
            }

            if (!DSPGame.IsMenuDemo && !GameMain.isPaused && !_loadedAudio)
            {
                _loadedAudio = true;
                var testPrefab = AssetLoader.AssetBundle.LoadAsset<GameObject>("Assets/Prefab/AudioTest.prefab");
                var inGameGo = GameObject.Find("UI Root/Overlay Canvas/In Game");
                _audioTestGo = Instantiate(testPrefab, inGameGo.transform, false);
                _audioSrc = _audioTestGo.GetComponentInChildren<AudioSource>();
                _audioSrc.loop = true;
                Logger.LogInfo($"loaded audio source {_audioSrc}");
            }

            if (VFInput.alt && Input.GetKeyDown(KeyCode.P))
            {
                if (_audioSrc.isPlaying)
                {
                    UIRealtimeTip.Popup($"stopping audio source", false);
                    _audioSrc.Stop();
                }
                else
                {
                    UIRealtimeTip.Popup($"starting audio source", false);
                    _audioSrc.Play();
                }
            }

            if (VFInput.alt && Input.GetKeyDown(KeyCode.T))
            {
                UIRealtimeTip.Popup($"playing sound 112", false);
                VFAudio.Create("warp-begin", GameMain.mainPlayer.transform, Vector3.zero, true);
            }
            
            // U for "unload", as in put the original AudioProto back like it was
            // after this is done you'll have to reload (F6)
            if (VFInput.alt && Input.GetKeyDown(KeyCode.U))
            {
                var audioProto = LDB.audios.Select(112);
                var savedAudioProto = JsonUtility.FromJson<AudioProto>(_savedAudioProto);

                if (LDB.audios.dataIndices != null)
                {
                    UIRealtimeTip.PopupAhead($"Restoring warp sound from {audioProto.ClipPath} to {savedAudioProto.ClipPath}", false);
                    LDB.audios[LDB.audios.dataIndices[122]] = savedAudioProto;
                }

                Logger.LogInfo($"restored proto: {JsonUtility.ToJson(LDB.audios.Select(122), true)}");
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