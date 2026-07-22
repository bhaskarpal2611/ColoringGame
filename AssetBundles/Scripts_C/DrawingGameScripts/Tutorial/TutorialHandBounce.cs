using UnityEngine;

namespace DrawingGame
{
    /// <summary>
    /// Pulses the hand icon's scale while it's active — a little "tap" bounce feel.
    /// Only plays for Tap-input steps (a swipe/drag hand sweeping across the screen
    /// shouldn't also be pulsing). Only touches localScale, so it layers safely on top
    /// of GenericTutorialManager's own position animation (which never touches scale).
    /// </summary>
    public class TutorialHandBounce : MonoBehaviour
    {
        [SerializeField] private GenericTutorialManager _tutorialManager;
        [SerializeField] private float _scaleAmount = 0.18f;
        [SerializeField] private float _speed = 3f;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            bool isTap = _tutorialManager != null &&
                (_tutorialManager.CurrentInputType == TutorialInputType.Tap ||
                 _tutorialManager.CurrentInputType == TutorialInputType.TapTarget);
            if (!isTap)
            {
                transform.localScale = _baseScale;
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _speed * Mathf.PI * 2f);
            transform.localScale = _baseScale * (1f + _scaleAmount * pulse);
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }
    }
}
