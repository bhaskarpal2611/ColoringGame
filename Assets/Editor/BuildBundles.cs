using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildBundles
{
    [MenuItem("Assets/Build AssetBundles/Build All")]
    static void BuildAllAssetBundles()
    {
        BuildBundlesForTarget(BuildTarget.Android, "android");
        BuildBundlesForTarget(BuildTarget.StandaloneWindows64, "windows");
    }

    [MenuItem("Assets/Build AssetBundles/Build Android")]
    static void BuildAndroidAssetBundles()
    {
        BuildBundlesForTarget(BuildTarget.Android, "android");
    }

    [MenuItem("Assets/Build AssetBundles/Build Windows 64")]
    static void BuildWindowsAssetBundles()
    {
        BuildBundlesForTarget(BuildTarget.StandaloneWindows64, "windows");
    }

    static void BuildBundlesForTarget(BuildTarget buildTarget, string subFolder)
    {
        // Use "Assets/AssetBundles" instead of StreamingAssets
        string assetBundleDirectory = Path.Combine("Assets/AssetBundles", subFolder);
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        // Build the bundles
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, 
                                        BuildAssetBundleOptions.ChunkBasedCompression, 
                                        buildTarget);
        
        Debug.Log($"<b>[BuildBundles]</b> Successfully built {buildTarget} bundles to: {assetBundleDirectory}");

        // Copy to StreamingAssets
        string streamingAssetsDirectory = Path.Combine(Application.streamingAssetsPath, subFolder);
        if (Directory.Exists(streamingAssetsDirectory))
        {
            Directory.Delete(streamingAssetsDirectory, true);
        }
        Directory.CreateDirectory(streamingAssetsDirectory);

        // Copy all files
        string[] files = Directory.GetFiles(assetBundleDirectory);
        foreach (string file in files)
        {
            if (file.EndsWith(".meta")) continue; // Optional: Skip meta files if not needed in StreamingAssets
            
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(streamingAssetsDirectory, fileName);
            File.Copy(file, destFile, true);
        }

        Debug.Log($"<b>[BuildBundles]</b> Copied bundles to StreamingAssets: {streamingAssetsDirectory}");
        
        // Refresh the AssetDatabase so the new files show up in the Editor
        AssetDatabase.Refresh();
    }
}
