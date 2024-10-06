using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ColorSwipeGame
{
    [CreateAssetMenu(fileName = "SO/DrawnData", menuName = "SO/DrawnDatas")]
    public class DrawnDataSO : ScriptableObject
    {
        public Sprite OriginalEmptySprite;
        public List<DrawnData> Levels = new();

        // get default empty index - 0 empty box

        // save and load data functions

        public DrawnData GetSaveData(int index)
        {
            return Levels[index];
        }

        public void RestoreDrawnData(DrawnData drawnData)
        {
            Levels[drawnData.IndexNumber] = drawnData;
        }

        public void SaveImageForIcon(int index, string fileName)
        {
            if (Levels.Count == index)
            {
                Levels.Add(new DrawnData(index));
                Levels[index].IndexNumber = index;
            }
            Levels[index].SnapshotFileName = fileName;
        }

        public string GetImageIconFileName(int index) => Levels[index].SnapshotFileName;

        public void SaveDrawnTexture(int index, Sprite editedTexture)
        {
            if (Levels.Count == index)
            {
                Levels.Add(Levels[index]);
            }
            string texFileName = "Texture_00" + index;
            SaveTextureToFile(editedTexture.texture, texFileName);
            Levels[index].DrawnTextureFileName = texFileName;
        }

        public DrawnTexture LoadDrawnTexture(int index)
        {
            DrawnTexture drawnTexture = new DrawnTexture();
            drawnTexture.OriginalSprite = OriginalEmptySprite;
            drawnTexture.CurrentTexture = LoadTextureFromFile(Levels[index].DrawnTextureFileName);
            return drawnTexture;
        }

        private void SaveTextureToFile(Texture2D texture, string fileName)
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
        }

        private Texture2D LoadTextureFromFile(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");

            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
                texture.LoadImage(fileData);  // LoadImage auto-resizes the texture dimensions
                return texture;
            }

            Debug.LogError("Texture file not found: " + filePath);
            return null;
        }
    }

    [System.Serializable]
    public class DrawnData
    {
        public int IndexNumber;
        public string DrawnTextureFileName;
        public string SnapshotFileName;

        public DrawnData()
        {
            IndexNumber = -1;
            DrawnTextureFileName = "";
            SnapshotFileName = "";
        }

        public DrawnData(int value)
        {
            IndexNumber = value;
            DrawnTextureFileName = null;
            SnapshotFileName = null;
        }
    }

    [System.Serializable]
    public struct DrawnTexture
    {
        public Sprite OriginalSprite;
        public Texture2D CurrentTexture;
    }
}
