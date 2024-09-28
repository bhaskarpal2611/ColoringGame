using DG.Tweening;
using UnityEngine;

namespace ColorSwipeGame
{
    public class PenSelectionHandler : MonoBehaviour
    {
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private float _mainPanelSlideTime = 0.25f;
        [SerializeField] private Transform _swapButton;

        public void SwapButton()
        {
            foreach (Transform tf in _swapButton)
            {
                if (tf.gameObject.activeSelf) tf.gameObject.SetActive(false);
                else tf.gameObject.SetActive(true);
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
