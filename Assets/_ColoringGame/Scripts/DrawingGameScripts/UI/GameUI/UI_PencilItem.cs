using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
{
    public enum MoveAxis
    {
        X, Y
    }
    public class UI_PencilItem : MonoBehaviour
    {
        [Header("Pencil Settings")]
        [SerializeField] private Image[] _pencilPieces;
        [SerializeField] private Color m_pencilColor;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Button _button;

        [Header("Selection Visuals")]
        [Tooltip("Assign a child Image that acts as the white outline ring. Show/hide on select.")]
        [SerializeField] private Image _outlineImage;

        [Tooltip("Scale of the item when selected (does not affect grid layout).")]
        [SerializeField] private float _selectedScale = 1.15f;

        [Tooltip("Tween duration for the scale animation.")]
        [SerializeField] private float tweenDuration = 0.25f;

        public Button Button => _button;

        public static event Action<Color> OnPenSelected;
        public static event Action OnAnyPenSelected;

        private void OnEnable()
        {
            OnPenSelected += CheckSelection;
        }

        private void Start()
        {
            SetColorOnPencil(m_pencilColor);
            Button.onClick.AddListener(OnButtonClick);
            SetOutlineVisible(false);
        }

        private void OnButtonClick()
        {
            OnPenSelected?.Invoke(m_pencilColor);
            OnAnyPenSelected?.Invoke();
            PenSelected();
        }

        private void OnDestroy()
        {
            Button.onClick.RemoveAllListeners();
            OnPenSelected -= CheckSelection;
        }

        private void CheckSelection(Color color)
        {
            if (color != m_pencilColor)
                UnselectedPen();
        }

        public void SetColorOnPencil(Color color)
        {
            foreach (var piece in _pencilPieces)
                piece.color = color;

            m_pencilColor = color;
        }

        public void PenSelected()
        {
            SetOutlineVisible(true);
            _rectTransform.DOScale(_selectedScale, tweenDuration).SetEase(Ease.OutBack);
        }

        public void UnselectedPen()
        {
            SetOutlineVisible(false);
            _rectTransform.DOScale(1f, tweenDuration).SetEase(Ease.OutQuad);
        }

        private void SetOutlineVisible(bool visible)
        {
            if (_outlineImage != null)
                _outlineImage.gameObject.SetActive(visible);
        }
    }
}
