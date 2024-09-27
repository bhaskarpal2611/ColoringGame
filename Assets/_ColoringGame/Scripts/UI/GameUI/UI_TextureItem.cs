using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ColorSwipeGame
{
    public class UI_TextureItem : MonoBehaviour
    {
        [SerializeField] private Button _button;
        private const float XPOS_SELECTED_PEN = -75f;
        
        private void OnDestroy()
        {
            Button.onClick.RemoveAllListeners();
        }

        public Button Button { get { return _button; } }
        public void OnTextureSelected() => transform.DOLocalMoveX(XPOS_SELECTED_PEN, 0.5f);
        public void UnselectedPen() => transform.DOLocalMoveX(0f, 0.5f);

    }
}
