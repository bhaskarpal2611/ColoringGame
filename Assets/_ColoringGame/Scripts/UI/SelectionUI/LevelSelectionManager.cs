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
        [SerializeField] private GameObject _selectionSceneCanvas;   // Selection_Scene
        [SerializeField] private GameObject _gameSceneRoot;           // Game_Scene  ← assign in Inspector
        [SerializeField] private SpriteRenderer _boardFrame;          // BG_FRAME — assign in Inspector
        [SerializeField] private Vector2 _drawingAreaOffset = new Vector2(0f, 0.4f);
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

        public UnityEvent OnLevelLoaded, OnBackButtonPressed = new UnityEvent();

        private Coroutine _saveCoroutine;

        public void Start()
        {
            // Ensure correct starting state
            _selectionSceneCanvas?.SetActive(true);
            _gameSceneRoot?.SetActive(false);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayIntroAudio();
        }

        // ── Board alignment ───────────────────────────────────────────────────────

        private void AlignLevelToBoard()
        {
            if (_boardFrame == null || _levelParent == null) return;
            Vector3 c = _boardFrame.bounds.center;
            _levelParent.position = new Vector3(
                c.x + _drawingAreaOffset.x,
                c.y + _drawingAreaOffset.y,
                _levelParent.position.z);
        }

        // ── Entry points ─────────────────────────────────────────────────────────

        public void LoadLevel(int index)
        {
            if (_currentLevel != null)
                Destroy(_currentLevel);

            _currentLevelIndex = index;

            // Show game, hide selection
            _selectionSceneCanvas?.SetActive(false);
            _gameSceneRoot?.SetActive(true);   // activates PaintService so Awake runs

            if (_referenceImageLoader != null)
                _referenceImageLoader.SetReferenceImage(index);

            _currentLevel = Instantiate(_levels.GetLevelPrefab(index), _levelParent);
            AlignLevelToBoard();

            if (_leftPanelHandler != null)
                _leftPanelHandler.ShowPanelAtStart();

            _penSelectionHandler?.ShowPanelAtStart();
            _firstTimeLoaded = true;

            if (_levels.IsEdited(index))
                _paintService.OnEditedLevelLoad(_levels.LoadTextures(index));
            else
            {
                _paintService.OnLevelLoad();
                _levels.SetIsEdited(index, true);
            }

            _paintService.CanPaint = true;

            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
            {
                OnLevelLoaded?.Invoke();
            });
        }

        public void LoadDrawingScene(int index = -1)
        {
            _selectionSceneCanvas?.SetActive(false);
            _gameSceneRoot?.SetActive(true);

            if (_leftPanelHandler != null)
                _leftPanelHandler.ShowPanelAtStart();

            if (index == -1)
            {
                _currentLevelIndex = _drawings.Levels.Count;
                _paintService.LoadDrawPaintLevel(_drawings.OriginalEmptySprite);
            }
            else
            {
                _currentLevelIndex = index;
                _paintService.OnEditedLevelLoad(_drawings.LoadDrawnTexture(index));
            }

            _paintService.CanPaint = true;
            OnLevelLoaded?.Invoke();
        }

        // ── Back / Save paths ────────────────────────────────────────────────────

        // Draw mode back
        public void BackToSelection()
        {
            if (_saveCoroutine != null) StopCoroutine(_saveCoroutine);
            _saveCoroutine = StartCoroutine(SaveDrawing());
        }

        // Coloring mode back
        public void GoBackToSelectionScene()
        {
            if (_saveCoroutine != null) StopCoroutine(_saveCoroutine);
            _saveCoroutine = StartCoroutine(SaveTextures());
        }

        private IEnumerator SaveTextures()
        {
            yield return null;
            yield return SaveLevelState();

            if (_levelImageHandler != null)
                _levelImageHandler.UpdateSprite(_currentLevelIndex);

            _saveManager?.SaveLevelsData();
            _paintService.OnBackButtonPressed();

            if (_currentLevel != null)
                Destroy(_currentLevel);

            _gameSceneRoot?.SetActive(false);
            _selectionSceneCanvas?.SetActive(true);

            if (_saveManager != null && _saveManager.CheckAllLevelsCompleted())
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayGameEndAudio();
        }

        private IEnumerator SaveDrawing()
        {
            yield return null;

            if (_paintService.IsDrawingEdited())
            {
                _drawnImageHandler?.UpdateDrawing(_currentLevelIndex);
                SaveDrawnState();
            }

            yield return null;

            if (_paintService.IsDrawingEdited())
                _drawnSaveManager?.SaveDrawingsData();

            _paintService.OnBackButtonPressed();

            _gameSceneRoot?.SetActive(false);
            _selectionSceneCanvas?.SetActive(true);
        }

        private IEnumerator SaveLevelState()
        {
            yield return _levels.SaveLevelState(_currentLevelIndex, _paintService.SaveCurrentState());
        }

        private void SaveDrawnState()
        {
            _drawings?.SaveDrawnTexture(_currentLevelIndex, _paintService.SaveDrawnState());
        }
    }
}
