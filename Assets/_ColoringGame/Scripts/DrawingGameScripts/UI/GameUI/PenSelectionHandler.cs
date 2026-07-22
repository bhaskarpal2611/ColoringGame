using DG.Tweening;
using UnityEngine;

namespace DrawingGame
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

        private readonly float LEFT_POS = 150f;
        private readonly float RIGHT_POS = 450f;

        // switch from color to tex pen
        public void SelectButton(int index)
        {

        }

        private void ShowMainPanel()
        {
            _mainPanel.DOAnchorPosX(LEFT_POS, _mainPanelSlideTime);
        }
        public void HideMainPanel(float waitTime)
        {
            //_mainPanel.DOAnchorPosX(RIGHT_POS, _mainPanelSlideTime).SetDelay(waitTime);
        }

        public void ShowPanelAtStart()
        {
            _mainPanel.DOAnchorPosX(LEFT_POS, _mainPanelSlideTime).OnComplete(() =>
            {
                _mainPanel.DOAnchorPosX(RIGHT_POS, _mainPanelSlideTime).SetDelay(1f);
            });
        }
    }
}
