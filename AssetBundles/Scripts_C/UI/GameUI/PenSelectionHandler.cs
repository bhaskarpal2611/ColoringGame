using ColorSwipeGame.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class PenSelectionHandler : MonoBehaviour
    {
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PensHandler _pensHandler;
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private float _mainPanelSlideTime = 0.25f;
        [SerializeField] private Transform _swapButton;
        [SerializeField] private Transform _colorButton;
        [SerializeField] private Transform _textureButton;
        [SerializeField] private Image _colorButtonImage;
        [SerializeField] private Image _textureButtonImage;

        private static readonly Color ActiveTint   = new Color(0.65f, 0.65f, 0.65f, 1f);
        private static readonly Color InactiveTint = Color.white;

        private bool _colorSelected = true;

        private void Start()
        {
            // Reflect initial state
            SetButtonTints(_colorSelected);
        }

        public void SwapButton()
        {
            foreach (Transform tf in _swapButton)
            {
                if (tf.gameObject.activeSelf) tf.gameObject.SetActive(false);
                else tf.gameObject.SetActive(true);
            }
        }

        public void SelectColorButton()
        {
            if (!_colorSelected)
            {
                _paintService.SetDefaultColorMode();
                _colorButton.DOScale(1.1f, 0.5f);
                _textureButton.DOScale(1f, 0.5f);
                _colorSelected = true;
                SetButtonTints(true);
            }
        }

        public void SelectTexture()
        {
            if (_colorSelected)
            {
                _paintService.SetDefaultTextureMode();
                _colorButton.DOScale(1f, 0.5f);
                _textureButton.DOScale(1.1f, 0.5f);
                _colorSelected = false;
                SetButtonTints(false);
            }
        }

        public void SetButtonTints(bool colorActive)
        {
            if (_colorButtonImage   != null) _colorButtonImage.color   = colorActive  ? ActiveTint : InactiveTint;
            if (_textureButtonImage != null) _textureButtonImage.color = !colorActive ? ActiveTint : InactiveTint;
        }

        [SerializeField] private float _visiblePosX = 150f;
        [SerializeField] private float _hiddenPosX  = 450f;

        public void SelectButton(int index) { }

        public void ShowMainPanel()
        {
            _mainPanel.DOAnchorPosX(_visiblePosX, _mainPanelSlideTime);
        }

        public void HideMainPanel(float waitTime) { }

        public void ShowPanelAtStart()
        {
            // Snap to hidden first so the slide-in always plays from a consistent position
            _mainPanel.anchoredPosition = new Vector2(_hiddenPosX, _mainPanel.anchoredPosition.y);
            _mainPanel.DOAnchorPosX(_visiblePosX, _mainPanelSlideTime);

            // Always reset to color mode and auto-select the first color
            _colorSelected = true;
            SetButtonTints(true);
            _pensHandler?.AutoSelectFirstColor();
        }
    }
}
