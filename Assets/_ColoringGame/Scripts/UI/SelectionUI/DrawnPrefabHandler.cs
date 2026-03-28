using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class DrawnPrefabHandler : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _drawnImage;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _pin;

        public Button Button { get { return _button; } }

        public void SetImage(Sprite sprite)
        {
            _drawnImage.sprite = sprite;
        }

        public void MovePosition(Vector2 position) => _rectTransform.anchoredPosition = position;

        public void SetPin(Sprite sprite) => _pin.sprite = sprite;
    }
}
