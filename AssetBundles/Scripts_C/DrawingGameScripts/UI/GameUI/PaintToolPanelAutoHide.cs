using DG.Tweening;
using UnityEngine;

namespace DrawingGame
{
    /// <summary>
    /// Slides the paint tool panel off-screen while the user is actively drawing on the
    /// canvas, and brings it back as soon as the touch lifts.
    /// </summary>
    public class PaintToolPanelAutoHide : MonoBehaviour
    {
        [SerializeField] private PaintService _paintService;
        [SerializeField] private RectTransform _paintToolPanel;
        [SerializeField] private float _hiddenOffsetX = 1000f;
        [SerializeField] private float _slideDuration = 0.25f;

        private float _shownX;

        private void Awake()
        {
            _shownX = _paintToolPanel.localPosition.x;
        }

        private void OnEnable()
        {
            _paintService.OnPaintTouchBegin += HidePanel;
            _paintService.OnPaintTouchEnd += ShowPanel;
        }

        private void OnDisable()
        {
            _paintService.OnPaintTouchBegin -= HidePanel;
            _paintService.OnPaintTouchEnd -= ShowPanel;
        }

        private void HidePanel()
        {
            _paintToolPanel.DOLocalMoveX(_shownX + _hiddenOffsetX, _slideDuration).SetEase(Ease.InOutQuad);
        }

        private void ShowPanel()
        {
            _paintToolPanel.DOLocalMoveX(_shownX, _slideDuration).SetEase(Ease.InOutQuad);
        }
    }
}
