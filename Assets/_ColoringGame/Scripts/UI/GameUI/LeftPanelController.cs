using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace ColorSwipeGame
{
    public class LeftPanelController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private float _sliderTime = 0.25f;
        [SerializeField] private float _startingDelayTime = 0.75f;
        private bool _isPanelOpen = false;

        private void TogglePanelOpen() => _isPanelOpen = !_isPanelOpen;

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (!_isPanelOpen) OpenSidePanel();
            else CloseSidePanel();
        }

        private const float XPOS_MAX = -50f;
        private const float XPOS_MIN = -450f;

        public void OpenSidePanel()
        {
            _rectTransform.DOAnchorPosX(XPOS_MAX, _sliderTime);
            TogglePanelOpen();
        }

        public void CloseSidePanel()
        {
            _rectTransform.DOAnchorPosX(XPOS_MIN, _sliderTime);
            TogglePanelOpen();
        }


        // opening panel and closing at start of level load
        public void ShowPanelAtStart()
        {
            _rectTransform.DOAnchorPosX(XPOS_MAX, _sliderTime).SetDelay(_startingDelayTime).OnComplete(() =>
            {
                _rectTransform.DOAnchorPosX(XPOS_MIN, _sliderTime).SetDelay(_startingDelayTime);
            });
        }
    }
}
