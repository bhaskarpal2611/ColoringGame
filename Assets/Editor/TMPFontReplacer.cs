using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class TMPFontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;
    private bool processScenes = true;
    private bool processPrefabs = true;

    [MenuItem("Tools/TMP Font Replacer")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontReplacer>("TMP Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Target Font Asset",
            targetFont,
            typeof(TMP_FontAsset),
            false);

        GUILayout.Space(10);

        processScenes = EditorGUILayout.Toggle("Update Scenes", processScenes);
        processPrefabs = EditorGUILayout.Toggle("Update Prefabs", processPrefabs);

        GUILayout.Space(20);

        GUI.enabled = targetFont != null;

        if (GUILayout.Button("Replace All TMP Fonts", GUILayout.Height(40)))
        {
            ReplaceFonts();
        }

        GUI.enabled = true;
    }

    private void ReplaceFonts()
    {
        if (targetFont == null)
        {
            Debug.LogError("Target font is null.");
            return;
        }

        int totalUpdated = 0;

        if (processScenes)
            totalUpdated += ReplaceFontsInScenes();

        if (processPrefabs)
            totalUpdated += ReplaceFontsInPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"TMP Font Replacement Complete. Updated {totalUpdated} text components.");
    }

    private int ReplaceFontsInScenes()
    {
        int updatedCount = 0;

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);

            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();

            foreach (TMP_Text text in texts)
            {
                if (!text.gameObject.scene.IsValid())
                    continue;

                Undo.RecordObject(text, "Replace TMP Font");

                text.font = targetFont;
                EditorUtility.SetDirty(text);

                updatedCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Updated Scene: {scenePath}");
        }

        return updatedCount;
    }

    private int ReplaceFontsInPrefabs()
    {
        int updatedCount = 0;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(path);

            bool modified = false;

            TMP_Text[] texts =
                prefabRoot.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text.font == targetFont)
                    continue;

                text.font = targetFont;
                modified = true;
                updatedCount++;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return updatedCount;
    }
}