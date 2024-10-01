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

        private Texture2D _texture;
        private Rect _rect;

        private int counter = 0;
        private float _startTime;
        private bool _flashing;
        private byte[] _bytes;

        private void Start()
        {
            InitializeTexture();
        }

        public void TakePhoto()
        {
            _leftPanelController.CompleteHidePanel();
            _backButton.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad);
            _clearButton.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad);

            _paintToolSelection.DOScale(0f, 0.25f).OnComplete(() =>
            {
                _paintToolPanel.DOLocalMoveX(1000f, .5f).OnComplete(() =>
                {
                    SaveRenderTextureToPNG();
                    _bottomLeftPanel.PopOpen();
                });
            });
        }

        public void SaveToGallery()
        {
            #region ANDROID
#if UNITY_ANDROID
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
            _leftPanelController.CloseSidePanel();
            _bottomLeftPanel.ForceCloseWindow();
            // move back the panel
            _backButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);
            _clearButton.DOScale(1f, 0.25f).SetEase(Ease.InOutQuad);

            _paintToolPanel.DOLocalMoveX(0f, .5f).OnComplete(() =>
            {
                _paintToolSelection.DOScale(1f, 0.25f);
            });
        }

        public Texture2D SaveTextureCopy()
        {
            RenderTexture.active = _cameraRT;

            // Read pixels from the RenderTexture into the Texture2D
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();

            return _texture;
        }

        private void SaveRenderTextureToPNG()
        {
            RenderTexture.active = _cameraRT;

            // Read pixels from the RenderTexture into the Texture2D
            _texture.ReadPixels(new Rect(0, 0, _cameraRT.width, _cameraRT.height), 0, 0);
            _texture.Apply();

            RenderTexture.active = null;

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

        private void InitializeTexture()
        {
            _rect = new Rect(0, 0, _cameraRT.width, _cameraRT.height);
            _texture = new Texture2D(_cameraRT.width, _cameraRT.height, TextureFormat.ARGB32, false);
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
                _flashing = true;

                yield return null;
            }

            _flashing = false;

            yield break;
        }
    }
}