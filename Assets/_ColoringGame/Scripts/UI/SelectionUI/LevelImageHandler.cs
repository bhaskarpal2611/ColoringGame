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

        private Texture2D _texture;
        private Image[] _images;

        private void Awake()
        {
            _images = new Image[transform.childCount];
            int i = 0;
            foreach (Transform child in transform)
            {
                _images[i++] = child.GetChild(0).GetComponent<Image>();
            }
        }

        private void OnDestroy()
        {
            Destroy(_texture);
        }

        private void OnEnable()
        {
            LoadSprites();
        }

        public void UpdateSprite(int index)
        {
            string fileName = _cameraScreenshotController.SaveCopy(index);
            _levelData.SaveEditedImage(index, fileName);
            _levelData.SaveLevelData(index);
        }

        public void LoadSprites()
        {

#if UNITY_ANDROID 
            for (int i = 0; i < _sprites.data.LevelSprites.Length; i++)
            {
                string fileName = _levelData.GetEditedImage(i);

                if (fileName != null || fileName != "")
                {
                    string filePath = Path.Combine(Application.persistentDataPath, "SavedPhotos/" + fileName + ".png");
                    if (File.Exists(filePath))
                    {
                        LoadSpritePNG(i, filePath);
                    }
                    else
                    {
                        _images[i].sprite = _sprites.data.LevelSprites[i];
                    }
                }
                else
                {
                    _images[i].sprite = _sprites.data.LevelSprites[i];
                }
            }
#endif

#if UNITY_IPHONE || UNITY_IOS
            for (int i = 0; i < _sprites.data.LevelSprites.Length; i++)
            {
                _images[i].sprite = _sprites.data.LevelSprites[i];
            }
#endif
        }
        private void LoadSpritePNG(int i, string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            _texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
            _texture.LoadImage(bytes);  // LoadImage auto-resizes the texture dimensions
            Rect rect = new Rect(0, 0, 1024, 1024);
            Sprite sprite = Sprite.Create(_texture, rect, Vector2.one * 0.5f);
            _images[i].sprite = sprite;
        }
    }
}
