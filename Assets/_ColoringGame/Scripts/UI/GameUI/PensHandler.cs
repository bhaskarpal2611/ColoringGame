using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace ColorSwipeGame.UI
{
    public class PensHandler : MonoBehaviour
    {
        // 20 essential colors for a kids coloring game
        private static readonly Color[] DefaultColors = new Color[]
        {
            new Color(1.00f, 0.10f, 0.10f), // Red
            new Color(1.00f, 0.40f, 0.00f), // Orange-Red
            new Color(1.00f, 0.60f, 0.00f), // Orange
            new Color(1.00f, 0.84f, 0.00f), // Yellow
            new Color(0.60f, 0.90f, 0.20f), // Yellow-Green
            new Color(0.20f, 0.78f, 0.20f), // Green
            new Color(0.00f, 0.50f, 0.20f), // Dark Green
            new Color(0.00f, 0.75f, 0.75f), // Teal
            new Color(0.40f, 0.78f, 1.00f), // Sky Blue
            new Color(0.10f, 0.40f, 1.00f), // Blue
            new Color(0.10f, 0.10f, 0.70f), // Dark Blue
            new Color(0.55f, 0.10f, 0.85f), // Purple
            new Color(0.80f, 0.20f, 0.80f), // Violet
            new Color(1.00f, 0.30f, 0.65f), // Hot Pink
            new Color(1.00f, 0.70f, 0.85f), // Light Pink
            new Color(0.55f, 0.27f, 0.07f), // Brown
            new Color(0.90f, 0.70f, 0.50f), // Skin / Peach
            new Color(0.90f, 0.90f, 0.90f), // Light Gray
            new Color(0.40f, 0.40f, 0.40f), // Dark Gray
            new Color(0.05f, 0.05f, 0.05f), // Black
        };

        [SerializeField] private Color[] _colors;

        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_TextureItem[] _texturedPencilPrefab;
        [SerializeField] private RectTransform _pencilParent;
        [SerializeField] private ScrollRect _scrollViewReference;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PenSelectionHandler _penSelectionHandler;
        [SerializeField] private EraseButtonHandler _eraseButtonHandler;
        [SerializeField] private CommonButtonFunctionsHandler _buttonsHandler;

        [Header("Color Grid Settings")]
        [SerializeField] private Sprite _colorButtonSprite;   // sprite used for the color circle
        [SerializeField] private Sprite _selectionRingSprite; // sprite used for the selection outline ring
        [SerializeField] private Vector2 _cellSize = new Vector2(161f, 161f);
        [SerializeField] private Vector2 _textureCellSize = new Vector2(161f, 161f);
        [SerializeField] private Vector2 _cellSpacing = new Vector2(10f, 10f);
     [SerializeField] private RectOffset _gridPadding;
        [SerializeField] private int _columnCount = 2;
        [SerializeField] private float _selectionOutlineSize = 8f; // px the outline extends beyond the button
        [SerializeField] private float _selectionRingYScale = 1f;  // tweak if ring overflows vertically

        private const float XPOS_RIGHT = 0f;
        private const float XPOS_LEFT = -360f;
        private const int _maxLen = 2;

        private int _currentSelectedIndex = 0;
        private SelectedPenData SelectedPenData;
        private GridLayoutGroup _colorGrid;

private void Awake()
{
    if (_gridPadding == null)
        _gridPadding = new RectOffset(12, 12, 12, 12);
}
        private void Start()
        {
            if (_colors == null || _colors.Length == 0)
                _colors = DefaultColors;

            if (_pencilParent == null)
            {
                Debug.LogError("PensHandler: _pencilParent is not assigned!", this);
                return;
            }
            if (_pencilParent.childCount == 0)
            {
                Debug.LogError("PensHandler: _pencilParent has no children!", this);
                return;
            }

            _pencilParent.GetChild(0).gameObject.SetActive(true);

            SetupColorGrid();

            int texLen = (_texturedPencilPrefab != null) ? _texturedPencilPrefab.Length : 0;
            SelectedPenData = new SelectedPenData(_colors.Length, texLen);

            GeneratePencils();
            if (texLen > 0)
            {
                SetupTextureGrid();
                GenerateTexturedPens();
            }

            MoveLeft();

            if (_buttonsHandler != null)
                _buttonsHandler.LoadButtonsAll();

            // Wait one frame for Canvas layout to settle, then fit cells and reset scroll
            StartCoroutine(InitAfterLayout());
        }

        private IEnumerator InitAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyResponsiveCellSize();
            Canvas.ForceUpdateCanvases();
            ResetScrollToTop();

            // Auto-select first color after all Start() calls have run
            if (_colors.Length > 0 && SelectedPenData.ColoredPens[0] != null)
            {
                _paintService.SetColor(_colors[0]);
                SelectedPenData.ColoredPenSelection(0);
                SelectedPenData.ColoredPens[0].PenSelected();
            }
        }

        // ── Grid Setup ──────────────────────────────────────────────────────────

        private void SetupColorGrid()
        {
            var colorContainer = _pencilParent.GetChild(0);
            if (colorContainer == null)
            {
                Debug.LogError("PensHandler: color container (child 0 of _pencilParent) is null!", this);
                return;
            }

            // Remove any conflicting non-Grid layout group
            var existing = colorContainer.GetComponent<LayoutGroup>();
            if (existing != null && !(existing is GridLayoutGroup))
                Destroy(existing);

            _colorGrid = colorContainer.GetComponent<GridLayoutGroup>();
            if (_colorGrid == null)
                _colorGrid = colorContainer.gameObject.AddComponent<GridLayoutGroup>();

            if (_colorGrid == null)
            {
                Debug.LogError("PensHandler: Failed to add GridLayoutGroup!", this);
                return;
            }

            _colorGrid.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            _colorGrid.constraintCount  = _columnCount;
            _colorGrid.spacing          = _cellSpacing;
            _colorGrid.padding          = _gridPadding;
            _colorGrid.childAlignment   = TextAnchor.UpperCenter;
            _colorGrid.startAxis        = GridLayoutGroup.Axis.Horizontal;
            _colorGrid.startCorner      = GridLayoutGroup.Corner.UpperLeft;

            _colorGrid.cellSize = _cellSize;

            var fitter = colorContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = colorContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            if (_scrollViewReference != null)
            {
                _scrollViewReference.content    = colorContainer as RectTransform;
                _scrollViewReference.vertical   = true;
                _scrollViewReference.horizontal = false;
            }
        }

        private void SetupTextureGrid()
        {
            var texContainer = _pencilParent.GetChild(1);

            var existing = texContainer.GetComponent<LayoutGroup>();
            if (existing != null && !(existing is GridLayoutGroup))
                Destroy(existing);

            var texGrid = texContainer.GetComponent<GridLayoutGroup>();
            if (texGrid == null)
                texGrid = texContainer.gameObject.AddComponent<GridLayoutGroup>();

            texGrid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            texGrid.constraintCount = _columnCount;
            texGrid.spacing         = _cellSpacing;
            texGrid.padding         = _gridPadding;
            texGrid.childAlignment  = TextAnchor.UpperCenter;
            texGrid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            texGrid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            texGrid.cellSize        = _textureCellSize;

            var fitter = texContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = texContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ApplyResponsiveCellSize()
        {
            if (_colorGrid == null) return;
            _colorGrid.cellSize = _cellSize;
        }

        // ── Scroll helpers ───────────────────────────────────────────────────────

        // verticalNormalizedPosition = 1 is the TOP in Unity's ScrollRect
        private void ResetScrollToTop()
        {
            if (_scrollViewReference == null) return;
            _scrollViewReference.StopMovement();
            _scrollViewReference.verticalNormalizedPosition = 1f;
        }

        // ── Panel movement ───────────────────────────────────────────────────────

        public void MoveRight()
        {
            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .75f);
        }

        // Slide the panel in and reset scroll to top once it arrives
        public void MoveLeft()
        {
            _pencilParent.DOAnchorPosX(XPOS_LEFT, .25f).OnComplete(ResetScrollToTop);
        }

        public void OnPenCategorySelection(int index)
        {
            if (index == _currentSelectedIndex) return;

            if (_eraseButtonHandler != null)
                _eraseButtonHandler.UnselectEraser();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

            if (_scrollViewReference != null)
                _scrollViewReference.content = _pencilParent.GetChild(_currentSelectedIndex) as RectTransform;

            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _pencilParent.GetChild(prevIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(true);
                MoveLeft(); // MoveLeft now resets scroll on complete
            });
        }

        public void OnPenCategorySelection()
        {
            if (_penSelectionHandler != null)
                _penSelectionHandler.SwapButton();
            if (_eraseButtonHandler != null)
                _eraseButtonHandler.UnselectEraser();

            int prevIndex = _currentSelectedIndex;
            _currentSelectedIndex = ++_currentSelectedIndex % _maxLen;

            bool isColorTab = _currentSelectedIndex % 2 == 0;
            if (isColorTab)
                _paintService.SetDefaultColorMode();
            else
                _paintService.SetDefaultTextureMode();

            if (_penSelectionHandler != null)
                _penSelectionHandler.SetButtonTints(isColorTab);

            if (_scrollViewReference != null)
                _scrollViewReference.content = _pencilParent.GetChild(_currentSelectedIndex) as RectTransform;

            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _pencilParent.GetChild(prevIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(true);
                MoveLeft(); // MoveLeft now resets scroll on complete
            });
        }

        public void AutoSelectFirstColor()
        {
            if (SelectedPenData.ColoredPens == null || SelectedPenData.ColoredPens[0] == null) return;

            // Invoking the button fires OnPenSelected event → all other pens deselect themselves,
            // eraser is unselected, and paint service color is set — same as a real tap.
            SelectedPenData.ColoredPens[0].Button?.onClick.Invoke();
        }

        public void UnselectAll()
        {
            if (SelectedPenData.CurrentSelectedColorPen != -1)
                SelectedPenData.UnselectCurrentColor();

            if (SelectedPenData.CurrentSelectedTexturePen != -1)
                SelectedPenData.UnselectCurrentTexture();
        }

        // ── Pencil generation ────────────────────────────────────────────────────

        private void GeneratePencils()
        {
            var colorContainer = _pencilParent.GetChild(0);

            for (int i = 0; i < _colors.Length; i++)
            {
                int index = i;
                Color color = _colors[index];
                color.a = 1f;

                var penItem = Instantiate(_pencilPrefab, colorContainer);
                penItem.transform.localRotation = Quaternion.identity;
                penItem.transform.localScale    = Vector3.one;

                // Hide all original children (crayon art etc.)
                for (int c = 0; c < penItem.transform.childCount; c++)
                    penItem.transform.GetChild(c).gameObject.SetActive(false);

                // Root Image: transparent container, handles raycasts for the button
                var rootImg = penItem.GetComponent<Image>();
                if (rootImg != null)
                {
                    rootImg.color         = new Color(0f, 0f, 0f, 0f);
                    rootImg.raycastTarget = true;
                }

                // Child 0 — white circular ring (renders behind child 1)
                var ringGO  = new GameObject("SelectionRing", typeof(Image));
                ringGO.transform.SetParent(penItem.transform, false);
                var ringRT  = ringGO.GetComponent<RectTransform>();
                ringRT.anchorMin = Vector2.zero;
                ringRT.anchorMax = Vector2.one;
                ringRT.offsetMin = new Vector2(-_selectionOutlineSize, -_selectionOutlineSize);
                ringRT.offsetMax = new Vector2( _selectionOutlineSize,  _selectionOutlineSize);
                var ringImg = ringGO.GetComponent<Image>();
                var ringSprite = _selectionRingSprite != null ? _selectionRingSprite : _colorButtonSprite;
                if (ringSprite != null) ringImg.sprite = ringSprite;
                ringImg.color         = Color.white;
                ringImg.type          = Image.Type.Simple;
                ringImg.raycastTarget = false;
                ringImg.enabled       = false; // hidden until selected
                ringGO.transform.localScale = new Vector3(1f, _selectionRingYScale, 1f);

                // Child 1 — colored circle (renders on top of ring; only the rim of the ring shows)
                var circleGO  = new GameObject("ColorCircle", typeof(Image));
                circleGO.transform.SetParent(penItem.transform, false);
                var circleRT  = circleGO.GetComponent<RectTransform>();
                circleRT.anchorMin = Vector2.zero;
                circleRT.anchorMax = Vector2.one;
                circleRT.offsetMin = Vector2.zero;
                circleRT.offsetMax = Vector2.zero;
                var circleImg = circleGO.GetComponent<Image>();
                if (_colorButtonSprite != null) circleImg.sprite = _colorButtonSprite;
                circleImg.color          = color;
                circleImg.type           = Image.Type.Simple;
                circleImg.preserveAspect = true;
                circleImg.raycastTarget  = false;

                // Wire up references so UI_PencilItem uses our new images
                penItem.SetReferences(circleImg, ringImg);
                penItem.SetColorOnPencil(color);
                SelectedPenData.ColoredPens[index] = penItem;

                var btn = penItem.Button ?? penItem.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        _paintService.SetColor(color);
                        SelectedPenData.ColoredPenSelection(index);
                        if (_eraseButtonHandler != null)
                            _eraseButtonHandler.UnselectEraser();
                    });
                }
            }
        }

        private void GenerateTexturedPens()
        {
            var texContainer = _pencilParent.GetChild(1);
            for (int i = 0; i < _texturedPencilPrefab.Length; i++)
            {
                int index = i;
                var texItem = Instantiate(_texturedPencilPrefab[i], texContainer);
                SelectedPenData.TexturedPens[i] = texItem;

                // White square border at sibling 0 — renders behind the pattern image children
                var borderGO  = new GameObject("SelectionBorder", typeof(Image));
                borderGO.transform.SetParent(texItem.transform, false);
                borderGO.transform.SetSiblingIndex(0);
                var borderRT  = borderGO.GetComponent<RectTransform>();
                borderRT.anchorMin = Vector2.zero;
                borderRT.anchorMax = Vector2.one;
                borderRT.offsetMin = new Vector2(-_selectionOutlineSize, -_selectionOutlineSize);
                borderRT.offsetMax = new Vector2( _selectionOutlineSize,  _selectionOutlineSize);
                var borderImg = borderGO.GetComponent<Image>();
                borderImg.color         = Color.white;
                borderImg.raycastTarget = false;
                texItem.SetSelectionBorder(borderImg);

                texItem.Button.onClick.AddListener(() =>
                {
                    _paintService.SetTexture(index);
                    SelectedPenData.TexPenSelection(index);
                    if (_eraseButtonHandler != null)
                        _eraseButtonHandler.UnselectEraser();
                });
            }
        }
    }

    // ── SelectedPenData ──────────────────────────────────────────────────────────

    [System.Serializable]
    public struct SelectedPenData
    {
        public UI_PencilItem[] ColoredPens;
        public UI_TextureItem[] TexturedPens;
        public int CurrentSelectedColorPen;
        public int CurrentSelectedTexturePen;

        public SelectedPenData(int len1, int len2)
        {
            CurrentSelectedColorPen  = -1;
            CurrentSelectedTexturePen = -1;
            ColoredPens  = new UI_PencilItem[len1];
            TexturedPens = len2 > 0 ? new UI_TextureItem[len2] : new UI_TextureItem[0];
        }

        public void UnselectCurrentColor()
        {
            if (CurrentSelectedColorPen < 0 || CurrentSelectedColorPen >= ColoredPens.Length) return;
            ColoredPens[CurrentSelectedColorPen].UnselectedPen();
        }

        public void UnselectCurrentTexture()
        {
            if (CurrentSelectedTexturePen < 0 || CurrentSelectedTexturePen >= TexturedPens.Length) return;
            TexturedPens[CurrentSelectedTexturePen].UnselectedTexture();
        }

        public void ColoredPenSelection(int index)
        {
            if (index < 0 || index >= ColoredPens.Length) return;
            CurrentSelectedColorPen = index;
        }

        public void TexPenSelection(int index)
        {
            if (index < 0 || index >= TexturedPens.Length) return;
            if (CurrentSelectedTexturePen == index) return;
            if (CurrentSelectedTexturePen != -1)
                TexturedPens[CurrentSelectedTexturePen].UnselectedTexture();
            TexturedPens[index].OnTextureSelected();
            CurrentSelectedTexturePen = index;
        }
    }
}
