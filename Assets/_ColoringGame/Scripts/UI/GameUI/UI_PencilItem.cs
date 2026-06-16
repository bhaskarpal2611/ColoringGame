using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class UI_PencilItem : MonoBehaviour
    {
        [Header("Color Circle Settings")]
        [SerializeField] private Image _colorImage;
        [SerializeField] private Image _selectionRing;
        [SerializeField] private Color m_pencilColor;
        [SerializeField] private Button _button;

        [Header("Selection Animation")]
        [SerializeField] private float selectedScale = 1.2f;
        [SerializeField] private float unselectedScale = 1.0f;
        [SerializeField] private float tweenDuration = 0.2f;

        public Button Button => _button;

        public static event Action<Color> OnPenSelected;

        private void Awake()
        {
            // Auto-grab references if not assigned in Inspector
            if (_colorImage == null)
                _colorImage = GetComponent<Image>();
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            OnPenSelected += CheckSelection;
        }

        private void Start()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClick);

            if (_selectionRing != null)
                _selectionRing.enabled = false;

            SetColorOnPencil(m_pencilColor);
        }

        private void OnButtonClick()
        {
            OnPenSelected?.Invoke(m_pencilColor);
            PenSelected();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveAllListeners();
            OnPenSelected -= CheckSelection;
        }

        private void CheckSelection(Color color)
        {
            if (color != m_pencilColor)
                UnselectedPen();
        }

        public void SetColorOnPencil(Color color)
        {
            color.a = 1f;
            m_pencilColor = color;

            if (_colorImage != null)
                _colorImage.color = color;
        }

        public void PenSelected()
        {
            transform.DOScale(selectedScale, tweenDuration).SetEase(Ease.OutBack);
            if (_selectionRing != null)
                _selectionRing.enabled = true;
        }

        public void UnselectedPen()
        {
            transform.DOScale(unselectedScale, tweenDuration).SetEase(Ease.InOutSine);
            if (_selectionRing != null)
                _selectionRing.enabled = false;
        }
    }
}
