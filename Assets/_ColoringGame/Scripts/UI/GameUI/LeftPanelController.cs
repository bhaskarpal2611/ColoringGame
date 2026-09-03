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

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (!_isPanelOpen) OpenSidePanel();
            else CloseSidePanel();
        }

        // now Y-Pos
        private const float YPOS_MAX = 50f;
        private const float YPOS_MIN = -240f;

        public void OpenSidePanel()
        {
            _rectTransform.DOAnchorPosY(YPOS_MAX, _sliderTime);
            _isPanelOpen = true;
        }

        public void CloseSidePanel()
        {
            _rectTransform.DOAnchorPosY(YPOS_MIN, _sliderTime);
            _isPanelOpen = false;
            _hintHelper.ForceCloseWindow(0.25f);
        }

        public void CompleteHidePanel()
        {
            _rectTransform.DOAnchorPosY(YPOS_MIN - 100f, _sliderTime);
            _isPanelOpen = false;
            _hintHelper.ForceCloseWindow(0.25f);
        }

        // opening panel and closing at start of level load
        public void ShowPanelAtStart()
        {
            _rectTransform.DOAnchorPosY(YPOS_MAX, _sliderTime).SetDelay(_startingDelayTime).OnComplete(() =>
            {
                _rectTransform.DOAnchorPosY (YPOS_MIN, _sliderTime).SetDelay(_startingDelayTime);
            });
        }
    }
}
