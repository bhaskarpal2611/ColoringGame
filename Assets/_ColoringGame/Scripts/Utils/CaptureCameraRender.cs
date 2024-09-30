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
        [SerializeField] private Image _referenceImage;
        [SerializeField] private Image _flashImage;
        [SerializeField] private string _albumName = "SavedPhotos";
        [SerializeField] private float _flashTimelength = 0.25f;

        private Texture2D texture;

        private int counter = 0;
        private float _startTime;
        private bool _flashing;
        private byte[] _bytes;

        public void TakePhoto()
        {
            SaveRenderTextureToPNG(_cameraRT);
        }

        public void SaveToGallery()
        {
            // string fileName = "ScreenShot_00" + ++counter + ".png";
            // NativeGallery.SaveImageToGallery(_bytes, _albumName, fileName, (success, path) =>
            // {
            //     if (success)
            //     {
            //         Debug.Log("saved success");
            //         Debug.Log(path);
            //     }
            //     else
            //     {
            //         Debug.Log("failed saving");
            //     }
            // });

            string editorPath = Path.Combine(Application.dataPath, "SavedImage.png");
            File.WriteAllBytes(editorPath, _bytes);
        }

        private void SaveRenderTextureToPNG(RenderTexture renderTexture)
        {
            Debug.Log(renderTexture.format);
            // Create a Texture2D from the RenderTexture
            Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderTexture;

            // Read pixels from the RenderTexture into the Texture2D
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            RenderTexture.active = null;

            // create a sprite from texture
            Sprite sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);
            sprite.name = "Saved Image";
            _referenceImage.sprite = sprite;
            _referenceImage.color = Color.white;


            byte[] bytes = texture.EncodeToPNG();
            _bytes = new byte[bytes.Length];
            _bytes = bytes;


            CameraFlash();

            // play camera click sound
            _audioManager.PlayCameraButtonSound();

            Debug.Log("Saved RenderTexture: ");
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