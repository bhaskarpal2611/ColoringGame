using DG.Tweening;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private Transform _loadingPanel;
        [SerializeField] private GameObject _selectionSceneCanvas;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private ReferenceImageLoader _referenceImageLoader;
        [SerializeField] private LeftPanelController _leftPanelHandler;
        [SerializeField] private PenSelectionHandler _penSelectionHandler;
        [SerializeField] private LevelImageHandler _levelImageHandler;
        [SerializeField] private float _levelLoadTimeDelay = 0.25f;

        private GameObject _currentLevel;
        private int _currentLevelIndex;
        private bool _firstTimeLoaded = false;

        private string filePath;

        public UnityEvent OnLevelLoaded = new();

        private void Start()
        {
            float aspectRatio = CalculateScreenAspectRatio();
            if (aspectRatio > 1.5f)
            {
                Camera.main.orthographicSize = 5.76f;
            }
            else
            {
                Camera.main.orthographicSize = 7.5f;
            }
            filePath = Path.Combine(Application.persistentDataPath, "currentTexturesData.json");


            for (int i = 0; i < _levels.Levels.levelsData.Length; i++)
            {
                var tex = LoadCurrentTextures();
                if (tex != null)
                {
                    _levels.Levels.levelsData[i].CurrentTextures = tex;
                    _levels.Levels.levelsData[i].IsEdited = true;
                    _levels.Levels.levelsData[i].SavedImageData.FileName = tex.LevelEditedImage;
                    _levels.Levels.levelsData[i].SavedImageData.Level = i;
                }
            }

            _levelImageHandler.LoadSprites();
        }

        public void SaveCurrentTextures(AllTexturesData currentTextures)
        {
            // Serialize the CurrentTextures field to JSON
            string json = JsonUtility.ToJson(currentTextures, true);  // Pretty print for readability

            // Write the JSON to a file
            File.WriteAllText(filePath, json);

            Debug.Log("CurrentTextures data saved to: " + filePath);
        }

        // Load the CurrentTextures field
        public AllTexturesData LoadCurrentTextures()
        {
            if (File.Exists(filePath))
            {
                // Read the JSON file
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string back into AllTexturesData
                AllTexturesData currentTextures = JsonUtility.FromJson<AllTexturesData>(json);

                Debug.Log("CurrentTextures data loaded from: " + filePath);
                return currentTextures;
            }
            else
            {
                Debug.LogWarning("Save file not found");
                return null;
            }
        }

        public void LoadLevel(int index)
        {
            if (_currentLevel != null)
            {
                Destroy(_currentLevel);
            }

            _currentLevelIndex = index;
            _referenceImageLoader.SetReferenceImage(index);
            _selectionSceneCanvas.SetActive(false);
            _currentLevel = Instantiate(_levels.GetLevelPrefab(index), _levelParent);
            _leftPanelHandler.ShowPanelAtStart();


            if (_firstTimeLoaded)
            {
                _penSelectionHandler.ShowPanelAtStart();
                _firstTimeLoaded = true;
            }


            if (_levels.IsEdited(index))
            {
                _paintService.OnEditedLevelLoad(_levels.LoadTextures(index));
            }
            else
            {
                _paintService.OnLevelLoad();
            }
            _paintService.CanPaint = true;

            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
        {
            OnLevelLoaded.Invoke();
        });
        }

        public void GoBackToSelectionScene()
        {
            _loadingPanel.DOScale(1f, .5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                StartCoroutine(SaveTextures());
            });
        }

        private IEnumerator SaveTextures()
        {

            yield return new WaitForSeconds(2f);
            SaveLevelState();
            _levelImageHandler.UpdateSprite(_currentLevelIndex);
            yield return null;

            SaveCurrentTextures(_levels.Levels.levelsData[_currentLevelIndex].CurrentTextures);
            _loadingPanel.DOScale(0f, .25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                _selectionSceneCanvas.SetActive(true);
                _paintService.OnBackButtonPressed();
                Destroy(_currentLevel);
            });
        }

        public Sprite[] GetSprite()
        {
            return _levels.Levels.levelsData[0].OriginalSprites;
        }

        public void SaveLevelState()
        {
            _levels.SaveLevelState(_currentLevelIndex, _paintService.SaveCurrentState());

        }

        public float CalculateScreenAspectRatio()
        {
            float width = Screen.width;
            float height = Screen.height;
            return width / height;
        }
    }

}
