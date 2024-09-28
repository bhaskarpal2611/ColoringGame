using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame.UI
{
    public class UI_PencilItem : MonoBehaviour
    {
        [SerializeField] Image[] _pencilPieces;
        [SerializeField] Button _button;

        public Button Button { get { return _button; } }

        private const float XPOS_SELECTED_PEN = -50f;


        private void OnDestroy()
        {
            Button.onClick.RemoveAllListeners();
        }

        public void SetColorOnPencil(Color color)
        {
            for (int i = 0; i < _pencilPieces.Length; i++)
            {
                _pencilPieces[i].color = color;
            }
        }

        public void OnPenSelected() => transform.GetChild(0).DOLocalMoveX(XPOS_SELECTED_PEN, 0.5f);
        public void UnselectedPen() => transform.GetChild(0).DOLocalMoveX(0f, 0.5f);
    }
}
