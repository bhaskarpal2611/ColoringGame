using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ColorSwipeGame
{
    [CreateAssetMenu(fileName = "SO/Levels", menuName = "SO/Levels")]
    public class LevelDataSO : ScriptableObject
    {
        public Levels Levels;

        public GameObject GetLevelPrefab(int index)
        {
            if (index < 0 || index >= Levels.levelsData.Length)
            {
                Debug.LogError("Index out of bound");
                return null;
            }
            return Levels.levelsData[index].LevelPrefab;
        }

        public SaveData GetSaveData(int index)
        {
            return Levels.levelsData[index].LastSaveData;
        }

        public void SaveLevelData(int index)
        {
            SaveData saveData = new SaveData(index);
            saveData.IsEdited = Levels.levelsData[index].IsEdited;
            if (saveData.IsEdited)
            {
                saveData.LevelImageName = Levels.levelsData[index].LevelImageName;
                saveData.TexturesData = Levels.levelsData[index].CurrentTextures;
            }
            Levels.levelsData[index].LastSaveData = saveData;
        }

        public void RestoreSaveData(SaveData saveData)
        {
            Levels.levelsData[saveData.LevelIndex].LevelIndex = saveData.LevelIndex;
            SetIsEdited(saveData.LevelIndex, saveData.IsEdited);
            if (IsEdited(saveData.LevelIndex))
            {
                SaveEditedImage(saveData.LevelIndex, saveData.LevelImageName);
                Levels.levelsData[saveData.LevelIndex].CurrentTextures = saveData.TexturesData;
            }
        }

        public bool IsEdited(int index) => Levels.levelsData[index].IsEdited;
        public bool SetIsEdited(int index, bool value) => Levels.levelsData[index].IsEdited = value;

        public void Reset()
        {
            for (int i = 0; i < Levels.levelsData.Length; i++)
            {
                Levels.levelsData[i].IsEdited = false;
            }
        }

        public void SaveEditedImage(int index, string fileName)
        {
            Levels.levelsData[index].LevelImageName = fileName;
        }

        public string GetEditedImage(int index)
        {
            return Levels.levelsData[index].LevelImageName;
        }

        public System.Collections.IEnumerator SaveLevelState(int index, Dictionary<int, Sprite> editedTextures)
        {
            Levels.levelsData[index].CurrentTextures.TexturesData.Clear();

            int count = 0;
            foreach (var kvp in editedTextures)
            {
                string textureFileName = "texture_" + index + "_" + kvp.Key;  // Unique file name
                SaveTextureToFile(kvp.Value, textureFileName);

                Levels.levelsData[index].CurrentTextures.TexturesData.Add(new TextureData(kvp.Key, textureFileName));
                
                count++;
                if (count % 3 == 0) // Yield every 3 textures to prevent freezing
                {
                    yield return null;
                    Resources.UnloadUnusedAssets();
                }
            }
        }

        public LevelTextures LoadTextures(int index)
        {
            LevelTextures levelTextures = new LevelTextures();
            levelTextures.EditedTextures = new();
            levelTextures.OriginalSprites = new();

            for (int i = 0; i < Levels.levelsData[index].OriginalSprites.Length; i++)
                levelTextures.OriginalSprites.Add(i, Levels.levelsData[index].OriginalSprites[i]);

            var textureDataList = Levels.levelsData[index].CurrentTextures.TexturesData;
            foreach (var textureData in textureDataList)
            {
                Texture2D texture = LoadTextureFromFile(textureData.textureFilePath);
                if (texture != null)
                {
                    levelTextures.EditedTextures[textureData.id] = texture;
                }
            }
            return levelTextures;
        }
        private void SaveTextureToFile(Sprite sprite, string fileName)
        {
            RenderTexture rt = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            Graphics.Blit(sprite.texture, rt);
            sprite.texture.ReadPixels(new Rect(0, 0, sprite.texture.width, sprite.texture.height), 0, 0);
            sprite.texture.Apply();

            byte[] textureBytes = sprite.texture.EncodeToPNG();  // Or use EncodeToJPG for smaller files

            string filePath = Path.Combine(Application.persistentDataPath, "SavedTextures");

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
                Debug.Log($"Created folder: {filePath}");
            }
            else
            {
                Debug.Log($"Folder already exists: {filePath}");
            }

            string folderName = fileName + ".png";
            string fullPath = Path.Combine(filePath, folderName);
                
            File.WriteAllBytes(fullPath, textureBytes);
        }

        private Texture2D LoadTextureFromFile(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, "SavedTextures/" + fileName + ".png");

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
                texture.LoadImage(fileData);  // LoadImage auto-resizes the texture dimensions
                return texture;
            }

            Debug.Log("Texture file not found: " + filePath);
            return null;
        }
    }

    // LOAD
    [System.Serializable]
    public class LevelTextures
    {
        public Dictionary<int, Sprite> OriginalSprites;
        public Dictionary<int, Texture2D> EditedTextures;

        public LevelTextures()
        {
            OriginalSprites = new();
            EditedTextures = new();
        }
    }

    [System.Serializable]
    public struct Levels
    {
        public LevelData[] levelsData;
        public readonly int Length() => levelsData.Length;
    }

    [System.Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public GameObject LevelPrefab;
        public bool IsLevelCompleted = false;
        public bool IsEdited = false;
        public Sprite[] OriginalSprites;
        public AllTexturesData CurrentTextures;
        public string LevelImageName;
        public SaveData LastSaveData;
    }

    [System.Serializable]
    public class AllTexturesData
    {
        public List<TextureData> TexturesData;
    }

    [System.Serializable]
    public class TextureData
    {
        public int id;
        public string textureFilePath;  // Store the file path instead of the texture itself
        public TextureData(int id, string textureFilePath)
        {
            this.id = id;
            this.textureFilePath = textureFilePath;
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public int LevelIndex;
        public bool IsEdited;
        public string LevelImageName;
        public AllTexturesData TexturesData;

        public SaveData(int index)
        {
            LevelIndex = index;
            IsEdited = false;
            LevelImageName = null;
            TexturesData = null;
        }
    }
}