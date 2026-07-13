using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    /// <summary>
    /// GenericTutorialManager animates the hand relative to wherever it already is —
    /// it doesn't know where each step's target sits on screen. Wire one of these
    /// parameterless methods to a step's onStepBegin so the hand jumps to the right
    /// spot before it starts sweeping.
    /// </summary>
    public class TutorialHandPositioner : MonoBehaviour
    {
        [SerializeField] private RectTransform _handRect;
        [SerializeField] private Camera _cam;

        private void OnEnable()
        {
            GenericTutorialManager.OnStepBeganAt += PlaceForStep;
        }

        private void OnDisable()
        {
            GenericTutorialManager.OnStepBeganAt -= PlaceForStep;
        }

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

        [Header("Step Targets — Transform works for both UI (RectTransform) and world sprites")]
        [SerializeField] private Transform _firstDrawingTarget;
        [SerializeField] private Transform _colorScrollTarget;
        [SerializeField] private Transform _colorPenTarget;
        [SerializeField] private Transform _canvasTarget;

        [Tooltip("Nudges the hand left/right after landing on the scroll target — a fixed correction for the panel's resting-vs-tween-timing drift, tune by eye.")]
        [SerializeField] private float _colorScrollOffsetX = -60f;

        public void PlaceAtFirstDrawing() => Place(_firstDrawingTarget);
        public void PlaceAtCanvas() => Place(_canvasTarget);

        public void PlaceAtColorScroll()
        {
            Place(_colorScrollTarget);
            _handRect.anchoredPosition += new Vector2(_colorScrollOffsetX, 0f);
        }

        /// <summary>
        /// Colored pens are spawned at runtime by PensHandler — _colorPenTarget is just their
        /// parent container. The first pen (index 0) can easily be scrolled out of the
        /// viewport (guaranteed off-screen during the idle loop, which starts mid-scroll-list
        /// rather than at the top), so instead pick whichever pen is both currently visible
        /// inside the scroll viewport and closest to its center.
        /// </summary>
        public void PlaceAtColorPen() => Place(GetVisiblePenNearestViewportCenter());

        private Transform GetVisiblePenNearestViewportCenter()
        {
            if (_colorPenTarget == null || _colorPenTarget.childCount == 0) return _colorPenTarget;

            ScrollRect scrollRect = _colorScrollTarget != null ? _colorScrollTarget.GetComponent<ScrollRect>() : null;
            RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;

            bool haveViewport = viewport != null;
            Rect viewportWorldRect = default;
            Vector3 centerWorld = _colorPenTarget.position;

            if (haveViewport)
            {
                Vector3[] corners = new Vector3[4];
                viewport.GetWorldCorners(corners); // 0=bottom-left, 2=top-right
                viewportWorldRect = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
                centerWorld = (corners[0] + corners[2]) * 0.5f;
            }

            Transform best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _colorPenTarget.childCount; i++)
            {
                Transform pen = _colorPenTarget.GetChild(i);
                if (!pen.gameObject.activeInHierarchy) continue;

                if (haveViewport && !viewportWorldRect.Contains(pen.position))
                    continue; // scrolled out of view — never point the hand at it

                float dist = Vector3.Distance(pen.position, centerWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = pen;
                }
            }

            // Nothing currently visible (e.g. mid-scroll-tween) — fall back to the first pen
            // rather than pointing at nothing.
            return best != null ? best : _colorPenTarget.GetChild(0);
        }

        private void Place(Transform target)
        {
            if (_handRect == null)
                _handRect = GenericTutorialManager.Instance != null ? GenericTutorialManager.Instance.HandRect : null;
            if (_handRect == null || target == null) return;

            RectTransform parentRT = _handRect.parent as RectTransform;
            if (parentRT == null) return;

            // A target under a Screen-Space-Overlay canvas has its RectTransform.position
            // already IN screen space — running that through camera projection math (as if
            // it were a real world/Screen-Space-Camera position) produces huge garbage values.
            // Resolve the correct camera per-target, not just for the hand's own canvas.
            Camera fallback = _cam != null ? _cam : Camera.main;
            Camera sourceCam = ResolveCameraFor(target, fallback);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCam, target.position);

            Camera destCam = ResolveCameraFor(_handRect, fallback);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenPos, destCam, out Vector2 local);
            _handRect.anchoredPosition = local;
        }

        /// <summary>
        /// null for Screen-Space-Overlay (its RectTransform.position IS screen space already),
        /// the canvas's own worldCamera for Screen-Space-Camera, or the fallback camera for a
        /// plain world-space object (no Canvas ancestor at all).
        /// </summary>
        private static Camera ResolveCameraFor(Transform t, Camera fallback)
        {
            Canvas canvas = t.GetComponentInParent<Canvas>();
            if (canvas == null) return fallback;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}
