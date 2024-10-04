using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class LevelImageHandler : MonoBehaviour
    {
        [SerializeField] private LevelImageSO _sprites;
        [SerializeField] private LevelDataSO _levelData;
        [SerializeField] private CaptureCameraRender _cameraScreenshotController;

        public Sprite _Sprite;
        public Texture2D _texture;

        private Image[] _images;

        private bool[] _isEdited;

        private void Awake()
        {
            _images = new Image[transform.childCount];
            int i = 0;
            foreach (Transform child in transform)
            {
                _images[i++] = child.GetChild(0).GetComponent<Image>();
            }
        }
        private void OnEnable()
        {
            LoadSprites();
        }

        public void UpdateSprite(int index)
        {
            string fileName = _cameraScreenshotController.SaveCopy(index);
            SavedImageData savedImageData = new(index, fileName);
            _levelData.SaveEditedImage(savedImageData);
        }

        public void LoadSprites()
        {
            for (int i = 0; i < _sprites.data.LevelSprites.Length; i++)
            {
                string fileName = _levelData.GetEditedImage(i);

                if (fileName != null || fileName != "")
                {
                    string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");
                    if (File.Exists(filePath))
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
                        texture.LoadImage(bytes);  // LoadImage auto-resizes the texture dimensions
                        _texture = texture;
                        Rect rect = new Rect(0, 0, 1024, 1024);
                        Sprite sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);
                        _Sprite = sprite;
                        _images[i].sprite = sprite;
                    }
                    else
                    {
                        Debug.Log("filename is not valid. loading default sprite for level");
                        _images[i].sprite = _sprites.data.LevelSprites[i];

                    }
                }
                else
                {
                    _images[i].sprite = _sprites.data.LevelSprites[i];
                }
            }
        }
    }

    [System.Serializable]
    public struct SavedImageData
    {
        public int Level;
        public string FileName;

        public SavedImageData(int index, string fileName)
        {
            Level = index;
            FileName = fileName;
        }
    }
}
