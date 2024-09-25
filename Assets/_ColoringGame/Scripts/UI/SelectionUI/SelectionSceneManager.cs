using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private GameObject _selectionSceneCanvas;
        [SerializeField] private ReferenceImageLoader _referenceImageLoader;

        [SerializeField] private float _levelLoadTimeDelay = 0.25f;
        private GameObject _currentLevel;


        private void Start()
        {
            Debug.Log(CalculateScreenAspectRatio());
        }

        public UnityEvent OnLevelLoaded = new();

        public void LoadLevel(int index)
        {
            _referenceImageLoader.SetReferenceImage(index);
            _selectionSceneCanvas.SetActive(false);
            _currentLevel = Instantiate(_levels.GetLevelPrefab(index), _levelParent);
            transform.DOMove(transform.position, _levelLoadTimeDelay).OnComplete(() =>
            {
                OnLevelLoaded.Invoke();
            });
        }

        public void GoBackToSelectionScene()
        {
            _selectionSceneCanvas.SetActive(true);
            Destroy(_currentLevel);
        }

        public float CalculateScreenAspectRatio()
        {
            float width = (float)Screen.width;
            float height = (float)Screen.height;
            return width / height;
        }
    }

}
