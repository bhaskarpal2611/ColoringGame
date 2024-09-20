using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace ColorSwipeGame.UI
{
    public class UI_PencilColorGenerator : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;
        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_PencilItem _texturedPencilPrefab;
        [SerializeField] private UI_PencilItem _texturedPencilType2Prefab;
        [SerializeField] private RectTransform _pencilParent;
        public ScrollRect _scrollViewReference;

        [SerializeField] private GPU_SpriteColoring _spriteSelectionHandler;
        [SerializeField] private RectTransform _targetPositionUp;
        [SerializeField] private RectTransform _targetPositionBottom;

        private int _prevIndex;

        private void Start()
        {
            GeneratePencils();
            GenerateTexturedPens();
            GenerateSecondTexturedPens();

            // Default active and color
            _pencilParent.GetChild(0).gameObject.SetActive(true);
            MoveUpPens();
        }

        public void MoveDownPens()
        {
            _pencilParent.DOAnchorPos(_targetPositionBottom.anchoredPosition, 1f).SetEase(Ease.OutQuad);
        }

        public void MoveUpPens()
        {
            _pencilParent.DOAnchorPos(_targetPositionUp.anchoredPosition, 1f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                // can enable pens button if disabled
            });
        }

        public void OnPencilSelection(int index)
        {
            if(_prevIndex == index) return;

            _scrollViewReference.content = _pencilParent.GetChild(index) as RectTransform;

            // move down current pens.
            _pencilParent.DOAnchorPos(_targetPositionBottom.anchoredPosition, 1f).SetEase(Ease.OutQuad).SetDelay(0.25f).OnComplete(() =>
            {
                // set inactive prevIndex item, set active current index one
                _pencilParent.GetChild(_prevIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(index).gameObject.SetActive(true);
                _prevIndex = index;
                MoveUpPens();                
            });
        }

        private void GeneratePencils()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                UI_PencilItem pencil = Instantiate(_pencilPrefab, _pencilParent.GetChild(0));
                Color color = _colors[i];
                color.a = 1f;
                pencil.PickColor(color);
               
                pencil.Button.onClick.AddListener(() =>
                {
                    _spriteSelectionHandler.SetPaintColorMode();
                    _spriteSelectionHandler.CurrentBrushColor = color;
                });
            }
        }
        private void GenerateTexturedPens()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                UI_PencilItem pencil = Instantiate(_texturedPencilPrefab, _pencilParent.GetChild(1));
                Color color = _colors[i];
                color.a = 1f;
                pencil.PickColor(color);
                pencil.Button.onClick.AddListener(() =>
                {
                    _spriteSelectionHandler.SetBrushTexture(0);
                    _spriteSelectionHandler.CurrentBrushColor = color;
                });
            }
        }
        private void GenerateSecondTexturedPens()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                UI_PencilItem pencil = Instantiate(_texturedPencilType2Prefab, _pencilParent.GetChild(2));
                Color color = _colors[i];
                color.a = 1f;
                pencil.PickColor(color);
                pencil.Button.onClick.AddListener(() =>
                {
                    _spriteSelectionHandler.SetBrushTexture(1);
                    _spriteSelectionHandler.CurrentBrushColor = color;
                });
            }
        }

    }
}