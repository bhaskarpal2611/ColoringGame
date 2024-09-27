using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{


    public class PhotoManager : MonoBehaviour
    {
        private  string savedPhotosDirectory;

        private void Start()
        {
#if UNITY_EDITOR
            savedPhotosDirectory = Application.persistentDataPath + "/SavedPhotos";
#elif UNITY_ANDROID
        savedPhotosDirectory = Application.persistentDataPath + "/SavedPhotos";
#endif

            // Ensure the directory exists
            if (Directory.Exists(savedPhotosDirectory))
            {
                LoadFirstSavedPhoto();
            }
            else
            {
                Debug.Log("SavedPhotos directory does not exist.");
            }
        }

        private void DisplaySavedPhotos()
        {
            // Get all PNG files in the directory
            string[] files = Directory.GetFiles(savedPhotosDirectory, "*.png");

            // 


            if (files.Length > 0)
            {
                Debug.Log("Displaying Saved Photos:");

                // Loop through each file and display its name
                foreach (string file in files)
                {
                    Debug.Log("Saved Photo: " + Path.GetFileName(file));
                }
            }
            else
            {
                Debug.Log("No photos found in SavedPhotos directory.");
            }
        }

        public void LoadFirstSavedPhoto()
        {
            string[] files = Directory.GetFiles(savedPhotosDirectory, "*.png");

            if (files.Length > 0)
            {
                DisplayTextureFromPath(files[0]);
            }
            else
            {
                Debug.Log("No saved photos found.");
            }
        }

        [SerializeField] private Canvas _canvas;
        public void DisplayTextureFromPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Load PNG file data into a Texture2D
                byte[] imageData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2); // Create a new texture (initial size 2x2 doesn't matter)
                texture.LoadImage(imageData); // Load the image data into the texture

                // Dynamically create a new GameObject for the RawImage
                GameObject newImageObject = new GameObject("SavedPhoto");

                // Add a RawImage component to the new GameObject
                RawImage rawImage = newImageObject.AddComponent<RawImage>();

                // Assign the Texture2D to the RawImage component
                rawImage.texture = texture;

                // Set the parent to the canvas so it's part of the UI
                newImageObject.transform.SetParent(_canvas.transform, false);

                // Optionally, adjust the RectTransform (size, position, etc.)
                RectTransform rectTransform = newImageObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(300, 300); // Set size (width x height)
                rectTransform.anchoredPosition = Vector2.zero; // Set position (centered)

                Debug.Log("Photo displayed from: " + filePath);
            }
            else
            {
                Debug.LogError("File not found: " + filePath);
            }
        }
    }
}