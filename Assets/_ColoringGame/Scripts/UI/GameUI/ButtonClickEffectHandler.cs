using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace ColorSwipeGame.UI
{
    public class ButtonClickEffectHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private bool _PopOnTouch = true;
        private Button _button;
        private readonly Vector2 _largeScale = 1.1f * Vector2.one;

        private PaintService _paintService;

        private void Start()
        {
            _button = GetComponent<Button>();
            if (!_button) Debug.LogError("Button is not at script" + gameObject.name);

            _paintService = FindObjectOfType<PaintService>();
        }

        private void OnTouchStart()
        {
            _paintService.CanPaint = false;
            if (_PopOnTouch)
                _button.transform.DOScale(_largeScale, 0.25f);
        }

        private void OnTouchEnd()
        {
            _paintService.CanPaint = true;
            if (_PopOnTouch)
                _button.transform.DOScale(1f, 0.25f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnTouchStart();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnTouchEnd();
        }
    }
}