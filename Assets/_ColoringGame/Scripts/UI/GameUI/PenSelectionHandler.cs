using DG.Tweening;
using UnityEngine;

namespace ColorSwipeGame
{
    public class PenSelectionHandler : MonoBehaviour
    {
        [SerializeField] private PaintService _paintService;
        [SerializeField] private RectTransform _mainPanel;  
        [SerializeField] private float _mainPanelSlideTime = 0.25f;
        [SerializeField] private Transform _swapButton;
        [SerializeField] private Transform _colorButton;
        [SerializeField] private Transform _textureButton;

        private bool _colorSelected = true;

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
            }
        }

        private readonly float VISIBLE_POS = 150f;
        private readonly float HIDDEN_POS  = 450f;

        public void SelectButton(int index) { }

        public void ShowMainPanel()
        {
            _mainPanel.DOAnchorPosX(VISIBLE_POS, _mainPanelSlideTime);
        }

        public void HideMainPanel(float waitTime) { }

        public void ShowPanelAtStart()
        {
            // Snap to hidden first so the slide-in always plays from a consistent position
            _mainPanel.anchoredPosition = new Vector2(HIDDEN_POS, _mainPanel.anchoredPosition.y);
            _mainPanel.DOAnchorPosX(VISIBLE_POS, _mainPanelSlideTime);
        }
    }
}
