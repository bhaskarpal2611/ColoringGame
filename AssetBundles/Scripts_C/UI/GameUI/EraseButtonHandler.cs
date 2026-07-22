using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ColorSwipeGame.UI;

namespace ColorSwipeGame
{
    public class EraseButtonHandler : MonoBehaviour
    {
        [SerializeField] private PensHandler _pensHandler;
        [SerializeField] private RectTransform _button;
        [SerializeField] private Image _selectionRing; // assign a ring Image on the eraser button

        private const float SELECTED_SCALE = 1.2f;
        private const float NORMAL_SCALE   = 1.0f;
        private bool _isOpen = false;

        // on click
        public void SelectEraser()
        {
            if (!_isOpen)
            {
                _isOpen = true;
                _button.DOScale(SELECTED_SCALE, 0.2f).SetEase(Ease.OutBack);
                if (_selectionRing != null) _selectionRing.enabled = true;
                _pensHandler.UnselectAll();
            }
        }

        public void UnselectEraser()
        {
            if (_isOpen)
            {
                _isOpen = false;
                _button.DOScale(NORMAL_SCALE, 0.2f).SetEase(Ease.InOutSine);
                if (_selectionRing != null) _selectionRing.enabled = false;
            }
        }
    }
}
