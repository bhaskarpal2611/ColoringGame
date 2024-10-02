using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
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

        public void LoadLevel(int index)
        {
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

            // if (_levels.IsEdited(index) && _levels.GetLevelState(index) != null)
            // {
            //     _paintService.OnEditedLevelLoad(_levels.GetLevelState(index));
            // }
            else
            {
                _paintService.OnLevelLoad();
            }
            _paintService.CanPaint = true;

            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
        {

            // OnLevelLoaded.Invoke();
        });
        }

        public void GoBackToSelectionScene()
        {
            _levels.SaveLevelState(_currentLevelIndex, _paintService.SaveCurrentState());
            _levelImageHandler.UpdateSprite();
            _selectionSceneCanvas.SetActive(true);
            Destroy(_currentLevel);
        }

        public float CalculateScreenAspectRatio()
        {
            float width = Screen.width;
            float height = Screen.height;
            return width / height;
        }
    }

}
