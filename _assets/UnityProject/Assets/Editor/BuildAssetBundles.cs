using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class BuildStandalone : EditorWindow
    {
        [MenuItem("Window/DSP Tools/Build Assembly")]
        public static void BuildAssembly()
        {
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                options = BuildOptions.None,
                locationPathName = Path.Combine(Application.streamingAssetsPath, "..", "..", "Temp"),
                targetGroup = BuildTargetGroup.Standalone,
                scenes = new []{ "" },
                target = BuildTarget.StandaloneWindows64,
            
            
            });
        }

        [MenuItem("Window/DSP Tools/Build AssetBundles/Uncompressed")]
        public static void ExportResourceUncomp()
        {
            string folderName = "AssetBundles";
            string filePath = Path.Combine(Application.streamingAssetsPath, folderName);

            //Build for Windows platform
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows64);

            //Uncomment to build for other platforms
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.iOS);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.Android);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.WebGL);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

            //Refresh the Project folder
            AssetDatabase.Refresh();
        }

        [MenuItem("Window/DSP Tools/Build AssetBundles/Compressed")]
        public static void ExportResourceComp()
        {
            string folderName = "AssetBundles";
            string filePath = Path.Combine(Application.streamingAssetsPath, folderName);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            //Build for Windows platform
            BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            //Uncomment to build for other platforms
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.iOS);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.Android);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.WebGL);
            //BuildPipeline.BuildAssetBundles(filePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

            //Refresh the Project folder
            AssetDatabase.Refresh();
        }
    }
}