using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ColorSwipeGame
{
    public class SubPanelExtender : MonoBehaviour
    {
        [SerializeField] private RectTransform _extendingPanel;
        [SerializeField] private float YPOS_Open;

        private bool _openToggle = false;

        public void ButtonClick()
        {
            if (_openToggle) ExtendDown();
            else ExtendUp();
        }

        private void ExtendUp()
        {
            _extendingPanel.DOSizeDelta(new Vector2(_extendingPanel.sizeDelta.x, YPOS_Open), 0.25f).SetEase(Ease.OutQuad);
            _openToggle = !_openToggle;
        }

        private void ExtendDown()
        {
            _extendingPanel.DOSizeDelta(new Vector2(_extendingPanel.sizeDelta.x, 0f), 0.25f).SetEase(Ease.OutQuad);
            _openToggle = !_openToggle;
        }
    }
}
