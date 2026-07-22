#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundlePushPull
{
    public static class PushManager
    {
        public static void Push(string serverRoot, string gameName, string abFolder, List<string> scripts)
        {
            if (string.IsNullOrEmpty(serverRoot) || !Directory.Exists(serverRoot))
            {
                EditorUtility.DisplayDialog("Push", "Invalid Server Root.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(gameName))
            {
                EditorUtility.DisplayDialog("Push", "Game Name is empty.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(abFolder) || !Directory.Exists(abFolder))
            {
                EditorUtility.DisplayDialog("Push", "Invalid AssetBundle Folder.", "OK");
                return;
            }

            string gameRoot = Path.Combine(serverRoot, gameName);
            string abDest = Path.Combine(gameRoot, "AssetBundles");
            string scriptDest = Path.Combine(gameRoot, "Scripts");

            int total = FileUtility.CountFiles(abFolder);
            foreach (string s in scripts)
                if (!string.IsNullOrEmpty(s)) total += FileUtility.CountFiles(s);

            FileUtility.Begin("Pushing...", total);
            try
            {
                FileUtility.CopyDirectory(abFolder, abDest);

                try { if (Directory.Exists(scriptDest)) Directory.Delete(scriptDest, true); } catch { }
                Directory.CreateDirectory(scriptDest);
                foreach (string s in scripts)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    if (Directory.Exists(s)) FileUtility.CopyContentsReplacing(s, scriptDest);
                    else FileUtility.CopyPath(s, scriptDest);
                }
            }
            finally { FileUtility.End(); }

            Report("Push");
        }

        public static void Report(string title)
        {
            if (FileUtility.Errors.Count == 0)
                UnityEngine.Debug.Log(title + " completed.");
            else
                EditorUtility.DisplayDialog(title,
                    title + " completed with " + FileUtility.Errors.Count + " error(s):\n\n" +
                    string.Join("\n", FileUtility.Errors.GetRange(0, Mathf.Min(10, FileUtility.Errors.Count))),
                    "OK");
        }
    }
}
#endif

