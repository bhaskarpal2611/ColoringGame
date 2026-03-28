using UnityEngine;
using DG.Tweening;

namespace ColorSwipeGame
{
    public class ExpandButton : MonoBehaviour
    {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _targetScale = 1.0f;

        private bool _isExpanded = false;
        public void PopOpen(float duration = 0.25f)
        {
            if (!_isExpanded)
            {
                // Animate the scale to the target scale value
                _targetTransform.DOScale(_targetScale, duration).SetEase(Ease.OutBack);
                _isExpanded = true;
            }
            else
            {
                _targetTransform.DOScale(0f, duration).SetEase(Ease.OutSine);
                _isExpanded = false;
            }
        }

        public void ForceCloseWindow(float duration = 0.25f)
        {
                _targetTransform.DOScale(0f, duration).SetEase(Ease.OutSine);
                _isExpanded = false;
        }
    }
}
