using DG.Tweening;
using UnityEngine;

namespace ColorSwipeGame
{
    /// <summary>
    /// Slides PaintTool_Parent off-screen only when the player presses on a paintable
    /// area (PolygonCollider2D), and brings it back when they lift their finger.
    /// </summary>
    public class PaintToolSlider : MonoBehaviour
    {
        [SerializeField] private InputHandler  _inputHandler;
        [SerializeField] private RectTransform _paintToolParent;
        [SerializeField] private Camera        _paintCamera;

        [Tooltip("Anchored X position when fully hidden (off-screen). Positive = right, negative = left.")]
        [SerializeField] private float _hiddenX = 300f;
        [SerializeField] private float _slideOutDuration = 0.2f;
        [SerializeField] private float _slideInDuration  = 0.3f;
        [SerializeField] private Ease  _slideOutEase = Ease.InOutQuad;
        [SerializeField] private Ease  _slideInEase  = Ease.OutBack;

        private float _shownX;
        private bool  _isHidden;

        private void Awake()
        {
            if (_paintCamera == null) _paintCamera = Camera.main;
            if (_paintToolParent != null)
                _shownX = _paintToolParent.anchoredPosition.x;
        }

        private void OnEnable()
        {
            if (_inputHandler == null) return;
            _inputHandler.OnBeginDrag += OnBeginDrag;
            _inputHandler.OnDragEnd   += OnDragEnd;
        }

        private void OnDisable()
        {
            if (_inputHandler == null) return;
            _inputHandler.OnBeginDrag -= OnBeginDrag;
            _inputHandler.OnDragEnd   -= OnDragEnd;
        }

        private void OnBeginDrag(Vector2 screenPos)
        {
            if (_isHidden || _paintToolParent == null) return;
            if (!HitsPaintableArea(screenPos)) return;

            _isHidden = true;
            _paintToolParent.DOKill();
            _paintToolParent.DOAnchorPosX(_hiddenX, _slideOutDuration).SetEase(_slideOutEase).SetUpdate(true);
        }

        private void OnDragEnd()
        {
            if (!_isHidden || _paintToolParent == null) return;
            _isHidden = false;
            _paintToolParent.DOKill();
            _paintToolParent.DOAnchorPosX(_shownX, _slideInDuration).SetEase(_slideInEase).SetUpdate(true);
        }

        private bool HitsPaintableArea(Vector2 screenPos)
        {
            if (_paintCamera == null) return false;
            Vector2 worldPos = _paintCamera.ScreenToWorldPoint(screenPos);
            foreach (var hit in Physics2D.RaycastAll(worldPos, Vector2.zero))
            {
                if (hit.collider is PolygonCollider2D)
                    return true;
            }
            return false;
        }
    }
}
