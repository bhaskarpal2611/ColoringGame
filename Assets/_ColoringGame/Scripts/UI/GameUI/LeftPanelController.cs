using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace ColorSwipeGame
{
    public class LeftPanelController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ExpandButton _hintHelper;

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

        private const float XPOS_MAX = 100f;
        private const float XPOS_MIN = -285f;

        public void OpenSidePanel()
        {
            _rectTransform.DOAnchorPosY(XPOS_MAX, _sliderTime);
            TogglePanelOpen();
        }

        public void CloseSidePanel()
        {
            _rectTransform.DOAnchorPosY(XPOS_MIN, _sliderTime);
            TogglePanelOpen();
            _hintHelper.ForceCloseWindow(0.25f);
        }

        // opening panel and closing at start of level load
        public void ShowPanelAtStart()
        {
            _rectTransform.DOAnchorPosY(XPOS_MAX, _sliderTime).SetDelay(_startingDelayTime).OnComplete(() =>
            {
                _rectTransform.DOAnchorPosY (XPOS_MIN, _sliderTime).SetDelay(_startingDelayTime);
            });
        }
    }
}
