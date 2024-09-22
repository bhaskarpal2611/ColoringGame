using UnityEngine;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private GameObject _selectionSceneCanvas;

        public void LoadLevel(int index)
        {
            _selectionSceneCanvas.SetActive(false);
            var level = Instantiate(_levels.levels[index].levelPrefab, _levelParent);
        }
    }

}
