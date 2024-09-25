using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace ColorSwipeGame
{
    public class LeftPanelController : MonoBehaviour, IPointerClickHandler
    {

        [SerializeField] private RectTransform _rectTransform;

        private bool _isPanelOpen;

        private void TogglePanelOpen() => _isPanelOpen = !_isPanelOpen;


        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (!_isPanelOpen)
            {
                OpenSidePanel();
            } else
            {
                CloseSidePanel();
            }
        }


        private const float XPOS_MAX = 0f;
        private const float XPOS_MIN = 750f;

        public void OpenSidePanel() => _rectTransform.DOAnchorPosX(XPOS_MAX, 0.5f).OnStart(TogglePanelOpen);

        public void CloseSidePanel() => _rectTransform.DOAnchorPosX(XPOS_MIN, 0.5f).OnStart(TogglePanelOpen);
    }
}
