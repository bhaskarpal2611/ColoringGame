using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GPU_SpriteColoring : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [SerializeField] private Transform _spritesParent;
    [SerializeField] private Color _color;
    [SerializeField] private int _maxColliderTouched = 10;
    [SerializeField, Range(0f, 2f)] private float _brushSize = 0.1f;
    [SerializeField] private Material _paintTextureMaterial;
    [SerializeField] private Material _eraseMaterial;
    [SerializeField] private int _brushTextureIndex = 0;
    [SerializeField] private Texture2D[] _textures;

    public Color CurrentColor
    {
        get { return _color; }
        set
        {
            _color = value;
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

    private Camera _mainCamera;
    private Touch _touch;
    private SpriteRenderer _currentSpriteRenderer;
    private RaycastHit2D[] _hits;
    private Dictionary<int, Texture2D> _originalTextures = new();
    private Dictionary<int, Texture2D> _editedTextures = new();
    private Dictionary<int, RenderTexture> _renderTextures = new();
    private Dictionary<int, Sprite> _sprites = new();

    public void SetPaintMode()
    {
        _brushMaterial = _paintTextureMaterial;
    }
    public void SetEraseMode()
    {
        _brushMaterial = _eraseMaterial;
    }


    private void Start()
    {
        Application.targetFrameRate = 60;
        _mainCamera = Camera.main;

        _hits = new RaycastHit2D[_maxColliderTouched];

        InitializeLevel();
        _brushMaterial = new Material(_paintTextureMaterial);
    }

    // private void Update()
    // {
    //     if (Input.touchCount > 0)
    //     {
    //         _touch = Input.GetTouch(0);

    //         switch (_touch.phase)
    //         {
    //             case TouchPhase.Began:
    //                 {
    //                     RaycastSprites();
    //                     break;
    //                 }
    //             case TouchPhase.Moved:
    //                 {
    //                     RaycastCurrentSprite();
    //                     break;
    //                 }
    //         }
    //     }
    // }

    public void OnBeginDrag(PointerEventData pointerEventData)
    {
        RaycastSprites(pointerEventData.position);
    }

    public void OnDrag(PointerEventData pointerEventData)
    {
        RaycastCurrentSprite(pointerEventData.position);
    }

    private void InitializeLevel()
    {
        foreach (Transform sprite in _spritesParent)
        {
            SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            int width = spriteRenderer.sprite.texture.width;
            int height = spriteRenderer.sprite.texture.height;
            int mipCount = spriteRenderer.sprite.texture.mipmapCount;
            TextureFormat textureFormat = spriteRenderer.sprite.texture.format;

            _originalTextures.Add(spriteIndex, spriteRenderer.sprite.texture);
            _editedTextures.Add(spriteIndex, new Texture2D(width, height, textureFormat, mipCount, false));

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, mipCount);
            rt.useMipMap = true;

            _renderTextures.Add(spriteIndex, rt);
        }

        // Pre-warm shaders by performing dummy Graphics.Blit operations
        // Create a temporary render texture for dummy blit
        RenderTexture tempRenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        // Pre-warm the erase mode
        SetEraseMode();
        Graphics.Blit(tempRenderTexture, tempRenderTexture, _brushMaterial);

        // Pre-warm the paint mode
        SetPaintMode();
        Graphics.Blit(tempRenderTexture, tempRenderTexture, _brushMaterial);

        // Release the temporary render texture
        tempRenderTexture.Release();
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
        ColorSpriteAtPosition(_hits[topIndex].point);
    }

    private void RaycastCurrentSprite(Vector2 touchPosition)
    {
        if (!_currentSpriteRenderer) return;

        Vector2 origin = _mainCamera.ScreenToWorldPoint(touchPosition);

        ColorSpriteAtPosition(origin);
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
            Graphics.CopyTexture(sprite.texture, _editedTextures[key]);

        // change brush texture
        _brushMaterial.SetTexture("_BrushTexture", _textures[_brushTextureIndex]);

        _brushMaterial.SetVector("_UVPosition", texturePoint / sprite.texture.width);
        _brushMaterial.SetColor("_BrushColor", CurrentColor);
        _brushMaterial.SetFloat("_BrushSize", _brushSize);
        _brushMaterial.SetTexture("_MainTex", _editedTextures[key]);
        _brushMaterial.SetTexture("_Original", _originalTextures[key]);

        // RenderTexture rt = _renderTextures[key];
        Graphics.Blit(_editedTextures[key], _renderTextures[key], _brushMaterial);
        Graphics.CopyTexture(_renderTextures[key], _editedTextures[key]);

        if (!_sprites.ContainsKey(key))
        {
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
