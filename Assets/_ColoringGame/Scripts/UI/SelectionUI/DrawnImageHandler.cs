using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class DrawnImageHandler : MonoBehaviour
    {
        [SerializeField] private Transform _contentParent;
        [SerializeField] private Transform _newDrawing;
        [SerializeField] private DrawnDataSO _drawnData;
        [SerializeField] private DrawnPrefabHandler _drawingIconPrefab;
        [SerializeField] private LevelSelectionManager _levelSelectionManager;
        [SerializeField] private CaptureCameraRender _cameraScreenshotController;
        [SerializeField] private Sprite[] _pins;

        private RectTransform _rectTransform;

        private Vector2 _basePosition = new Vector2(250f, -500f);
        private Vector2 _baseContentSize = new Vector2(500f, 775f);

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
            string fileName = _cameraScreenshotController.SaveCopy(index, 1); // passing 1 to take screenshot photo by another name
            _drawnData.SaveImageForIcon(index, fileName);
        }

        bool initLoadFlag = false;

        private void LoadDrawingIcons()
        {
            if (initLoadFlag)
            {
                Debug.Log("level count: " + (_drawnData.Levels.Count - 1));
                Debug.Log("tf count: " + transform.childCount);

                int index = _drawnData.Levels.Count - 1;

                string fileName = _drawnData.GetImageIconFileName(index);
                if (fileName != null)
                {
                    InitializeDrawingImagePrefab(index, fileName);
                }
                return;
            }

            for (int i = _drawnData.Levels.Count - 1; i >= 0; i--)
            {
                string fileName = _drawnData.GetImageIconFileName(i);
                if (fileName != null)
                {
                    InitializeDrawingImagePrefab(i, fileName);
                }
            }
            initLoadFlag = true;
        }

        int _currentIndex = -1;
        int _signChanger = -1;

        private void InitializeDrawingImagePrefab(int i, string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, "SavedPhotos/" + fileName + ".png");
            if (File.Exists(filePath))
            {
                Sprite sprite = LoadSpritePNG(i, filePath);
                int index = i;
                DrawnPrefabHandler drawingIconPrefab = Instantiate(_drawingIconPrefab, _contentParent);
                drawingIconPrefab.SetImage(sprite);
                drawingIconPrefab.transform.SetAsFirstSibling();
                int randomIndex = Random.Range(0, _pins.Length);
                drawingIconPrefab.SetPin(_pins[randomIndex]);
                drawingIconPrefab.Button.onClick.AddListener(() =>
                {
                    _currentIndex = index;
                    _levelSelectionManager.LoadDrawingScene(index);
                });

                float zRotation = Random.Range(2f, 7f);

                _signChanger *= -1;
                zRotation *= _signChanger;

                drawingIconPrefab.transform.localEulerAngles = new(transform.localEulerAngles.x, transform.localEulerAngles.y, zRotation);
            }
            _newDrawing.SetAsFirstSibling();
        }

        private void ResizeContent()
        {
            Vector2 sizeDelta = _rectTransform.sizeDelta;
            sizeDelta.x += 550f;
            _rectTransform.sizeDelta = sizeDelta;
        }

        private void ArrangeIconPosition(int counter, DrawnPrefabHandler drawingIconPrefab)
        {
            // Increment the base x position by a fixed amount each time
            _basePosition.x += 550f;  // or 500f based on preference

            // Use counter for yOffset to alternate the position
            //float yOffset = (counter % 2 == 0) ? -500f : -250f;
            float yOffset = -375f;

            Vector2 position = new Vector2(_basePosition.x, yOffset);

            // Set the position
            drawingIconPrefab.MovePosition(position);
        }

        private Sprite LoadSpritePNG(int i, string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);  // Size doesn't matter here; it will be overridden by LoadImage
            texture.LoadImage(bytes);  // LoadImage auto-resizes the texture dimensions

            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);

            return sprite;
        }
    }
}
