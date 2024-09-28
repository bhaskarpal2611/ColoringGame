using UnityEngine;
using DG.Tweening;

namespace ColorSwipeGame
{
    public class EraseButtonHandler : MonoBehaviour
    {
        private const float SLIDE_OUT = -50f;
        private const float SLIDE_BACK_IN = 0f;
        private bool _isOpen = false;

        // on click
        public void HandleClick()
        {
            if (!_isOpen)
            {
                transform.DOLocalMoveX(SLIDE_OUT, 0.25f);
                _isOpen = true;
            } else
            {
                transform.DOLocalMoveX(SLIDE_BACK_IN, 0.25f);
                _isOpen = false;
            }
        }
    
        
    }
}
