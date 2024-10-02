using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "SO/Levels", menuName = "SO/Levels")]
public class LevelDataSO : ScriptableObject
{
    public Levels Levels;


    public GameObject GetLevelPrefab(int index)
    {
        if (index < 0 || index >= Levels.Length())
        {
            Debug.LogError("Index out of bound");
            return null;
        }
        return Levels.levelsData[index].LevelPrefab;
    }

    public bool IsEdited(int index) => Levels.levelsData[index].IsEdited;

    public void Reset()
    {
        for (int i = 0; i < Levels.levelsData.Length; i++)
        {
            Levels.levelsData[i].IsEdited = false;
        }
    }

    // public Dictionary<int, Texture2D> GetLevelState(int index) => Levels.levelsData[index].CurrentTextures;

    // public void SaveLevelState(int index, Dictionary<int, Texture2D> editedTextures)
    // {
    //     Levels.levelsData[index].CurrentTextures = new Dictionary<int, Texture2D>(editedTextures);
    //     Levels.levelsData[index].IsEdited = true;
    //     LoadTextures(index, editedTextures);
    // }

    public void SaveLevelState(int index, Dictionary<int, Texture2D> editedTextures)
    {
        Levels.levelsData[index].CurrentTextures.Clear();

        foreach (var kvp in editedTextures)
        {
            //mod texture first



            string textureFileName = "texture_" + index + "_" + kvp.Key;  // Unique file name
            SaveTextureToFile(kvp.Value, textureFileName);

            Levels.levelsData[index].CurrentTextures.Add(new TextureData { id = kvp.Key, textureFilePath = textureFileName });
        }

        Levels.levelsData[index].IsEdited = true;
    }

    public Dictionary<int, Texture2D> LoadTextures(int index)
    {
        Dictionary<int, Texture2D> dictionary = new();

        var textureDataList = Levels.levelsData[index].CurrentTextures;
        foreach (var textureData in textureDataList)
        {
            Texture2D texture = LoadTextureFromFile(textureData.textureFilePath);
            if (texture != null)
            {
                dictionary.Add(textureData.id, texture);
                // Do something with the loaded texture (e.g., apply it to a material)
            }
        }
        return dictionary;
    }
    public void SaveTextureToFile(Texture2D texture, string fileName)
    {
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(texture, rt);
        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture.Apply();

        byte[] textureBytes = texture.EncodeToPNG();  // Or use EncodeToJPG for smaller files
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");

        File.WriteAllBytes(filePath, textureBytes);

        Debug.Log("Texture saved to: " + filePath);
    }

    public Texture2D LoadTextureFromFile(string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");

        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
            texture.LoadImage(fileData);  // LoadImage auto-resizes the texture dimensions
            Debug.Log(texture.mipmapCount);
            return texture;
        }

        Debug.LogError("Texture file not found: " + filePath);
        return null;
    }
}

[System.Serializable]
public struct Levels
{
    public LevelData[] levelsData;
    public readonly int Length() => levelsData.Length;
}

[System.Serializable]
public class TextureData
{
    public int id;
    public string textureFilePath;  // Store the file path instead of the texture itself
}

[System.Serializable]
public class LevelData
{
    public int LevelIndex;
    public GameObject LevelPrefab;
    public bool IsLevelCompleted = false;

    public bool IsEdited = false;

    public List<TextureData> CurrentTextures;
    public Texture2D[] _textures;
}
