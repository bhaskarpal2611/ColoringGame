using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GPU_SpriteColoring : MonoBehaviour
{
    [SerializeField] private Transform _spritesParent;
    [SerializeField] private Color _brushColor = Color.green;
    [SerializeField] private int _maxColliderTouched = 10;
    [SerializeField, Range(0f, 2f)] private float _brushSize = 0.1f;
    [SerializeField] private Material _paintByColorMaterial;
    [SerializeField] private Material _paintTextureMaterial;
    [SerializeField] private Material _eraseMaterial;
    [SerializeField] private int _brushTextureIndex = 0;
    [SerializeField] private Texture2D[] _textures;

    public Sprite _testSprite;
    public List<UndoData> _test = new();

    private Stack<UndoData> lastEditedTextures = new();

    public Color CurrentBrushColor
    {
        get => _brushColor;
        set
        {
            _brushColor = value;
            _brushMaterial.SetColor("_BrushColor", _brushColor);
        }
    }

    public int BrushTextureIndex
    {
        get => _brushTextureIndex;
        set
        {
            _brushTextureIndex = value;
            _brushMaterial.SetTexture("_BrushTexture", _textures[_brushTextureIndex]);
        }
    }

    private Material _brushMaterial;
    private bool _isDragging;
    private Vector2 _lastTouchPosition;
    private bool _firstTouch = true;

    private Camera _mainCamera;
    private SpriteRenderer _currentSpriteRenderer;
    private Collider2D _currentCollider;
    private readonly RaycastHit2D[] _hits;
    private readonly Dictionary<int, Texture2D> _editedTextures = new Dictionary<int, Texture2D>();
    private readonly Dictionary<int, RenderTexture> _renderTextures = new Dictionary<int, RenderTexture>();
    private readonly Dictionary<int, Sprite> _originalSprites = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

    private static readonly int BrushColorProperty = Shader.PropertyToID("_BrushColor");
    private static readonly int BrushSizeProperty = Shader.PropertyToID("_BrushSize");
    private static readonly int BrushTextureProperty = Shader.PropertyToID("_BrushTexture");
    private static readonly int UVPositionProperty = Shader.PropertyToID("_UVPosition");
    private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
    private static readonly int OriginalProperty = Shader.PropertyToID("_Original");

    public GPU_SpriteColoring()
    {
        _hits = new RaycastHit2D[_maxColliderTouched];
    }
    private void Start()
    {
        _mainCamera = Camera.main;
        Application.targetFrameRate = 60;
        PreWarmShaders();
        _brushMaterial.SetFloat(BrushSizeProperty, _brushSize);

    }

    private void Update()
    {
        if (Input.touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _isDragging = true;
                _firstTouch = true;
                RaycastSprites(touch.position);
                break;
            case TouchPhase.Moved:
                RaycastCurrentSprite(touch.position);
                break;
            case TouchPhase.Ended:
                EndDrag();
                break;
        }
    }

    private void OnDestroy()
    {
        CleanupResources();
    }

    public void SetPaintMode(Material material)
    {
        _brushMaterial = material;
        _brushMaterial.SetColor(BrushColorProperty, CurrentBrushColor);
        _brushMaterial.SetFloat(BrushSizeProperty, _brushSize);
    }

    public void SetPaintTextureMode() => SetPaintMode(_paintTextureMaterial);
    public void SetEraseMode() => SetPaintMode(_eraseMaterial);
    public void SetPaintColorMode() => SetPaintMode(_paintByColorMaterial);

    public void SetBrushScale(float value)
    {
        _brushSize = value;
        _brushMaterial.SetFloat(BrushSizeProperty, _brushSize);
    }

    public void SetBrushTexture(int index)
    {
        SetPaintTextureMode();
        _brushTextureIndex = index;
        _brushMaterial.SetTexture(BrushTextureProperty, _textures[_brushTextureIndex]);
    }

    public void ClearPainting()
    {
        RestoreOriginalTextures();
        ReapplySpritesToRenderers();

        // also clear undo stack
        lastEditedTextures.Clear();
    }

    public void InitializeLevel()
    {
        foreach (Transform spriteTransform in _spritesParent.GetChild(0).GetChild(0))
        {
            InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>());
        }
    }

    private void InitializeSprite(SpriteRenderer spriteRenderer)
    {
        int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
        Sprite originalSprite = spriteRenderer.sprite;
        Texture2D originalTexture = originalSprite.texture;

        _originalSprites.Add(spriteIndex, originalSprite);
        _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount, false));
        _renderTextures.Add(spriteIndex, new RenderTexture(originalTexture.width, originalTexture.height, 0, RenderTextureFormat.ARGB32, originalTexture.mipmapCount)
        {
            useMipMap = false,
            autoGenerateMips = false
        });
    }

    private void PreWarmShaders()
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        RenderTexture tempDestRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        RenderTexture prevActiveRT = RenderTexture.active;

        PreWarmMaterial(_eraseMaterial, tempRT, tempDestRT);
        PreWarmMaterial(_paintTextureMaterial, tempRT, tempDestRT);
        PreWarmMaterial(_paintByColorMaterial, tempRT, tempDestRT);

        RenderTexture.active = prevActiveRT;
        RenderTexture.ReleaseTemporary(tempRT);
        RenderTexture.ReleaseTemporary(tempDestRT);
    }

    private void PreWarmMaterial(Material material, RenderTexture source, RenderTexture destination)
    {
        SetPaintMode(material);
        Graphics.Blit(source, destination, _brushMaterial);
    }

    private void RaycastSprites(Vector2 touchPosition)
    {
        Vector2 origin = _mainCamera.ScreenToWorldPoint(touchPosition);
        int hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.zero, _hits);
        if (hitCount <= 0) return;

        int maxSortingLayer = -1000;
        int topIndex = -1;

        for (int i = 0; i < hitCount; i++)
        {
            if (!_hits[i].collider.TryGetComponent(out SpriteRenderer sr)) continue;

            int sortingOrder = sr.sortingOrder;
            if (sortingOrder <= maxSortingLayer) continue;

            maxSortingLayer = sortingOrder;
            topIndex = i;
            _currentSpriteRenderer = sr;
        }

        if (topIndex == -1) return;

        _currentCollider = _currentSpriteRenderer.GetComponent<Collider2D>();
        _currentCollider.enabled = false;
        ColorSpriteAtPosition(_hits[topIndex].point);
    }

    private void RaycastCurrentSprite(Vector2 touchPosition)
    {
        if (!_currentSpriteRenderer) return;

        Vector2 origin = _mainCamera.ScreenToWorldPoint(touchPosition);

        if (_isDragging)
        {
            DrawLines(origin);
        }
    }

    private void DrawLines(Vector2 currentHitPoint)
    {
        if (_firstTouch)
        {
            _lastTouchPosition = currentHitPoint;
            _firstTouch = false;
        }

        float distance = Vector2.Distance(_lastTouchPosition, currentHitPoint);
        int steps = Mathf.CeilToInt(distance / (_brushSize * 0.75f));
        int currentSteps = 0;
        const int blitThreshold = 10;

        for (int i = 0; i <= steps; i++)
        {
            currentSteps++;
            if (currentSteps >= blitThreshold)
            {
                currentSteps = 0;
                Vector2 interpolatedPoint = Vector2.Lerp(_lastTouchPosition, currentHitPoint, i / (float)steps);
                ColorSpriteAtPosition(interpolatedPoint);
            }
        }
        _lastTouchPosition = currentHitPoint;
    }

    private void ColorSpriteAtPosition(Vector2 hitPoint)
    {
        if (_currentSpriteRenderer == null) return;

        Vector2 texturePoint = WorldToTexturePoint(hitPoint);
        Sprite sprite = _currentSpriteRenderer.sprite;
        int key = _currentSpriteRenderer.transform.GetSiblingIndex();

        if (sprite.texture != _editedTextures[key])
            Graphics.CopyTexture(sprite.texture, _editedTextures[key]);

        _brushMaterial.SetVector(UVPositionProperty, texturePoint / sprite.texture.width);
        _brushMaterial.SetTexture(MainTexProperty, _editedTextures[key]);
        _brushMaterial.SetTexture(OriginalProperty, _originalSprites[key].texture);

        Graphics.Blit(_editedTextures[key], _renderTextures[key], _brushMaterial);
        Graphics.CopyTexture(_renderTextures[key], _editedTextures[key]);

        if (!_sprites.TryGetValue(key, out Sprite _))
        {
            Sprite newSprite = Sprite.Create(_editedTextures[key], sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
            _sprites.Add(key, newSprite);

            // create original clean version of the sprite
            Texture2D newTex = new(_originalSprites[key].texture.width, _originalSprites[key].texture.height, TextureFormat.RGBA32, 1, false);

            Graphics.CopyTexture(_originalSprites[key].texture, newTex);
            Sprite test = Sprite.Create(newTex, _currentSpriteRenderer.sprite.rect, new(0.5f, 0.5f), _currentSpriteRenderer.sprite.pixelsPerUnit);

            UndoData undoData = new UndoData(key, newTex, test.rect, test.pixelsPerUnit);
            lastEditedTextures.Push(undoData);
        }
        _currentSpriteRenderer.sprite = _sprites[key];
    }

    private Vector2 WorldToTexturePoint(Vector2 worldPos)
    {
        Vector2 texturePoint = _currentSpriteRenderer.transform.InverseTransformPoint(worldPos);
        Vector2 spriteSize = _currentSpriteRenderer.bounds.size;
        Rect spriteRect = _currentSpriteRenderer.sprite.rect;

        texturePoint = new Vector2(
            (texturePoint.x / spriteSize.x + 0.5f) * spriteRect.width + spriteRect.x,
            (texturePoint.y / spriteSize.y + 0.5f) * spriteRect.height + spriteRect.y
        );
        return texturePoint;
    }

    private void EndDrag()
    {
        if (_currentSpriteRenderer)
        {
            _isDragging = false;

            int index = _currentSpriteRenderer.transform.GetSiblingIndex();
            if(lastEditedTextures.Count == 0)
            {
                // create original clean version of the sprite
                Texture2D cleanTex = new(_originalSprites[index].texture.width, _originalSprites[index].texture.height, TextureFormat.RGBA32, 1, false);

                Graphics.CopyTexture(_originalSprites[index].texture, cleanTex);
                Sprite test = Sprite.Create(cleanTex, _currentSpriteRenderer.sprite.rect, new(0.5f, 0.5f), _currentSpriteRenderer.sprite.pixelsPerUnit);

                UndoData cleanData = new UndoData(index, cleanTex, test.rect, test.pixelsPerUnit);
                lastEditedTextures.Push(cleanData);
            }

            Texture2D newTex = new(_editedTextures[index].width, _editedTextures[index].height, TextureFormat.RGBA32, 1, false);
            Graphics.CopyTexture(_editedTextures[index], newTex);

            UndoData undoData = new UndoData(index, newTex, _currentSpriteRenderer.sprite.rect, _currentSpriteRenderer.sprite.pixelsPerUnit);

            lastEditedTextures.Push(undoData);

            if (_currentCollider != null)
            {
                _currentCollider.enabled = true;
                _currentCollider = null;
            }
            _currentSpriteRenderer = null;
        }
    }
    public void PerformUndo()
    {
        if (lastEditedTextures.Count > 0)
        {
            UndoData undoData = lastEditedTextures.Pop();
            Sprite newSprite = Sprite.Create(undoData.Texture, undoData.Rect, new(0.5f, 0.5f), undoData.PPU);
            _testSprite = newSprite;
            _sprites[undoData.Index] = newSprite;
            _spritesParent.GetChild(undoData.Index).GetComponent<SpriteRenderer>().sprite = newSprite;
        }
    }

    private void CleanupResources()
    {
        foreach (var rt in _renderTextures.Values)
        {
            rt.Release();
        }
        _renderTextures.Clear();

        foreach (var texture in _editedTextures.Values)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        _editedTextures.Clear();

        _sprites.Clear();
        _originalSprites.Clear();
    }
    private void RestoreOriginalTextures()
    {
        foreach (var kvp in _originalSprites)
        {
            int index = kvp.Key;
            Sprite originalSprite = kvp.Value;
            Texture2D originalTexture = originalSprite.texture;

            if (_editedTextures.TryGetValue(index, out Texture2D editedTexture))
            {
                Graphics.CopyTexture(originalTexture, editedTexture);
            }

            if (_sprites.TryGetValue(index, out Sprite sprite))
            {
                _sprites[index] = Sprite.Create(editedTexture, sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
            }
        }
    }
    private void ReapplySpritesToRenderers()
    {
        foreach (Transform spriteTransform in _spritesParent)
        {
            SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            if (_sprites.TryGetValue(spriteIndex, out Sprite newSprite))
            {
                spriteRenderer.sprite = newSprite;
            }
        }
    }

    [Serializable]
    public struct UndoData
    {
        public int Index;
        public Texture2D Texture;
        public Rect Rect;
        public float PPU;

        public UndoData(int index, Texture2D texture, Rect rect, float ppu)
        {
            Index = index;
            Texture = texture;
            Rect = rect;
            PPU = ppu;
        }
    }
}