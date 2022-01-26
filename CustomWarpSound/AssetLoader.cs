using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CommonAPI.Systems;
using UnityEngine;
using static CustomWarpSound.Plugin;

namespace CustomWarpSound
{
    public static class AssetLoader
    {
        public static AssetBundle AssetBundle { get; private set; }

        public static void InitAssets(string key)
        {
            string assetBundleFolder;
            try
            {
                assetBundleFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            catch (Exception e)
            {
                LOG.LogWarning($"Got exception figuring out plugin folder {e.Message}. Trying stupid things now");
                try
                {
                    assetBundleFolder = FindAssetBundleLocationTheHardWay(key);
                }
                catch (Exception e2)
                {
                    LOG.LogWarning($"stupid stuff failed too {e2.Message} {e2.StackTrace}");
                    return;
                }

                if (assetBundleFolder == null)
                {
                    LOG.LogWarning("no dice stupid");
                    return;
                }

                LOG.LogInfo($"found folder the hard way {assetBundleFolder}");
            }

            LOG.LogWarning($"plugin folder is {assetBundleFolder}");
            var resources = new ResourceData(PluginInfo.PLUGIN_GUID, key, assetBundleFolder);
            resources.LoadAssetBundle("customwarpsound");
            ProtoRegistry.AddResource(resources);
            AssetBundle = resources.bundle;
        }

        public static void UnloadBundle()
        {
            try
            {
                AssetBundle.Unload(true);
            }
            catch (Exception e)
            {
                LOG.LogWarning($"failed to unload bundle {e.Message}\r\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// This is just some stupid stuff to be allow us to find where the asset bundle is even when loaded by scriptengine
        /// </summary>
        private static string FindAssetBundleLocationTheHardWay(string key)
        {
            var matchingAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .ToList()
                .FindAll(ass => !string.IsNullOrEmpty(ass.FullName) && ass.FullName.ToLower().Contains("BepInEx".ToLower()))
                .ToList();
            if (matchingAssemblies.Count == 0)
            {
                LOG.LogWarning($"No bepinex found");
                return null;
            }

            LOG.LogInfo($"found a few to try {matchingAssemblies.Count} {matchingAssemblies[0].Location}");
            // first we look in plugins (not scripts where the plugin actually is) to see if bundle is there
            foreach (var bepinAsm in matchingAssemblies)
            {
                var bepDirName = Path.GetDirectoryName(bepinAsm.Location);

                if (bepDirName == null)
                    continue;
                string pluginFolderPath = null;
                if (bepDirName.ToLower().Contains("core"))
                {
                    pluginFolderPath = Path.Combine(Path.GetDirectoryName(bepDirName) ?? string.Empty, "plugins");
                }

                if (pluginFolderPath == null)
                {
                    LOG.LogWarning($"did not find plugin folder path from {bepDirName}");
                    continue;
                }

                if (File.Exists(Path.Combine(pluginFolderPath, key)))
                {
                    LOG.LogInfo($"found bundle file at {pluginFolderPath}");
                    return pluginFolderPath;
                }

                // well, now lets see if there is a sibling dir "scripts"
                var pluginParentDir = Path.GetDirectoryName(pluginFolderPath);
                if (pluginParentDir == null)
                    continue;
                var scriptsDir = Path.Combine(pluginParentDir, "scripts");
                if (Directory.Exists(scriptsDir))
                {
                    var bundleFullPath = Path.Combine(scriptsDir, key);
                    if (File.Exists(bundleFullPath))
                    {
                        LOG.LogInfo($"found bundle file at {scriptsDir}");
                        return scriptsDir;
                    }

                    LOG.LogInfo($"bundle file was not at {scriptsDir}");
                }
            }

            return null;
        }
    }
}