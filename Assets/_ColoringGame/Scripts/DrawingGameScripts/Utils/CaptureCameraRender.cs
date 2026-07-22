using DG.Tweening;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
{
    public class CaptureCameraRender : MonoBehaviour
    {
        [SerializeField] private RenderTexture _cameraRT;
        [SerializeField] private string _captureOnlyLayerName = "Water";
        [SerializeField] private float _frameMargin = 0f;
        private Camera _camera;
        private int _captureOnlyLayer;
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
        [SerializeField] private SpriteRenderer _paintingArea;

        public Texture2D _texture;
        public Sprite _sprite;
        private Rect _rect;

        private string _filePath;

        private int counter = 0;
        private float _startTime;
        private byte[] _bytes;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _captureOnlyLayer = LayerMask.NameToLayer(_captureOnlyLayerName);
            if (_captureOnlyLayer >= 0)
            {
                _camera.cullingMask = 1 << _captureOnlyLayer;
            }
            else
            {
                Debug.LogWarning($"[CaptureCameraRender] Layer '{_captureOnlyLayerName}' not found — capture will render all layers.");
            }

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
            #region ANDROID
#if UNITY_ANDROID || UNITY_EDITOR
            string fileName = "ScreenShot_00" + ++counter + ".png";
            NativeGallery.SaveImageToGallery(_bytes, _albumName, fileName, (success, path) =>
            {
                if (success)
                {
                    Debug.Log("saved success");
                    Debug.Log(path);
                }
                else
                {
                    Debug.Log("failed saving");
                }
            });
#endif
            #endregion

            #region EDITOR
#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "SavedImage.png");
            File.WriteAllBytes(editorPath, _bytes);
#endif
            #endregion

            ClosePanel();
        }

        public void ClosePanel()
        {
            if (_leftPanelController != null)
                _leftPanelController.CloseSidePanel();
            _bottomLeftPanel.ForceCloseWindow();
            // move back the panel


            _paintToolPanel.DOLocalMoveX(0f, .5f).OnComplete(() =>
            {
                //_paintingArea.DOFade(1f, 0.25f);
                _paintToolSelection.DOScale(1f, 0.25f);
                _backButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
                _clearButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
                _cameraButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);

            });

        }


        public string SaveCopy(int index, int isDrawing = 0)
        {
            AlignToPaintingArea();
            _camera.Render();

            RenderTexture.active = _cameraRT;

            //_rect = new Rect(0, 0, _cameraRT.width, _cameraRT.height);
            //_texture = new Texture2D(_cameraRT.width, _cameraRT.height, TextureFormat.ARGB32, false);

            // Read pixels from the RenderTexture into the Texture2D
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();

            RenderTexture.active = null;

            if (_sprite != null) Destroy(_sprite); 

            // create a sprite from texture
            Sprite sprite = Sprite.Create(_texture, _rect, Vector2.one * 0.5f);
            _sprite = sprite;
            _referenceImage.sprite = sprite;
            _referenceImage.color = Color.white;

            _bytes = _texture.EncodeToPNG();

            string fileName;
            if (isDrawing == 0)
            {
                fileName = "SavedLevelImage_00" + index;
            }
            else
            {
                fileName = "SavedDrawing_00" + index;
            }

            string editorPath = Path.Combine(_filePath, fileName + ".png");
            System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(editorPath, _bytes));
            return fileName;
        }

        private void TakeSnap()
        {
            //_paintingArea.DOFade(0f, 0.25f);

            AlignToPaintingArea();
            _camera.Render();

            RenderTexture.active = _cameraRT;

            // Read pixels from the RenderTexture into the Texture2D
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();

            RenderTexture.active = null;

            if (_referenceImage.sprite != null) Destroy(_referenceImage.sprite);

            // create a sprite from texture
            Sprite sprite = Sprite.Create(_texture, _rect, Vector2.one * 0.5f);
            sprite.name = "Saved Image";
            _referenceImage.sprite = sprite;
            _referenceImage.color = Color.white;

            _bytes = _texture.EncodeToPNG();

            CameraFlash();

            // play camera click sound
            _audioManager.PlayCameraButtonSound();


        }

        /// <summary>
        /// Puts BG_FRAME (and any runtime children, e.g. the paint stroke layer) on the
        /// capture-only layer, then sizes/positions this orthographic camera to exactly
        /// bound its SpriteRenderer so the capture is a tight crop of just the painting.
        /// </summary>
        private void AlignToPaintingArea()
        {
            if (_paintingArea == null || _camera == null) return;

            if (_captureOnlyLayer >= 0)
            {
                SetLayerRecursively(_paintingArea.transform, _captureOnlyLayer);
            }

            // Framing off the sprite's own bounds is unreliable: PaintController swaps
            // BG_FRAME's sprite between a blank template (new drawing) and the reloaded PNG
            // (reopened drawing), and those two sprite assets can have different pixel
            // dimensions/pivots — giving a different crop depending on which one happens to
            // be assigned. BG_FRAME's BoxCollider2D size is fixed and sprite-independent
            // (used for paint hit-testing), so anchor framing to that instead for a
            // consistent crop regardless of which sprite is currently loaded.
            Transform paintingTransform = _paintingArea.transform;
            Vector3 worldCenter;
            Vector3 worldExtents;

            BoxCollider2D box = _paintingArea.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                worldCenter = paintingTransform.TransformPoint(box.offset);
                worldExtents = Vector3.Scale(new Vector3(box.size.x, box.size.y, 0f) * 0.5f, paintingTransform.lossyScale);
            }
            else
            {
                Bounds localBounds = _paintingArea.sprite != null ? _paintingArea.sprite.bounds : _paintingArea.bounds;
                worldCenter = paintingTransform.TransformPoint(localBounds.center);
                worldExtents = Vector3.Scale(localBounds.extents, paintingTransform.lossyScale);
            }

            transform.position = new Vector3(worldCenter.x, worldCenter.y, transform.position.z);

            float halfHeightNeeded = Mathf.Abs(worldExtents.y) + _frameMargin;
            float halfWidthNeeded = Mathf.Abs(worldExtents.x) + _frameMargin;

            // Use the render texture's own dimensions rather than Camera.aspect — on the very
            // first capture, Camera.aspect can still reflect the screen/game view instead of
            // the RT it's about to render into, producing a skewed first shot.
            float rtAspect = _cameraRT != null ? (float)_cameraRT.width / _cameraRT.height : _camera.aspect;

            _camera.orthographicSize = Mathf.Max(halfHeightNeeded, halfWidthNeeded / rtAspect);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private void InitializeTexture()
        {
            _rect = new Rect(0, 0, _cameraRT.width, _cameraRT.height);
            _texture = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
        }

        private void CameraFlash()
        {
            // initial color
            Color col = _flashImage.color;

            // start time to fade over time
            _startTime = Time.time;


            // start it as alpha = 1.0 (opaque)
            col.a = 1.0f;

            // flash image start color
            _flashImage.color = col;

            StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            bool done = false;

            while (!done)
            {
                float perc;
                Color col = _flashImage.color;

                perc = Time.time - _startTime;
                perc = perc / _flashTimelength;

                if (perc > 1.0f)
                {
                    perc = 1.0f;
                    done = true;
                }

                col.a = Mathf.Lerp(1.0f, 0.0f, perc);
                _flashImage.color = col;

                yield return null;
            }

            yield break;
        }
    }
}