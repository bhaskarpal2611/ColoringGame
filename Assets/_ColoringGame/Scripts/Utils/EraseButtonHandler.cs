using UnityEngine;
using DG.Tweening;
using ColorSwipeGame.UI;

namespace ColorSwipeGame
{
    public class EraseButtonHandler : MonoBehaviour
    {
        [SerializeField] private PensHandler _pensHandler;
        [SerializeField] private RectTransform _button;

        private const float SLIDE_OUT = -30f;   
        private const float SLIDE_BACK_IN = 0f;
        private bool _isOpen = false;

        // on click
        public void SelectEraser()
        {
            if (!_isOpen)
            {
                _button.DOAnchorPosX(SLIDE_OUT, 0.25f).OnComplete(() =>
                {
                    _isOpen = true;
                });
                _pensHandler.UnselectAll();
            }
        }

        public void UnselectEraser()
        {
            if (_isOpen)
            {
                _button.DOAnchorPosX(SLIDE_BACK_IN, 0.25f).OnComplete(() =>
                {
                    _isOpen = false;
                });
            }
        }


    }
}
