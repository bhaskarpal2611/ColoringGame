using UnityEngine;

namespace DrawingGame
{
    /// <summary>
    /// Explicit per-device-bucket position/scale override. Two buckets only (phone/tablet),
    /// each tuned by eye — no formula, so fixing one device never breaks the other.
    /// </summary>
    public class DeviceBucketSetter : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _aspectRatioThreshold = 1.4f; // below this = tablet (e.g. iPad 4:3)

        [Header("Phone (aspect >= threshold)")]
        [SerializeField] private Vector3 _phonePosition;
        [SerializeField] private Vector3 _phoneScale = Vector3.one;

        [Header("Tablet (aspect < threshold)")]
        [SerializeField] private Vector3 _tabletPosition;
        [SerializeField] private Vector3 _tabletScale = Vector3.one;

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
#if UNITY_EDITOR
            Apply();
#endif
        }

        private void Apply()
        {
            if (_target == null) return;

            float aspectRatio = (float)Screen.width / Screen.height;
            bool isTablet = aspectRatio < _aspectRatioThreshold;

            _target.localPosition = isTablet ? _tabletPosition : _phonePosition;
            _target.localScale = isTablet ? _tabletScale : _phoneScale;
        }

#if UNITY_EDITOR
        [ContextMenu("Capture Current As Phone Values")]
        private void CapturePhone()
        {
            if (_target == null) return;
            _phonePosition = _target.localPosition;
            _phoneScale = _target.localScale;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Capture Current As Tablet Values")]
        private void CaptureTablet()
        {
            if (_target == null) return;
            _tabletPosition = _target.localPosition;
            _tabletScale = _target.localScale;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
