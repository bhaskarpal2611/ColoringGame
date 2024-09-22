using UnityEngine;
using System.IO;

namespace ColorSwipeGame
{
    public class CaptureCameraRender : MonoBehaviour
    {
        [SerializeField] private RenderTexture _cameraRT;

        public void TakePhoto(int levelNumber)
        {
            SaveRenderTextureToPNG(_cameraRT, Application.dataPath + "/Resources/SavedPhotos/RenderTextureOutput_" + levelNumber + ".png");
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