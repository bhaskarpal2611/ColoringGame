using UnityEngine;
using DG.Tweening;
using DrawingGame.UI;

namespace DrawingGame
{
    public class EraseButtonHandler : MonoBehaviour
    {
        [SerializeField] private PensHandler _pensHandler;
        [SerializeField] private RectTransform _button;
        [SerializeField] private GameObject _eraserHL;

        private const float SELECTED_SCALE = 1.08f;
        private const float NORMAL_SCALE   = 1f;
        private const float TWEEN_DURATION = 0.25f;

        private bool _isSelected = false;

        private void OnEnable()  => UI_PencilItem.OnAnyPenSelected += UnselectEraser;
        private void OnDisable() => UI_PencilItem.OnAnyPenSelected -= UnselectEraser;

        public void SelectEraser()
        {
            // Always re-apply scale: ButtonClickEffectHandler resets it to 1f on PointerUp
            // before this onClick fires, so a re-click would leave it at 1f without this.
            _button.DOScale(SELECTED_SCALE, TWEEN_DURATION).SetEase(Ease.OutBack);

            if (_isSelected) return;

            _isSelected = true;
            if (_eraserHL != null) _eraserHL.SetActive(true);
            _pensHandler.UnselectAll();
        }

        public void UnselectEraser()
        {
            if (!_isSelected) return;

            _isSelected = false;
            _button.DOScale(NORMAL_SCALE, TWEEN_DURATION).SetEase(Ease.OutQuad);
            if (_eraserHL != null) _eraserHL.SetActive(false);
        }
    }
}
