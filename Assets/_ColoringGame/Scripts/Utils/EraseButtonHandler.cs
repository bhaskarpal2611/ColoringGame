using UnityEngine;
using DG.Tweening;
using ColorSwipeGame.UI;

namespace ColorSwipeGame
{
    public class EraseButtonHandler : MonoBehaviour
    {
        [SerializeField] private PensHandler _pensHandler;

        private const float SLIDE_OUT = -50f;
        private const float SLIDE_BACK_IN = 0f;
        private bool _isOpen = false;

        // on click
        public void SelectEraser()
        {
            if (!_isOpen)
            {
                transform.DOLocalMoveX(SLIDE_OUT, 0.25f).OnComplete(() =>
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
                transform.DOLocalMoveX(SLIDE_BACK_IN, 0.25f).OnComplete(() =>
                {
                    _isOpen = false;
                });
            }
        }


    }
}
