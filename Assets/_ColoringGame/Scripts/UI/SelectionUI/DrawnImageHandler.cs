using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class DrawnImageHandler : MonoBehaviour
    {
        [SerializeField] private Transform _sprites;
        [SerializeField] private DrawnDataSO _drawnData;
        [SerializeField] private DrawnPrefabHandler _drawingIconPrefab;
        [SerializeField] private LevelSelectionManager _levelSelectionManager;
        [SerializeField] private CaptureCameraRender _cameraScreenshotController;

        private RectTransform _rectTransform;

        private readonly Vector2 _basePosition = new Vector2(700f, -500f);
        private readonly Vector2 _baseContentSize = new Vector2(500f, 775f);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            LoadDrawingIcons();
        }


        public void UpdateDrawing(int index)
        {
            string fileName = _cameraScreenshotController.SaveCopy(index);
            _drawnData.SaveImageForIcon(index, fileName);
        }

        private void LoadDrawingIcons()
        {
            int counter = 0;
            // resize to base size
            _rectTransform.sizeDelta = _baseContentSize;

            for (int i = 0; i < _drawnData.Levels.Count; i++)
            {
                string fileName = _drawnData.GetImageIconFileName(i);
                if (fileName != null)
                {
                    string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");
                    if (File.Exists(filePath))
                    {
                        Sprite sprite = LoadSpritePNG(i, filePath);
                        int index = i;
                        DrawnPrefabHandler drawingIconPrefab = Instantiate(_drawingIconPrefab, _sprites);
                        drawingIconPrefab.SetImage(sprite);
                        drawingIconPrefab.Button.onClick.AddListener(() =>
                        {
                            _levelSelectionManager.LoadDrawingScene(index);
                        });

                        ArrangeIconPosition(counter, drawingIconPrefab);

                        ResizeContent();

                        counter++;
                    }
                }
            }
        }

        private void ResizeContent()
        {
            Vector2 sizeDelta = _rectTransform.sizeDelta;
            sizeDelta.x += 450f;
            _rectTransform.sizeDelta = sizeDelta;
        }

        private void ArrangeIconPosition(int counter, DrawnPrefabHandler drawingIconPrefab)
        {
            float xOffset = counter * 450f;
            float yOffset = (counter % 2 == 0) ? -500f : -250f;
            Vector2 position = new Vector2(_basePosition.x + xOffset, yOffset);

            // Set the position
            drawingIconPrefab.MovePosition(position);
        }

        private Sprite LoadSpritePNG(int i, string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
            texture.LoadImage(bytes);  // LoadImage auto-resizes the texture dimensions

            Rect rect = new Rect(0, 0, 1024, 1024);
            Sprite sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);

            return sprite;
        }
    }
}
