using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class UI_PencilItem : MonoBehaviour
    {
        [SerializeField] Image[] _pencilPieces;
        [SerializeField] private Color m_pencilColor;

        private Button _button;

        public Button Button { get { return _button; } }

        private float YPOS_SELECTED_PEN = -50f;

        private RectTransform _rectTransform;

        public static event Action<Color> OnPenSelected;

        private void Start()
        {
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();
            SetColorOnPencil(m_pencilColor);

            YPOS_SELECTED_PEN = _rectTransform.localPosition.y;
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnEnable()
        {
            UI_PencilItem.OnPenSelected += CheckSelection;
        }

        private void OnButtonClick()
        {
            OnPenSelected?.Invoke(m_pencilColor);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        private void CheckSelection(Color color)
        {
            if (color == m_pencilColor)
            {
                PenSelected();
            }
            else
            {
                UnselectedPen();
            }
        }


        public void SetColorOnPencil(Color color)
        {
            for (int i = 0; i < _pencilPieces.Length; i++)
            {
                _pencilPieces[i].color = color;
            }
        }
        public void PenSelected() => _rectTransform.DOAnchorPosY(-1.5f, 0.5f);

        public void UnselectedPen() => _rectTransform.DOAnchorPosY(-2.5f, 0.5f);
    }
}
