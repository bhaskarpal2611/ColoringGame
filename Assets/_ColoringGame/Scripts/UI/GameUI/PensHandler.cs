using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ColorSwipeGame.UI
{
    public class PensHandler : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;
        [SerializeField] private int numberOfColors = 64;

        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_TextureItem[] _texturedPencilPrefab;
        [SerializeField] private RectTransform _pencilParent;
        [SerializeField] private ScrollRect _scrollViewReference;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PenSelectionHandler _penSelectionHandler;
        [SerializeField] private EraseButtonHandler _eraseButtonHandler;

        private const float XPOS_LEFT = 0f;
        private const float XPOS_RIGHT = -360f;
        private const int _maxLen = 2; // pen types
        // Golden ratio conjugate for hue generation
        private const float goldenRatioConjugate = 0.61803398875f;

        private List<Color> Colors = new();
        private int _currentSelectedIndex = 0;
        private SelectedPenData SelectedPenData;

        private void Start()
        {
            // Default active and color
            _pencilParent.GetChild(0).gameObject.SetActive(true);


            // gen colors then
            Colors = GenerateGoldenRatioColors();
            SelectedPenData = new SelectedPenData(Colors.Count, _texturedPencilPrefab.Length);

            GeneratePencils();
            GenerateTexturedPens();
            MoveLeft();
        }

        // Returns a list of evenly spaced colors using the golden ratio for hue generation
        public List<Color> GenerateGoldenRatioColors()
        {
            List<Color> colors = new List<Color>();
            float hue = 0;  // Start with a random hue value

            for (int i = 0; i < numberOfColors; i++)
            {
                // Generate hue by adding the golden ratio conjugate and taking modulo 1
                hue = (hue + goldenRatioConjugate) % 1f;

                // Convert HSV to RGB (Saturation and Value are fixed at 1 for vivid colors)
                Color color = Color.HSVToRGB(hue, 1f, 1f);
                colors.Add(color);
            }

            return colors;
        }

        private bool IsColorImportant(Color color)
        {
            // Ignore colors that are too dark or too bright
            float brightness = (color.r + color.g + color.b) / 3f;
            return brightness > 0.1f && brightness < 0.9f;
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


        //// SINGLE BUTTON SWAPPING
        //public void OnPenCategorySelection()
        //{
        //    _penSelectionHandler.SwapButton();
        //    _eraseButtonHandler.UnselectEraser();

        //    int prevIndex = _currentSelectedIndex;
        //    _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

        //    if (_currentSelectedIndex % 2 != 0)
        //    {
        //        _paintService.SetDefaultTextureMode();
        //    }
        //    else
        //    {
        //        _paintService.SetDefaultColorMode();
        //    }

        //    _scrollViewReference.content = _pencilParent.GetChild(_currentSelectedIndex) as RectTransform;

        //    // move right current pens.
        //    // then move left the new pens selected

        //    _pencilParent.DOAnchorPosX(XPOS_LEFT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
        //    {
        //        _pencilParent.GetChild(prevIndex).gameObject.SetActive(false);
        //        _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(true);

        //        MoveLeft();
        //    });
        //}
        
        // SINGLE BUTTON SWAPPING
        public void OnPenCategorySelection(int index)
        {
            if (index == _currentSelectedIndex) return;

            //_penSelectionHandler.SwapButton();
            _eraseButtonHandler.UnselectEraser();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

            //if (_currentSelectedIndex % 2 != 0)
            //{
            //    _paintService.SetDefaultTextureMode();
            //}
            //else
            //{
            //    _paintService.SetDefaultColorMode();
            //}

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
        // SINGLE BUTTON SWAPPING
        public void OnPenCategorySelection()
        {
            _penSelectionHandler.SwapButton();
            _eraseButtonHandler.UnselectEraser();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

            if (_currentSelectedIndex % 2 != 0)
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

        public void UnselectAll()
        {
            if (SelectedPenData.CurrentSelectedColorPen != -1)
                SelectedPenData.UnselectCurrentColor();

            if (SelectedPenData.CurrentSelectedTexturePen != -1)
                SelectedPenData.UnselectCurrentTexture();
        }

        private void GeneratePencils()
        {
            for (int i = 0; i < Colors.Count; i++)
            {
                int index = i;
                SelectedPenData.ColoredPens[i] = Instantiate(_pencilPrefab, _pencilParent.GetChild(0));
                Color color = Colors[i];
                color.a = 1f;
                SelectedPenData.ColoredPens[i].SetColorOnPencil(color);
                SelectedPenData.ColoredPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetColor(color);
                    SelectedPenData.ColoredPenSelection(index);
                    _eraseButtonHandler.UnselectEraser();
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
                    _eraseButtonHandler.UnselectEraser();
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

        public void UnselectCurrentColor()
        {
            if (CurrentSelectedColorPen < 0 || CurrentSelectedColorPen >= ColoredPens.Length)
            {
                Debug.LogError("Selected index is out of bound");
                return;
            }
            ColoredPens[CurrentSelectedColorPen].UnselectedPen();
        }

        public void UnselectCurrentTexture()
        {
            if (CurrentSelectedTexturePen < 0 || CurrentSelectedTexturePen >= TexturedPens.Length)
            {
                Debug.LogError("Index out of bounds");
                return;
            }

            TexturedPens[CurrentSelectedTexturePen].UnselectedTexture();
        }

        public void ColoredPenSelection(int index)
        {
            if (index < 0 || index >= ColoredPens.Length)
            {
                Debug.LogError("Index out of bounds");
                return;
            }

            if (CurrentSelectedColorPen == index) return;

            if (CurrentSelectedColorPen != -1)
            {
                ColoredPens[CurrentSelectedColorPen].UnselectedPen();
            }

            ColoredPens[index].OnPenSelected();
            CurrentSelectedColorPen = index;
        }
        public void TexPenSelection(int index)
        {
            if (index < 0 || index >= TexturedPens.Length)
            {
                Debug.LogError("Index out of bound");
                return;
            }
            if (CurrentSelectedTexturePen == index) return;

            if (CurrentSelectedTexturePen != -1)
            {
                TexturedPens[CurrentSelectedTexturePen].UnselectedTexture();
            }
            TexturedPens[index].OnTextureSelected();
            CurrentSelectedTexturePen = index;
        }
    }
}