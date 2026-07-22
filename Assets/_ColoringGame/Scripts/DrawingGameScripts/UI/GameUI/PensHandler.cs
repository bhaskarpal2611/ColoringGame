using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace DrawingGame.UI
{
    public class PensHandler : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;

        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_TextureItem[] _texturedPencilPrefab;
        [SerializeField] private RectTransform _pencilParent;
        [SerializeField] private ScrollRect _scrollViewReference;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PenSelectionHandler _penSelectionHandler;
        [SerializeField] private EraseButtonHandler _eraseButtonHandler;
        [SerializeField] private CommonButtonFunctionsHandler _buttonsHandler;

        [SerializeField] private float XPOS_RIGHT = 0f;
        [SerializeField] private float XPOS_LEFT = -360f;

        [Header("Tab Button Visuals")]
        [Tooltip("Images for the Color and Texture tab buttons, in order (0=Color, 1=Texture).")]
        [SerializeField] private Image[] _categoryTabImages;
        [SerializeField] private Color _tabSelectedColor   = new Color(0.65f, 0.65f, 0.65f, 1f);
        [SerializeField] private Color _tabUnselectedColor = Color.white;

        private const int _maxLen = 2; // pen types

        private int _currentSelectedIndex = 0;
        private SelectedPenData SelectedPenData;

        private void Start()
        {
            // Default active and color
            _pencilParent.GetChild(0).gameObject.SetActive(true);

            SelectedPenData = new SelectedPenData(_colors.Length, _texturedPencilPrefab.Length);

            GeneratePencils();
            GenerateTexturedPens();
            MoveLeft();
            _buttonsHandler.LoadButtonsAll();
            RefreshTabVisuals();

            // Force the layout to rebuild before resetting scroll position — the grid's
            // content size isn't final until after the pens above are instantiated, so
            // setting this without a forced rebuild can silently no-op.
            Canvas.ForceUpdateCanvases();
            if (_scrollViewReference != null)
                _scrollViewReference.verticalNormalizedPosition = 1f;
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
            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .75f);
        }

        public void MoveLeft()
        {
            // Tween to the left (e.g., to -300 on the X-axis in 1 second)
            _pencilParent.DOAnchorPosX(XPOS_LEFT, .25f);
        }
        
        // Called by buttons with explicit index: 0 = Color, 1 = Texture
        public void OnPenCategorySelection(int index)
        {
            if (index == _currentSelectedIndex) return;

            SwitchToCategory(index);
        }

        // Called by the single toggle button (swaps between 0 and 1)
        public void OnPenCategorySelection()
        {
            int next = (_currentSelectedIndex + 1) % _maxLen;
            SwitchToCategory(next);
        }

        private void SwitchToCategory(int index)
        {
            _eraseButtonHandler.UnselectEraser();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = index;

            if (_currentSelectedIndex == 1)
                _paintService.SetDefaultTextureMode();
            else
                _paintService.SetDefaultColorMode();

            _scrollViewReference.content = _pencilParent.GetChild(_currentSelectedIndex) as RectTransform;

            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _pencilParent.GetChild(prevIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(true);
                MoveLeft();
            });

            RefreshTabVisuals();
        }

        private void RefreshTabVisuals()
        {
            if (_categoryTabImages == null) return;
            for (int i = 0; i < _categoryTabImages.Length; i++)
            {
                if (_categoryTabImages[i] == null) continue;
                _categoryTabImages[i].color = (i == _currentSelectedIndex) ? _tabSelectedColor : _tabUnselectedColor;
            }
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
            for (int i = 0; i < _colors.Length; i++)
            {
                int index = i;
                SelectedPenData.ColoredPens[index] = Instantiate(_pencilPrefab, _pencilParent.GetChild(0));
                Color color = _colors[index];
                color.a = 1f;
                SelectedPenData.ColoredPens[index].SetColorOnPencil(color);

                SelectedPenData.ColoredPens[index].Button.onClick.AddListener(() => 
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