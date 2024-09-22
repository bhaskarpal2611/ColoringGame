using UnityEngine;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private GameObject _selectionSceneCanvas;

        private GameObject _currentLevel;

        public void LoadLevel(int index)
        {
            _selectionSceneCanvas.SetActive(false);
            _currentLevel = Instantiate(_levels.levels[index].levelPrefab, _levelParent);
        }

        public void GoBackToSelectionScene()
        {
            _selectionSceneCanvas.SetActive(true);
            Destroy(_currentLevel);
        }
    }

}
