using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
{
    /// <summary>
    /// Positions the hand icon for each tutorial step by subscribing to
    /// GenericTutorialManager.OnStepBeganAt. Assign step targets in the Inspector;
    /// the hand snaps to each target before AnimateHand reads restPos.
    /// </summary>
    public class TutorialHandPositioner : MonoBehaviour
    {
        [SerializeField] private RectTransform _handRect;
        [SerializeField] private Camera        _cam;

        [Header("Step Targets")]
        [SerializeField] private Transform _firstDrawingTarget;
        [SerializeField] private Transform _colorScrollTarget;
        [SerializeField] private Transform _colorPenTarget;
        [SerializeField] private Transform _canvasTarget;

        [Tooltip("Canvas-local nudge applied after the hand lands on the scroll viewport. +X = right  -X = left  +Y = up  -Y = down. Edit in the prefab (not Play mode) so it persists.")]
        [SerializeField] private Vector2 _colorScrollOffset = Vector2.zero;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()  => GenericTutorialManager.OnStepBeganAt += PlaceForStep;
        private void OnDisable() => GenericTutorialManager.OnStepBeganAt -= PlaceForStep;

        // ── Step routing ──────────────────────────────────────────────────────

        private void PlaceForStep(int index)
        {
            switch (index)
            {
                case 0: PlaceAtFirstDrawing(); break;
                case 1: PlaceAtColorScroll();  break;
                case 2: PlaceAtColorPen();     break;
                case 3: PlaceAtCanvas();       break;
            }
        }

        // ── Public placement methods (can also be called from Inspector events) ─

        public void PlaceAtFirstDrawing() => Place(_firstDrawingTarget);
        public void PlaceAtCanvas()       => Place(_canvasTarget);

        public void PlaceAtColorScroll()
        {
            if (_colorScrollTarget == null) return;

            // Point at the scroll viewport (the visible window), not the container pivot.
            ScrollRect sr = _colorScrollTarget.GetComponent<ScrollRect>();
            Transform target = (sr != null && sr.viewport != null)
                ? (Transform)sr.viewport
                : _colorScrollTarget;

            Place(target);

            EnsureHandRect();
            if (_handRect != null)
                _handRect.anchoredPosition += _colorScrollOffset;
        }

        /// <summary>
        /// Picks the pen that is currently visible inside the scroll viewport and
        /// closest to its center — avoids pointing at a pen scrolled off-screen.
        /// </summary>
        public void PlaceAtColorPen() => Place(GetVisiblePenNearestViewportCenter());

        // ── Core placement ────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors GenericTutorialManager's ScreenToHandCanvas logic exactly:
        ///   ResolveCameraFor(source) → WorldToScreenPoint → ScreenPointToLocalPointInRectangle.
        /// Works for ScreenSpaceOverlay UI, ScreenSpaceCamera UI, and world/2D-sprite objects.
        /// </summary>
        private void Place(Transform target)
        {
            EnsureHandRect();
            if (_handRect == null || target == null) return;

            RectTransform parentRT = _handRect.parent as RectTransform;
            if (parentRT == null) return;

            Camera fallback = _cam != null ? _cam : Camera.main;

            // Resolve the correct camera for the source object's canvas (null for Overlay).
            Camera sourceCam = ResolveCameraFor(target, fallback);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCam, target.position);

            // Resolve the correct camera for the hand's canvas, then map to local space.
            Camera destCam = ResolveCameraFor(_handRect, fallback);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, screenPos, destCam, out Vector2 local);

            _handRect.anchoredPosition = local;

            // Tell the manager the hand was placed externally so AnimateHand's own
            // TapTarget fallback doesn't recompute (and jump) the position afterwards.
            if (GenericTutorialManager.Instance != null)
                GenericTutorialManager.Instance.HandPlacedExternally = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureHandRect()
        {
            if (_handRect == null && GenericTutorialManager.Instance != null)
                _handRect = GenericTutorialManager.Instance.HandRect;
        }

        private Transform GetVisiblePenNearestViewportCenter()
        {
            if (_colorPenTarget == null || _colorPenTarget.childCount == 0)
                return _colorPenTarget;

            ScrollRect sr       = _colorScrollTarget != null ? _colorScrollTarget.GetComponent<ScrollRect>() : null;
            RectTransform vport = sr != null ? sr.viewport : null;

            bool haveViewport = vport != null;
            Rect viewportRect = default;
            Vector3 centerWorld = _colorPenTarget.position;

            if (haveViewport)
            {
                Vector3[] corners = new Vector3[4];
                vport.GetWorldCorners(corners);
                viewportRect = new Rect(corners[0].x, corners[0].y,
                                        corners[2].x - corners[0].x,
                                        corners[2].y - corners[0].y);
                centerWorld = (corners[0] + corners[2]) * 0.5f;
            }

            Transform best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _colorPenTarget.childCount; i++)
            {
                Transform pen = _colorPenTarget.GetChild(i);
                if (!pen.gameObject.activeInHierarchy) continue;
                if (haveViewport && !viewportRect.Contains(pen.position)) continue;

                float d = Vector3.Distance(pen.position, centerWorld);
                if (d < bestDist) { bestDist = d; best = pen; }
            }

            return best != null ? best : _colorPenTarget.GetChild(0);
        }

        /// <summary>
        /// Returns null for ScreenSpaceOverlay canvases (WorldToScreenPoint must receive null
        /// for Overlay, because the element's world position IS already in screen space).
        /// Returns the canvas worldCamera for ScreenSpaceCamera / WorldSpace canvases.
        /// Returns the fallback camera when the target has no Canvas ancestor (world/sprite).
        /// </summary>
        private static Camera ResolveCameraFor(Transform t, Camera fallback)
        {
            Canvas canvas = t.GetComponentInParent<Canvas>();
            if (canvas == null) return fallback;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}
