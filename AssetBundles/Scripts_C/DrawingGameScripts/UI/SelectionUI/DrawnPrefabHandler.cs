using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
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
            _drawnImage.type = Image.Type.Simple;
            _drawnImage.preserveAspect = false;

            // Force the image rect to stretch-fill the mask (anchors 0,0 → 1,1, zero offsets)
            RectTransform rt = _drawnImage.rectTransform;
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
        }

        public void MovePosition(Vector2 position) => _rectTransform.anchoredPosition = position;

        public void SetPin(Sprite sprite) => _pin.sprite = sprite;
    }
}
