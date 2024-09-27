using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace ColorSwipeGame.UI
{
    public class PensHandler : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;
        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_TextureItem[] _texturedPencilPrefab;
        [SerializeField] private RectTransform _pencilParent;
        [SerializeField] private ScrollRect _scrollViewReference;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PenSelectionHandler _mainMenuHandler;

        private const float XPOS_LEFT = 900f;
        private const float XPOS_RIGHT = -300f;

        private int _currentSelectedIndex = 0;
        private int _maxLen = 2;
        private UI_PencilItem _currentSelectedPen;
        private SelectedPenData SelectedPenData;

        private void Start()
        {
            // Default active and color
            _pencilParent.GetChild(0).gameObject.SetActive(true);
            SelectedPenData = new SelectedPenData(_colors.Length, _texturedPencilPrefab.Length);

            GeneratePencils();
            GenerateTexturedPens();
            MoveLeft();
        }

        // Dotween movements

        public void MoveRight()
        {
            // Tween to the right (e.g., to 300 on the X-axis in 1 second)
            _pencilParent.DOAnchorPosX(XPOS_LEFT, .75f);
        }

        public void MoveLeft()
        {
            // Tween to the left (e.g., to -300 on the X-axis in 1 second)
            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .25f);
        }

        // Function to scale up (pop open) a UI image or any GameObject
        public void PopOpen(Transform targetTransform, float targetScale = 1.0f, float duration = 0.3f)
        {
            // Start by setting the scale to zero (or any initial scale)
            targetTransform.localScale = Vector3.zero;

            // Animate the scale to the target scale value
            targetTransform.DOScale(targetScale, duration).SetEase(Ease.OutBack);
        }

        public void OnPenCategorySelection()
        {
            _mainMenuHandler.SwapButton();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

            if(_currentSelectedIndex % 2 != 0)
            {
                _paintService.SetDefaultTextureMode();
            }
            else
            {
                _paintService.SetDefaultColorMode();
            }

            _scrollViewReference.content = _pencilParent.GetChild(_currentSelectedIndex) as RectTransform;

            // move right current pens.
            // then move left the new pens selected

            _pencilParent.DOAnchorPosX(XPOS_LEFT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _pencilParent.GetChild(prevIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(true);

                MoveLeft();
            });
        }

        private void GeneratePencils()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                int index = i;
                SelectedPenData.ColoredPens[i] = Instantiate(_pencilPrefab, _pencilParent.GetChild(0));
                Color color = _colors[i];
                color.a = 1f;
                SelectedPenData.ColoredPens[i].SetColorOnPencil(color);
                SelectedPenData.ColoredPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetColor(color);
                    SelectedPenData.ColoredPens[index].OnPenSelected();
                    SelectedPenData.ColoredPenSelection(index);
                });
            }
        }
        private void GenerateTexturedPens()
        {
            for (int i = 0; i < _texturedPencilPrefab.Length; i++)
            {
                int index = i;
                SelectedPenData.TexturedPens[i] = Instantiate(_texturedPencilPrefab[i], _pencilParent.GetChild(1));
                SelectedPenData.TexturedPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetTexture(index);
                    SelectedPenData.TexPenSelection(index);
                });
            }
        }
    }

    [System.Serializable]
    public struct SelectedPenData
    {
        public UI_PencilItem[] ColoredPens;
        public UI_TextureItem[] TexturedPens;

        public int CurrentSelectedColorPen;
        public int CurrentSelectedTexturePen;

        // function to call this pen's OnPenSelected
        // And call Unselected For rest.

        public SelectedPenData(int len1, int len2)
        {
            CurrentSelectedColorPen = -1;
            CurrentSelectedTexturePen = -1;

            ColoredPens = new UI_PencilItem[len1];
            TexturedPens = new UI_TextureItem[len2];
        }

        public void ColoredPenSelection(int index)
        {
            if (CurrentSelectedColorPen == index) return;

            for (int i = 0; i < ColoredPens.Length; i++)
            {
                if (i == index)
                {
                    ColoredPens[i].OnPenSelected();
                    CurrentSelectedColorPen = i;
                }
                else
                {
                    ColoredPens[i].UnselectedPen();
                }
            }
        }
        public void TexPenSelection(int index)
        {
            if (CurrentSelectedTexturePen == index) return;

            for (int i = 0; i < TexturedPens.Length; i++)
            {
                if (i == index)
                {
                    // func call for selected from UI_TextureItem


                    CurrentSelectedTexturePen = i;
                }
                else
                {
                    TexturedPens[i].UnselectedPen();    
                }
            }
        }
    }
}