using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;

public class ShaderInclusionFixer
{
    [MenuItem("Tools/Fix Pink Shaders")]
    public static void FixShaders()
    {
        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/2D/Sprite-Lit-Default",
            "Custom/PaintCircleTexture_URP6" 
        };

        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        SerializedObject serializedObject = new SerializedObject(graphicsSettings);
        SerializedProperty arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");

        bool changed = false;

        foreach (var shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Could not find shader '{shaderName}'. Make sure it exists in the project.");
                continue;
            }

            bool present = false;
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == shader)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                int index = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(index);
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
                element.objectReferenceValue = shader;
                changed = true;
                Debug.Log($"Added '{shaderName}' to Always Included Shaders.");
            }
            else
            {
                Debug.Log($"'{shaderName}' is already included.");
            }
        }

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("Graphics Settings updated successfully!");
        }
    }
}
