using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;


namespace ColorTap
{
    public class Screenshot : MonoBehaviour
    {
        public GameObject uiPanel;
        public GameObject frame;
        public Image PreviewImage; 
        public GameObject previewSS;

        public void TakeScreenshot()
        {
            Debug.Log("TakeScreenshot");
            StartCoroutine(TakeAndSaveScreenshot());
            

        }

        public IEnumerator TakeAndSaveScreenshot()
        {
            yield return new WaitForEndOfFrame();
            uiPanel.SetActive(false);
            frame.SetActive(true);

            yield return new WaitForSeconds(0.5f);
            frame.SetActive(false);
            uiPanel.SetActive(false);
            //ImageChanger.Instance.currentImage.SetActive(false);

            Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            //Get Image from screen
            screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenImage.Apply();

            //Convert to png
            byte[] imageBytes = screenImage.EncodeToPNG();

            //Save image to gallery
            NativeGallery.SaveImageToGallery(imageBytes, "AlbumName", "ScreenshotName.png", null);
           

            // Create sprite and assign it to the UI Image component
            previewSS.SetActive(true);
            Sprite mySprite = Sprite.Create(screenImage, new Rect(0.0f, 0.0f, screenImage.width, screenImage.height), new Vector2(0.5f, 0.5f));
            PreviewImage.sprite = mySprite; // Ensure PreviewImage is set in the inspector

            yield return new WaitForSeconds(1f);
        }
    }
}
