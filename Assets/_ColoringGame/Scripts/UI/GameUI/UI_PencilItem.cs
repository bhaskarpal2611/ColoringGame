using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
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

        [Header("Movement Settings")]
        [Tooltip("Choose whether this pen moves along X or Y axis when selected/unselected.")]
        [SerializeField] private MoveAxis moveAxis = MoveAxis.X;

        [Tooltip("Anchored position value when the pen is selected.")]
        [SerializeField] private float selectedPosition = 225f;

        [Tooltip("Anchored position value when the pen is unselected.")]
        [SerializeField] private float unselectedPosition = 275f;

        [Tooltip("Tween duration for the pen movement.")]
        [SerializeField] private float tweenDuration = 0.5f;

        public Button Button => _button;

        public static event Action<Color> OnPenSelected;

        private void OnEnable()
        {
            OnPenSelected += CheckSelection;
        }

        private void Start()
        {
            SetColorOnPencil(m_pencilColor);
            Button.onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            OnPenSelected?.Invoke(m_pencilColor);
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
            MoveToPosition(selectedPosition);
        }

        public void UnselectedPen()
        {
            MoveToPosition(unselectedPosition);
        }

        private void MoveToPosition(float target)
        {
            if (moveAxis == MoveAxis.X)
                _rectTransform.DOAnchorPosX(target, tweenDuration);
            else
                _rectTransform.DOAnchorPosY(target, tweenDuration);
        }
    }
}
