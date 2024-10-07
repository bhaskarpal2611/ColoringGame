using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace ColorSwipeGame.UI
{
    public class ButtonClickEffectHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Button _button;
        private readonly Vector2 _largeScale = 1.1f * Vector2.one;

        private void Start()
        {
            _button = GetComponent<Button>();
            if (!_button) Debug.LogError("Button is not at script" + gameObject.name);
        }

        private void OnTouchStart()
        {
            _button.transform.DOScale(_largeScale, 0.25f);
        }

        private void OnTouchEnd()
        {
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