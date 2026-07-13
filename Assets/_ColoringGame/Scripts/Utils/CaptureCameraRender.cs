using DG.Tweening;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class CaptureCameraRender : MonoBehaviour
    {
        [SerializeField] private RenderTexture _cameraRT;
        [SerializeField] private string _captureOnlyLayerName = "Water";
        [SerializeField] private float _frameMargin = 0f;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private LeftPanelController _leftPanelController;
        [SerializeField] private ExpandButton _bottomLeftPanel;
        [SerializeField] private Image _referenceImage;
        [SerializeField] private Image _flashImage;
        [SerializeField] private string _albumName = "SavedPhotos";
        [SerializeField] private float _flashTimelength = 0.25f;
        [SerializeField] private Transform _paintToolSelection;
        [SerializeField] private RectTransform _paintToolPanel;
        [SerializeField] private Transform _backButton;
        [SerializeField] private Transform _clearButton;
        [SerializeField] private Transform _cameraButton;
        [SerializeField] private Camera _captureCamera;
        [SerializeField] private SpriteRenderer _paintingArea;

        public Texture2D _texture;
        public Sprite _sprite;
        private Rect _rect;

        private string _filePath;
        private int _captureOnlyLayer;
        private int counter = 0;
        private float _startTime;
        private byte[] _bytes;

        private void Start()
        {
            _captureOnlyLayer = LayerMask.NameToLayer(_captureOnlyLayerName);
            if (_captureOnlyLayer >= 0)
                _captureCamera.cullingMask = 1 << _captureOnlyLayer;
            else
                Debug.LogWarning($"[CaptureCameraRender] Layer '{_captureOnlyLayerName}' not found — capture will render all layers.");

            InitializeTexture();

            _filePath = Path.Combine(Application.persistentDataPath, _albumName);
            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
                Debug.Log($"Created folder: {_filePath}");
            }
            else
            {
                Debug.Log($"Folder already exists: {_filePath}");
            }
        }

        public void TakePhoto()
        {
            if (_leftPanelController != null)
                _leftPanelController.CompleteHidePanel();

            _backButton.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad);
            _clearButton.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad);
            _cameraButton.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad);

            _paintToolSelection.DOScale(0f, 0.25f).OnComplete(() =>
            {
                _paintToolPanel.DOLocalMoveX(1000f, .5f);
                TakeSnap();
                _bottomLeftPanel.PopOpen();
            });
        }

        public void SaveToGallery()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            string fileName = "ScreenShot_00" + ++counter + ".png";
            NativeGallery.SaveImageToGallery(_bytes, _albumName, fileName, (success, path) =>
            {
                Debug.Log(success ? $"Saved: {path}" : "Save failed");
            });
#endif

#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "SavedImage.png");
            File.WriteAllBytes(editorPath, _bytes);
#endif

            ClosePanel();
        }

        public void ClosePanel()
        {
            if (_leftPanelController != null)
                _leftPanelController.CloseSidePanel();
            _bottomLeftPanel.ForceCloseWindow();

            _paintToolPanel.DOLocalMoveX(0f, .5f).OnComplete(() =>
            {
                _paintToolSelection.DOScale(1f, 0.25f);
                _backButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
                _clearButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
                _cameraButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
            });
        }

        public string SaveCopy(int index, int isDrawing = 0)
        {
            AlignToPaintingArea();
            _captureCamera.Render();

            RenderTexture.active = _cameraRT;
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();
            RenderTexture.active = null;

            if (_sprite != null) Destroy(_sprite);

            Sprite sprite = Sprite.Create(_texture, _rect, Vector2.one * 0.5f);
            _sprite = sprite;
            _referenceImage.sprite = sprite;
            _referenceImage.color = Color.white;

            _bytes = _texture.EncodeToPNG();

            string fileName = isDrawing == 0 ? "SavedLevelImage_00" + index : "SavedDrawing_00" + index;
            string editorPath = Path.Combine(_filePath, fileName + ".png");
            System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(editorPath, _bytes));
            return fileName;
        }

        private void TakeSnap()
        {
            AlignToPaintingArea();
            _captureCamera.Render();

            RenderTexture.active = _cameraRT;
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();
            RenderTexture.active = null;

            if (_referenceImage.sprite != null) Destroy(_referenceImage.sprite);

            Sprite sprite = Sprite.Create(_texture, _rect, Vector2.one * 0.5f);
            sprite.name = "Saved Image";
            _referenceImage.sprite = sprite;
            _referenceImage.color = Color.white;

            _bytes = _texture.EncodeToPNG();

            CameraFlash();
            _audioManager.PlayCameraButtonSound();
        }

        /// <summary>
        /// Moves the painting area to the capture-only layer, then sizes and positions
        /// the capture camera to tightly frame it. Uses BoxCollider2D bounds when available
        /// so the crop stays consistent even when the sprite asset swaps.
        /// </summary>
        private void AlignToPaintingArea()
        {
            if (_paintingArea == null || _captureCamera == null) return;

            if (_captureOnlyLayer >= 0)
                SetLayerRecursively(_paintingArea.transform, _captureOnlyLayer);

            Transform t = _paintingArea.transform;
            Vector3 worldCenter, worldExtents;

            BoxCollider2D box = _paintingArea.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                worldCenter  = t.TransformPoint(box.offset);
                worldExtents = Vector3.Scale(new Vector3(box.size.x, box.size.y, 0f) * 0.5f, t.lossyScale);
            }
            else
            {
                Bounds localBounds = _paintingArea.sprite != null ? _paintingArea.sprite.bounds : _paintingArea.bounds;
                worldCenter  = t.TransformPoint(localBounds.center);
                worldExtents = Vector3.Scale(localBounds.extents, t.lossyScale);
            }

            _captureCamera.transform.position = new Vector3(worldCenter.x, worldCenter.y, _captureCamera.transform.position.z);

            float halfH = Mathf.Abs(worldExtents.y) + _frameMargin;
            float halfW = Mathf.Abs(worldExtents.x) + _frameMargin;

            // Use RT dimensions rather than Camera.aspect — on the first capture Camera.aspect
            // can still reflect the screen/game view instead of the RT, skewing the crop.
            float rtAspect = _cameraRT != null ? (float)_cameraRT.width / _cameraRT.height : _captureCamera.aspect;
            _captureCamera.orthographicSize = Mathf.Max(halfH, halfW / rtAspect);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
                SetLayerRecursively(child, layer);
        }

        private void InitializeTexture()
        {
            _rect = new Rect(0, 0, _cameraRT.width, _cameraRT.height);
            _texture = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
        }

        private void CameraFlash()
        {
            Color col = _flashImage.color;
            _startTime = Time.time;
            col.a = 1.0f;
            _flashImage.color = col;
            StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            bool done = false;
            while (!done)
            {
                Color col = _flashImage.color;
                float perc = (Time.time - _startTime) / _flashTimelength;
                if (perc > 1f) { perc = 1f; done = true; }
                col.a = Mathf.Lerp(1f, 0f, perc);
                _flashImage.color = col;
                yield return null;
            }
        }
    }
}
