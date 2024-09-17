using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GPU_SpriteColoring : MonoBehaviour
{
    [SerializeField] private Transform _spritesParent;
    [SerializeField] private Color _brushColor;
    [SerializeField] private int _maxColliderTouched = 10;
    [SerializeField, Range(0f, 2f)] private float _brushSize = 0.1f;
    [SerializeField] private Material _paintByColorMaterial;
    [SerializeField] private Material _paintTextureMaterial;
    [SerializeField] private Material _eraseMaterial;
    [SerializeField] private int _brushTextureIndex = 0;
    [SerializeField] private Texture2D[] _textures;

    public Color CurrentBrushColor
    {
        get { return _brushColor; }
        set
        {
            _brushColor = value;
            _brushMaterial.SetColor("_BrushColor", CurrentBrushColor);
        }
    }

    public int BrushTextureIndex
    {
        get { return _brushTextureIndex; }
        set
        {
            _brushTextureIndex = value;
            _brushMaterial.SetTexture("_BrushTexture", _textures[_brushTextureIndex]);
        }
    }

    private Material _brushMaterial;
    private Coroutine _initializeRoutine;
    private bool _isDragging;

    private Vector2 _lastTouchPosition;
    private bool _firstTouch = true;

    private Camera _mainCamera;
    private Touch _touch;
    private SpriteRenderer _currentSpriteRenderer;
    private Collider2D _currentCollider;

    private RaycastHit2D[] _hits;
    private Dictionary<int, Texture2D> _originalTextures = new();
    private Dictionary<int, Texture2D> _editedTextures = new();
    private Dictionary<int, RenderTexture> _renderTextures = new();
    private Dictionary<int, Sprite> _sprites = new();

    private void Start()
    {
        _mainCamera = Camera.main;
        Application.targetFrameRate = 60;
        _hits = new RaycastHit2D[_maxColliderTouched];
        InitializeLevel();
        _brushMaterial = new Material(_paintTextureMaterial);
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    {
                        _isDragging = true;
                        _firstTouch = true;
                        RaycastSprites(touch.position);
                        break;
                    }
                case TouchPhase.Moved:
                    {
                        RaycastCurrentSprite(touch.position);
                        break;
                    }

                case TouchPhase.Ended:
                    {
                        _isDragging = false;
                        if (_currentCollider != null)
                        {
                            _currentCollider.enabled = true;
                            _currentCollider = null;
                        }
                        _currentSpriteRenderer = null;
                        break;
                    }
            }

        }

    }

    //public void OnPointerDown(PointerEventData pointerEventData)
    //{
    //    _isDragging = true;
    //    _firstTouch = true;
    //    RaycastSprites(pointerEventData.position);
    //}
    //public void OnPointerUp(PointerEventData pointerEventData)
    //{
    //    _isDragging = false;
    //    _currentCollider.enabled = true;
    //    _currentCollider = null;
    //    _currentSpriteRenderer = null;
    //}

    //public void OnDrag(PointerEventData pointerEventData)
    //{
    //    RaycastCurrentSprite(pointerEventData.position);
    //}

    public void SetPaintMode() => _brushMaterial = _paintTextureMaterial;
    public void SetEraseMode() => _brushMaterial = _eraseMaterial;
    public void SetPaintColorMode() => _brushMaterial = _paintByColorMaterial;
    public void SetBrushScale(float value)
    {
        _brushSize = value;
        _brushMaterial.SetFloat("_BrushSize", _brushSize);
    }

    public void SetBrushTexture(int index)
    {
        SetPaintMode();
        _brushTextureIndex = index;
        _brushMaterial.SetTexture("_BrushTexture", _textures[_brushTextureIndex]);
    }

    private void InitializeLevel()
    {
        foreach (Transform spriteTransform in _spritesParent)
        {
            SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            int width = spriteRenderer.sprite.texture.width;
            int height = spriteRenderer.sprite.texture.height;
            int mipCount = spriteRenderer.sprite.texture.mipmapCount;
            TextureFormat textureFormat = spriteRenderer.sprite.texture.format;
            _originalTextures.Add(spriteIndex, spriteRenderer.sprite.texture);
            _editedTextures.Add(spriteIndex, CreateUncompressedCopy(spriteRenderer.sprite.texture));

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.useMipMap = false; // Explicitly disable mipmaps
            rt.autoGenerateMips = false; // Disable automatic mipmap generation

            _renderTextures.Add(spriteIndex, rt);

            // // // add sprites as well
            // Graphics.CopyTexture(_originalTextures[spriteIndex], _editedTextures[spriteIndex]);

            // // // Create a new sprite from the modified texture
            // Sprite newSprite = Sprite.Create(_editedTextures[spriteIndex], spriteRenderer.sprite.rect, Vector2.one / 2, spriteRenderer.sprite.pixelsPerUnit);
            // // // Add to dictionary
            // _sprites.Add(spriteIndex, newSprite);
            // spriteRenderer.sprite = newSprite;

            // yield return null;
        }

        // Pre-warm shaders
        RenderTexture tempRenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        RenderTexture tempDestinationTexture = new RenderTexture(Screen.width, Screen.height, 0);

        // Store the current active RenderTexture
        RenderTexture previousActiveRT = RenderTexture.active;

        // Pre-warm the erase mode
        SetEraseMode();
        Graphics.Blit(tempRenderTexture, tempDestinationTexture, _brushMaterial);

        // Pre-warm the paint mode
        SetPaintMode();
        Graphics.Blit(tempRenderTexture, tempDestinationTexture, _brushMaterial);

        // pre-warm paint color mode
        SetPaintColorMode();
        Graphics.Blit(tempRenderTexture, tempDestinationTexture, _brushMaterial);

        RenderTexture.active = previousActiveRT;

        // Release the temporary render texture
        tempRenderTexture.Release();
        tempDestinationTexture.Release();
    }

    private void RaycastSprites(Vector2 touchPosition)
    {
        Vector2 origin = _mainCamera.ScreenToWorldPoint(touchPosition);

        _hits = Physics2D.RaycastAll(origin, Vector2.zero);
        if (_hits.Length <= 0) return;

        // select top-most sprite renderer
        int maxSortingLayer = -1000, topIndex = -1;
        for (int i = 0; i < _hits.Length; i++)
        {
            if (!_hits[i].collider.TryGetComponent(out SpriteRenderer sr))
            {
                continue;
            }
            int sortingOrder = sr.sortingOrder;
            if (sortingOrder > maxSortingLayer)
            {
                maxSortingLayer = sortingOrder;
                topIndex = i;
                _currentSpriteRenderer = sr;
            }
        }
        _currentCollider = _currentSpriteRenderer.GetComponent<Collider2D>();
        _currentCollider.enabled = false;
        ColorSpriteAtPosition(_hits[topIndex].point);
    }
    private Texture2D CreateUncompressedCopy(Texture2D compressedTexture)
    {
        // Create a new uncompressed texture of the same size
        Texture2D uncompressedTexture = new Texture2D(compressedTexture.width, compressedTexture.height, TextureFormat.RGBA32, 1, false);

        // Copy pixel data from the compressed texture to the uncompressed one
        Color[] pixels = compressedTexture.GetPixels();
        uncompressedTexture.SetPixels(pixels);
        uncompressedTexture.Apply();

        return uncompressedTexture;
    }

    private void RaycastCurrentSprite(Vector2 touchPosition)
    {
        if (!_currentSpriteRenderer) return;

        Vector2 origin = _mainCamera.ScreenToWorldPoint(touchPosition);

        if (_isDragging)
            //ColorSpriteAtPosition(origin);
            DrawLines(origin);
    }

    private void DrawLines(Vector2 currentHitPoint)
    {
        if (_currentSpriteRenderer == null) return;

        if (_firstTouch)
        {
            _lastTouchPosition = currentHitPoint;
            _firstTouch = false;
        }

        // Get the distance between last and current positions
        float distance = Vector2.Distance(_lastTouchPosition, currentHitPoint);

        // If the distance is large, interpolate points between them
        int steps = Mathf.CeilToInt(distance / (_brushSize * 0.75f)); // Adjust the step size based on brush size
        int currentSteps = 0;
        int blitThreshold = 10;
        for (int i = 0; i <= steps; i++)
        {
            currentSteps++;
            if (currentSteps >= blitThreshold)
            {
                currentSteps = 0;
                // Interpolate between the last and current position
                Vector2 interpolatedPoint = Vector2.Lerp(_lastTouchPosition, currentHitPoint, i / (float)steps);
                // Convert interpolated point to texture coordinates and paint
                ColorSpriteAtPosition(interpolatedPoint);
            }
        }

        _lastTouchPosition = currentHitPoint;
    }

    private void ColorSpriteAtPosition(Vector2 hitPoint)
    {
        if (_currentSpriteRenderer == null) return;

        // Convert our hitPoint (World Space) to a texture point
        Vector2 texturePoint = WorldToTexturePoint(hitPoint);

        // Get the sprite and its texture
        Sprite sprite = _currentSpriteRenderer.sprite;

        int key = _currentSpriteRenderer.transform.GetSiblingIndex();

        if (sprite.texture != _editedTextures[key])
            // Graphics.CopyTexture(sprite.texture, _editedTextures[key]);

            Debug.Log("mipCount: " + sprite.texture.mipmapCount);
        // edit brush material common values
        _brushMaterial.SetVector("_UVPosition", texturePoint / sprite.texture.width);
        _brushMaterial.SetTexture("_MainTex", _editedTextures[key]);
        _brushMaterial.SetTexture("_Original", _originalTextures[key]);


        Graphics.Blit(_editedTextures[key], _renderTextures[key], _brushMaterial);
        Graphics.CopyTexture(_renderTextures[key], _editedTextures[key]);

        if (!_sprites.ContainsKey(key))
        {
            // Debug.LogError("Should not be reaching this line");

            // Create a new sprite from the modified texture
            Sprite newSprite = Sprite.Create(_editedTextures[key], sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
            // Add to dictionary
            _sprites.Add(key, newSprite);
        }
        _currentSpriteRenderer.sprite = _sprites[key];
    }

    private Vector2 WorldToTexturePoint(Vector2 worldPos)
    {
        Vector2 texturePoint = _currentSpriteRenderer.transform.InverseTransformPoint(worldPos);

        // Position between -5 and 5
        texturePoint.x /= _currentSpriteRenderer.bounds.size.x;
        texturePoint.y /= _currentSpriteRenderer.bounds.size.y;

        // Position between 0 & 1
        texturePoint += Vector2.one / 2;

        // Offset in Texture space
        texturePoint.x *= _currentSpriteRenderer.sprite.rect.width;
        texturePoint.y *= _currentSpriteRenderer.sprite.rect.height;
        // Position in Texture Space
        texturePoint.x += _currentSpriteRenderer.sprite.rect.x;
        texturePoint.y += _currentSpriteRenderer.sprite.rect.y;

        return texturePoint;
    }
}
