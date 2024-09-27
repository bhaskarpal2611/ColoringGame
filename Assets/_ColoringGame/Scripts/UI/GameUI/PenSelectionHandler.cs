using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class PenSelectionHandler : MonoBehaviour
    {
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private float _buttonPopTime = 0.25f;
        [SerializeField] private float _mainPanelSlideTime = 0.25f;

        private Button[] _buttons;
        private readonly float _SelectedValue = 15f;
        private int _selectedIndex = 0;

        private readonly float LEFT_POS = 150f;
        private readonly float RIGHT_POS = 450f;

        private void Start()
        {
            int i = 0;
            _buttons = new Button[transform.childCount];
            foreach (Transform child in transform)
            {
                _buttons[i] = child.GetChild(0).GetComponent<Button>();
                i++;
            }
        }
        public void SelectButton(int index)
        {
            ShowMainPanel();

            if (index == _selectedIndex) return;
            int i = 0;
            foreach (Button button in _buttons)
            {
                if (i == index)
                {
                    _selectedIndex = i;
                    button.transform.DOLocalMoveX(-_SelectedValue, _buttonPopTime);
                    button.transform.DOScale(1.1f, _buttonPopTime).SetEase(Ease.Linear);
                }
                else
                {
                    button.transform.DOLocalMoveX(_SelectedValue, 0.5f);
                    button.transform.DOScale(1f, _buttonPopTime).SetEase(Ease.Linear);
                }
                i++;
            }
        }

        private void ShowMainPanel()
        {
            _mainPanel.DOAnchorPosX(LEFT_POS, _mainPanelSlideTime);
        }
        public void HideMainPanel(float waitTime)  
        {
            _mainPanel.DOAnchorPosX(RIGHT_POS, _mainPanelSlideTime).SetDelay(waitTime);
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
