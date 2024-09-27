using UnityEngine;
using System.IO;

namespace ColorSwipeGame
{
    public class CaptureCameraRender : MonoBehaviour
    {
        [SerializeField] private RenderTexture _cameraRT;

        private int counter = 1;

        public void TakePhoto()
        {
            string filePath = ""; // Declare filePath outside the platform-specific blocks

#if UNITY_EDITOR
            filePath = Application.dataPath + "/SavedPhotos/SavedPhoto_0" + counter++ + ".png";
#endif
#if UNITY_ANDROID
            string directoryPath = Application.persistentDataPath + "/SavedPhotos";

    // Ensure the directory exists on Android
    if (!Directory.Exists(directoryPath))
    {
        Directory.CreateDirectory(directoryPath);
    }

    filePath = directoryPath + "/SavedPhoto_0" + counter++ + ".png";
#endif

            SaveRenderTextureToPNG(_cameraRT, filePath);
        }

        private void SaveRenderTextureToPNG(RenderTexture renderTexture, string filePath)
        {
            // Create a Texture2D from the RenderTexture
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            // Encode texture to PNG
            byte[] bytes = texture.EncodeToPNG();

            // Save to file
            File.WriteAllBytes(filePath, bytes);

            Debug.Log("Saved RenderTexture to: " + filePath);
        }
    }
}