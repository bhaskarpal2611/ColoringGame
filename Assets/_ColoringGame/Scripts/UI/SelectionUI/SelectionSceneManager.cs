using UnityEngine;

namespace ColorSwipeGame
{
    public class SelectionSceneManager : MonoBehaviour
    {
        [SerializeField] private LevelDataSO _levels;
        [SerializeField] private Transform _levelParent;
        [SerializeField] private GameObject _selectionSceneCanvas;
        [SerializeField] private GPU_SpriteColoring _spriteColoringController;

        public void LoadLevel(int index)
        {
            Debug.Log("chk");
            _selectionSceneCanvas.SetActive(false);
            var level = Instantiate(_levels.levels[index].levelPrefab, _levelParent);
            _spriteColoringController.InitializeLevel();
                
        }
    }

}
