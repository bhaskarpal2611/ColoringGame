using System.Collections.Generic;
using UnityEngine;

public class GPU_SpriteColoring_Textures : MonoBehaviour
{
    [SerializeField] private Color _color;
    [SerializeField, Range(0f, 2f)] private float _brushSize = 0.1f;
    [SerializeField] private Material _brushMaterial;

    public Color CurrentColor
    {
        get { return _color; }
        set
        {
            _color = value;
        }
    }

    private Touch _touch;
    private SpriteRenderer _currentSpriteRenderer;
    private Collider2D _selectedCollider;

    //private List<Collider2D> _colliders;

    private Dictionary<int, Texture2D> _originalTextures = new();
    private Dictionary<int, Texture2D> _editedTextures = new();
    private Dictionary<int, RenderTexture> _renderTextures = new();
    private Dictionary<int, Sprite> _sprites = new();

    private Camera _mainCamera;

    private void Start()
    {
        Application.targetFrameRate = 60;
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            _touch = Input.GetTouch(0);

            switch (_touch.phase)
            {
                case TouchPhase.Began:
                    {
                        RaycastSprites();
                        break;
                    }
                case TouchPhase.Moved:
                    {
                        RaycastCurrentSprite();
                        break;
                    }
            }
        }
    }

    private void RaycastSprites()
    {
        Vector2 origin = _mainCamera.ScreenToWorldPoint(_touch.position);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.zero);
        if (hits.Length <= 0) return;

        // select top-most sprite renderer
        int maxSortingLayer = -1000;
        int topIndex = -1;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].collider.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
            {
                continue;
            }
            int sortingOrder = sr.sortingOrder;
            if (sortingOrder > maxSortingLayer)
            {
                maxSortingLayer = sortingOrder;
                topIndex = i;
            }
        }

        _selectedCollider = hits[topIndex].collider;
        _currentSpriteRenderer = _selectedCollider.GetComponent<SpriteRenderer>();

        int SpriteIndex = _currentSpriteRenderer.transform.GetSiblingIndex();
        int width = _currentSpriteRenderer.sprite.texture.width;
        int height = _currentSpriteRenderer.sprite.texture.height;
        int mipCount = _currentSpriteRenderer.sprite.texture.mipmapCount;
        TextureFormat textureFormat = _currentSpriteRenderer.sprite.texture.format;

        if (!_originalTextures.ContainsKey(SpriteIndex))
        {
            _originalTextures.Add(SpriteIndex, _currentSpriteRenderer.sprite.texture);
            _editedTextures.Add(SpriteIndex, new Texture2D(width, height, textureFormat, mipCount, false));

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, mipCount);
            rt.useMipMap = true;
            _renderTextures.Add(SpriteIndex, rt);
        }

        ColorSpriteAtPosition(_selectedCollider, hits[topIndex].point);
    }
    private void RaycastCurrentSprite()
    {
        Vector2 origin = _mainCamera.ScreenToWorldPoint(_touch.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.zero);
        if (hits.Length <= 0) return;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].collider.TryGetComponent(out SpriteRenderer spriteRenderer)) continue;
            if (spriteRenderer == _currentSpriteRenderer)
            {
                ColorSpriteAtPosition(hits[i].collider, hits[i].point);
                break;
            }
        }
    }
    private void ColorSpriteAtPosition(Collider2D collider, Vector2 hitPoint)
    {
        // Get the SpriteRenderer component from the collider
        SpriteRenderer spriteRenderer = collider.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        // Convert our hitPoint (World Space) to a texture point
        Vector2 texturePoint = WorldToTexturePoint(spriteRenderer, hitPoint);

        // Get the sprite and its texture
        Sprite sprite = spriteRenderer.sprite;

        int key = _currentSpriteRenderer.transform.GetSiblingIndex();
        Texture2D originalTexture = _originalTextures[key];

        // Create a new writable texture with the same dimensions as the original
        Texture2D tex = _editedTextures[key];

        if (sprite.texture != tex)
            Graphics.CopyTexture(sprite.texture, tex);

        _brushMaterial.SetTexture("_MainTex", tex);
        _brushMaterial.SetTexture("_Original", originalTexture);
        _brushMaterial.SetColor("_BrushColor", CurrentColor);
        _brushMaterial.SetFloat("_BrushSize", _brushSize);
        _brushMaterial.SetVector("_UVPosition", texturePoint / sprite.texture.width);


        RenderTexture rt = _renderTextures[key];

        Graphics.Blit(tex, rt, _brushMaterial);

        Graphics.CopyTexture(rt, tex);

        // takes approx 10ms on first click - Sprite.Create function

        if (!_sprites.ContainsKey(key))
        {
            // Create a new sprite from the modified texture
            Sprite newSprite = Sprite.Create(tex, sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
            // Add to dictionary
            _sprites.Add(key, newSprite);
        }
            spriteRenderer.sprite = _sprites[key];
    }

    private Vector2 WorldToTexturePoint(SpriteRenderer sr, Vector2 worldPos)
    {
        Vector2 texturePoint = sr.transform.InverseTransformPoint(worldPos);

        // Position between -5 and 5
        texturePoint.x /= sr.bounds.size.x;
        texturePoint.y /= sr.bounds.size.y;

        // Position between 0 & 1
        texturePoint += Vector2.one / 2;

        // Offset in Texture space
        texturePoint.x *= sr.sprite.rect.width;
        texturePoint.y *= sr.sprite.rect.height;
        // Position in Texture Space
        texturePoint.x += sr.sprite.rect.x;
        texturePoint.y += sr.sprite.rect.y;

        return texturePoint;
    }

    //private Vector2 WorldToTexturePoint(SpriteRenderer sr, Vector2 worldPos)
    //{
    //    // Convert world position to local space (relative to the SpriteRenderer)
    //    Vector2 localPoint = sr.transform.InverseTransformPoint(worldPos);

    //    // // Adjust for the sprite's pivot (pivot is in the range 0 to 1, adjust to local coordinate)
    //    // Vector2 pivotOffset = new Vector2(
    //    //     (sr.sprite.pivot.x / sr.sprite.rect.width) - 0.5f,
    //    //     (sr.sprite.pivot.y / sr.sprite.rect.height) - 0.5f
    //    // );
    //    // localPoint -= new Vector2(
    //    //     pivotOffset.x * sr.bounds.size.x,
    //    //     pivotOffset.y * sr.bounds.size.y
    //    // );

    //    // Adjust for the SpriteRenderer's scale
    //    localPoint.x /= sr.transform.localScale.x;
    //    localPoint.y /= sr.transform.localScale.y;

    //    // Normalize the local point to [0,1] range
    //    Vector2 normalizedPoint = new Vector2(
    //        (localPoint.x / sr.bounds.size.x) + 0.5f,
    //        (localPoint.y / sr.bounds.size.y) + 0.5f
    //    );

    //    // Convert normalized coordinates to texture coordinates
    //    Vector2 texturePoint = new Vector2(
    //        normalizedPoint.x * sr.sprite.rect.width,
    //        normalizedPoint.y * sr.sprite.rect.height
    //    );

    //    // Offset by the sprite's position in the texture atlas
    //    texturePoint += new Vector2(sr.sprite.rect.x, sr.sprite.rect.y);

    //    return texturePoint;
    //}

    //private void ColorSprite(SpriteRenderer spriteRenderer)
    //{
    //    Sprite sprite = spriteRenderer.sprite;
    //    Texture2D texture = new Texture2D(sprite.texture.width, sprite.texture.height);

    //    for (int x = 0; x < texture.width; x++)
    //    {
    //        for (int y = 0; y < texture.height; y++)
    //        {
    //            // 1 for black , 0 for white
    //            Color pixelColor = _color;
    //            Color spritePixel = sprite.texture.GetPixel(x, y);
    //            pixelColor.a = spritePixel.a;

    //            pixelColor *= spritePixel;

    //            texture.SetPixel(x, y, pixelColor);
    //        }
    //    }

    //    texture.Apply();

    //    Sprite newSprite = Sprite.Create(texture, sprite.rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
    //    spriteRenderer.sprite = newSprite;
    //}
}
