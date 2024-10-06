using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ColorSwipeGame
{
    public class LevelSelectionManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private DrawnDataSO _drawings;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private Transform _loadingPanel;
        [SerializeField] private GameObject _selectionSceneCanvas;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private ReferenceImageLoader _referenceImageLoader;
        [SerializeField] private LeftPanelController _leftPanelHandler;
        [SerializeField] private PenSelectionHandler _penSelectionHandler;
        [SerializeField] private LevelImageHandler _levelImageHandler;
        [SerializeField] private DrawnImageHandler _drawnImageHandler;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private Draw_SaveManager _drawnSaveManager;
        [SerializeField] private float _levelLoadTimeDelay = 0.25f;

        private GameObject _currentLevel;
        private int _currentLevelIndex;
        private bool _firstTimeLoaded = false;

        public UnityEvent OnLevelLoaded = new();

        private Coroutine _saveCoroutine;

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

        public void LoadDrawingScene(int index = 0)
        {
            _selectionSceneCanvas.SetActive(false);
            _leftPanelHandler.ShowPanelAtStart();
            // _penSelectionHandler.ShowPanelAtStart();

            if (index == 0)
            {
                Debug.Log(_drawings.Levels.Count);
                _currentLevelIndex = _drawings.Levels.Count;
                _paintService.LoadDrawPaintLevel();
            }
            else
            {
                _paintService.OnEditedLevelLoad(_drawings.LoadDrawnTexture(index));
            }
            _paintService.CanPaint = true;

            OnLevelLoaded.Invoke();
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
                _levels.SetIsEdited(index, true);
            }
            _paintService.CanPaint = true;

            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
        {
            OnLevelLoaded.Invoke();
        });
        }


        // DRAW _ Paint Mode
        public void BackToSelection()
        {
            _loadingPanel.DOScale(1f, 0.25f).SetEase(Ease.InSine).OnComplete(() =>
            {
                if (_saveCoroutine != null)
                {
                    StopCoroutine(_saveCoroutine);
                }
                _saveCoroutine = StartCoroutine(SaveDrawing());
            });
        }

        // COLORING MODE
        public void GoBackToSelectionScene()
        {
            _loadingPanel.DOScale(1f, .25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                if (_saveCoroutine != null)
                {
                    StopCoroutine(_saveCoroutine);
                }
                _saveCoroutine = StartCoroutine(SaveTextures());
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

        private IEnumerator SaveDrawing()
        {
            Debug.Log(_currentLevelIndex);
            _drawnImageHandler.UpdateDrawing(_currentLevelIndex);
            SaveDrawnState();
            yield return null;

            _loadingPanel.DOScale(0f, 0.25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                _selectionSceneCanvas.SetActive(true);
                _drawnSaveManager.SaveDrawingsData();
                _paintService.OnBackButtonPressed();
            });
        }

        private void SaveLevelState()
        {
            _levels.SaveLevelState(_currentLevelIndex, _paintService.SaveCurrentState());
        }

        private void SaveDrawnState()
        {
            _drawings.SaveDrawnTexture(_currentLevelIndex, _paintService.SaveDrawnState());
        }

        private float CalculateScreenAspectRatio()
        {
            float width = Screen.width;
            float height = Screen.height;
            return width / height;
        }
    }
}
