using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ColorSwipeGame
{
    public class SpriteSheetManager : MonoBehaviour
    {
        public Texture2D spritesheet;
        private Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();

        [System.Serializable]
        private class SpriteMetadata
        {
            public int id;
            public RectData rect;
            public Vector2Data pivot;
        }

        [System.Serializable]
        private class RectData
        {
            public float x, y, width, height;
        }

        [System.Serializable]
        private class Vector2Data
        {
            public float x, y;
        }

        [System.Serializable]
        private class SpritesheetMetadata
        {
            public List<SpriteMetadata> sprites;
        }

        public void InitializeSprites(Rect[] spriteRects)
        {
            for (int i = 0; i < spriteRects.Length; i++)
            {
                sprites[i] = Sprite.Create(spritesheet, spriteRects[i], new Vector2(0.5f, 0.5f));
            }
        }

        public async Task SaveSpritesheetToDisk(string fileName)
        {
            string directory = Application.persistentDataPath;
            string spritesheetPath = Path.Combine(directory, $"{fileName}.png");
            string metadataPath = Path.Combine(directory, $"{fileName}_metadata.json");

            // Save the spritesheet
            byte[] bytes = spritesheet.EncodeToPNG();
            await File.WriteAllBytesAsync(spritesheetPath, bytes);

            // Prepare and save metadata
            var metadata = new SpritesheetMetadata
            {
                sprites = sprites.Select(kvp => new SpriteMetadata
                {
                    id = kvp.Key,
                    rect = new RectData
                    {
                        x = kvp.Value.rect.x,
                        y = kvp.Value.rect.y,
                        width = kvp.Value.rect.width,
                        height = kvp.Value.rect.height
                    },
                    pivot = new Vector2Data
                    {
                        x = kvp.Value.pivot.x,
                        y = kvp.Value.pivot.y
                    }
                }).ToList()
            };

            string jsonMetadata = JsonUtility.ToJson(metadata, true);
            await File.WriteAllTextAsync(metadataPath, jsonMetadata);

            Debug.Log($"Spritesheet saved to: {spritesheetPath}");
            Debug.Log($"Metadata saved to: {metadataPath}");
        }

        public async Task<Dictionary<int, Sprite>> LoadSpritesheetFromDisk(string fileName)
        {
            string directory = Application.persistentDataPath;
            string spritesheetPath = Path.Combine(directory, $"{fileName}.png");
            string metadataPath = Path.Combine(directory, $"{fileName}_metadata.json");

            if (!File.Exists(spritesheetPath) || !File.Exists(metadataPath))
            {
                Debug.LogError("Spritesheet or metadata file not found.");
                return null;
            }

            // Load spritesheet
            byte[] fileData = await File.ReadAllBytesAsync(spritesheetPath);
            Texture2D loadedSpritesheet = new Texture2D(2, 2);
            loadedSpritesheet.LoadImage(fileData);

            // Load metadata
            string jsonMetadata = await File.ReadAllTextAsync(metadataPath);
            SpritesheetMetadata metadata = JsonUtility.FromJson<SpritesheetMetadata>(jsonMetadata);

            // Create sprites
            Dictionary<int, Sprite> loadedSprites = new Dictionary<int, Sprite>();
            foreach (var spriteData in metadata.sprites)
            {
                Rect rect = new Rect(spriteData.rect.x, spriteData.rect.y, spriteData.rect.width, spriteData.rect.height);
                Vector2 pivot = new Vector2(spriteData.pivot.x, spriteData.pivot.y);
                loadedSprites[spriteData.id] = Sprite.Create(loadedSpritesheet, rect, pivot);
            }

            return loadedSprites;
        }
    }
}