using UnityEngine;

namespace DrawingGame
{
    /// <summary>
    /// Attach to any 2D world-space object to auto-adjust position and scale on all devices.
    ///
    /// SETUP (one time per object):
    ///   1. Position/scale the object how you want on your reference device.
    ///   2. Set _referenceOrthoSize to match CaptureCameraResizer's phone size (e.g. 2).
    ///   3. Right-click component header → "Capture Reference Position".
    ///   4. Done — works on all devices automatically.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class WorldObjectAnchor : MonoBehaviour
    {
        [Tooltip("Must match the phone orthographicSize in CaptureCameraResizer (e.g. 2).")]
        [SerializeField] private float _referenceOrthoSize = 2f;

        [Tooltip("Auto-filled by Capture. Object's position expressed as a camera viewport point (0-1, aspect-independent).")]
        [SerializeField] private Vector2 _viewportPosition;

        [Tooltip("Auto-filled by Capture. Object's distance from the camera along its forward axis.")]
        [SerializeField] private float _distanceFromCamera;

        [Tooltip("Auto-filled by Capture. Local scale at the reference ortho size.")]
        [SerializeField] private Vector3 _referenceScale;

        [Tooltip("Scale the object proportionally so it keeps the same visual size on all screens.")]
        [SerializeField] private bool _adjustScale = true;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Re-apply every frame in the Editor (Play mode) so field tweaks in the
            // Inspector are visible immediately without stopping/restarting Play mode.
            Apply();
#endif
        }

        private void Apply()
        {
            Camera cam = Camera.main;
            if (cam == null || _referenceOrthoSize <= 0f) return;

            // ViewportToWorldPoint correctly accounts for both orthographic size and
            // current screen aspect ratio, so the anchor lands in the same relative
            // spot on the screen on every device.
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(_viewportPosition.x, _viewportPosition.y, _distanceFromCamera));
            world.z = transform.position.z;
            transform.position = world;

            if (_adjustScale && _referenceScale != Vector3.zero)
            {
                float scaleFactor = cam.orthographicSize / _referenceOrthoSize;
                transform.localScale = _referenceScale * scaleFactor;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Capture Reference Position")]
        private void CaptureReferencePosition()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[WorldObjectAnchor] No Camera.main found in the scene.");
                return;
            }

            if (_referenceOrthoSize <= 0f)
            {
                Debug.LogError("[WorldObjectAnchor] Set _referenceOrthoSize first (match CaptureCameraResizer phone size).");
                return;
            }

            Vector3 viewport = cam.WorldToViewportPoint(transform.position);
            _viewportPosition   = new Vector2(viewport.x, viewport.y);
            _distanceFromCamera = viewport.z;
            _referenceScale     = transform.localScale;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[WorldObjectAnchor] '{gameObject.name}' captured — viewport:{_viewportPosition} dist:{_distanceFromCamera:F3} refScale:{_referenceScale}");
        }
#endif
    }
}
