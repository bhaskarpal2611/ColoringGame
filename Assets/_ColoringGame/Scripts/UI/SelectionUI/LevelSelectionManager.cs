using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ColorSwipeGame
{
    public class LevelSelectionManager : MonoBehaviour
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
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private float _levelLoadTimeDelay = 0.25f;

        private GameObject _currentLevel;
        private int _currentLevelIndex;
        private bool _firstTimeLoaded = false;

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
        }

        public void LoadDrawingScene()
        {
            _selectionSceneCanvas.SetActive(false);
            _leftPanelHandler.ShowPanelAtStart();
            _penSelectionHandler.ShowPanelAtStart();

            _paintService.OnLevelLoad();
            _paintService.CanPaint = true;
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
                _levels.Levels.levelsData[index].IsEdited = true;
            }
            _paintService.CanPaint = true;

            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
        {
            OnLevelLoaded.Invoke();
        });
        }

        public void GoBackToSelectionScene()
        {
            _loadingPanel.DOScale(1f, .25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                StartCoroutine(SaveTextures());
            });
        }

        private IEnumerator SaveTextures()
        {
            yield return null;

            SaveLevelState();
            _levelImageHandler.UpdateSprite(_currentLevelIndex);

            _loadingPanel.DOScale(0f, 0.25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                _saveManager.SaveLevelsData();
                _selectionSceneCanvas.SetActive(true);
                _paintService.OnBackButtonPressed();
                Destroy(_currentLevel);
            });
        }

        public Sprite[] GetSprite()
        {
            return _levels.Levels.levelsData[0].OriginalSprites;
        }

        private void SaveLevelState()
        {
            _levels.SaveLevelState(_currentLevelIndex, _paintService.SaveCurrentState());
        }

        private float CalculateScreenAspectRatio()
        {
            float width = Screen.width;
            float height = Screen.height;
            return width / height;
        }
    }
}
