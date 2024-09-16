using UnityEngine;
using UnityEngine.UI;

namespace ColoringGame.UI
{

    public class UI_PencilItem : MonoBehaviour
    {
        [SerializeField] Image[] _pencilPieces;
        [SerializeField] Button _button;

        public Button Button { get { return _button; } } 

        private void OnDisable()
        {
            Button.onClick.RemoveAllListeners();
        }

        public void PickColor(Color color)
        {
            for (int i = 0; i < _pencilPieces.Length; i++)
            {
                _pencilPieces[i].color = color;
            }
        }
    }
}
