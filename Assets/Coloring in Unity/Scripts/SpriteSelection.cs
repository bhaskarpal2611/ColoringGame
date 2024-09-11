using System.Collections.Generic;

//using System.Drawing;
using UnityEngine;

public class SpriteSelection : MonoBehaviour
{
    [SerializeField] private Color _color;
    [SerializeField] private int brushSize = 10;
    [SerializeField] private Vector3 _parentScale;

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
    private List<Collider2D> _colliders;
    private Dictionary<int, Texture2D> _originalTextures = new();

    private void Start()
    {
        // get all colliders
        //_colliders = new List<Collider2D>();
        //for (int i = 0; i < transform.childCount; i++)
        //{
        //    Collider2D collider = transform.GetChild(i).GetComponent<Collider2D>();
        //    if(collider != null) _colliders.Add(collider);
        //}
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

        // Coloring - by enabling and disabling only first touched collider

        //if(_touch.phase == TouchPhase.Ended)
        //{
        //    Debug.Log("chk");
        //    foreach (Collider2D collider in _colliders)
        //    {
        //        if (_selectedCollider != collider)
        //        {
        //            collider.enabled = false;
        //        }
        //        else
        //        {
        //            collider.enabled = true;
        //        }
        //    }
        //} 

    }

    private void RaycastSprite()
    {
        Vector2 origin = Camera.main.ScreenToWorldPoint(_touch.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down);

        if (hit.collider == null) return;

        ColorSprite(hit.collider.GetComponent<SpriteRenderer>());
    }

    private void RaycastSprites()
    {
        Vector2 origin = Camera.main.ScreenToWorldPoint(_touch.position);

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
        if (!_originalTextures.ContainsKey(SpriteIndex))
        {
            _originalTextures.Add(SpriteIndex, _currentSpriteRenderer.sprite.texture);
        }

        ColorSpriteAtPosition(_selectedCollider, hits[topIndex].point);
    }
    private void RaycastCurrentSprite()
    {
        Vector2 origin = Camera.main.ScreenToWorldPoint(_touch.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.zero);
        if (hits.Length <= 0)
            return;
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
        Texture2D tex = new Texture2D(sprite.texture.width, sprite.texture.height, sprite.texture.format, sprite.texture.mipmapCount, true);
        //Debug.Log("sprite mipmaps: " + sprite.texture.mipmapCount);
        //Debug.Log("tex mipmaps: " + tex.mipmapCount);           
        Graphics.CopyTexture(sprite.texture, tex);

        // Square

        //// Paint on the texture within the brush size
        //for (int x = -brushSize / 2; x < brushSize / 2; x++)
        //{
        //    for (int y = -brushSize / 2; y < brushSize / 2; y++)
        //    {
        //        int pixelX = x + (int)texturePoint.x;
        //        int pixelY = y + (int)texturePoint.y;

        //        // Ensure the pixel is within the texture bounds - SQUARE SHAPE
        //        if (pixelX >= 0 && pixelX < tex.width && pixelY >= 0 && pixelY < tex.height)
        //        {
        //            // Get the color at the pixel and modify its alpha
        //            Color pixelColor = _color; // Replace with your desired color
        //            pixelColor.a = sprite.texture.GetPixel(pixelX, pixelY).a;

        //            pixelColor *= originalTexture.GetPixel(pixelX, pixelY);

        //            Color32 color = new Color(pixelColor.r, pixelColor.g, pixelColor.b, pixelColor.a);
        //            // Set the new color on the texture
        //            tex.SetPixel(pixelX, pixelY, pixelColor);
        //        }
        //    }
        //}

        // Circle

        for (int x = -brushSize / 2; x < brushSize / 2; x++)
        {
            for (int y = -brushSize / 2; y < brushSize / 2; y++)
            {
                int pixelX = x + (int)texturePoint.x;
                int pixelY = y + (int)texturePoint.y;

                Vector2 pixelPoint = new Vector2(pixelX, pixelY);

                if (Vector2.Distance(pixelPoint, texturePoint) > brushSize / 2)
                    continue;

                Color pixelColor = _color;
                pixelColor.a = sprite.texture.GetPixel(pixelX, pixelY).a;

                pixelColor *= originalTexture.GetPixel(pixelX, pixelY);

                tex.SetPixel(pixelX, pixelY, pixelColor);
            }
        }

        // Rounded Smooth Brush
        //for (int x = -brushSize / 2; x < brushSize / 2; x++)
        //{
        //    for (int y = -brushSize / 2; y < brushSize / 2; y++)
        //    {
        //        int pixelX = x + (int)texturePoint.x;
        //        int pixelY = y + (int)texturePoint.y;

        //        float squaredRadius = x * x + y * y;
        //        float factor = Mathf.Exp(-squaredRadius / brushSize);

        //        Color previousColor = sprite.texture.GetPixel(pixelX, pixelY);
        //        Color pixelColor = Color.Lerp(previousColor, _color, factor);

        //        // Keep the transparency
        //        pixelColor.a = previousColor.a;

        //        pixelColor *= originalTexture.GetPixel(pixelX, pixelY);

        //        tex.SetPixel(pixelX, pixelY, pixelColor);
        //    } 
        //}

        // Apply changes to the texture
        tex.Apply();

        // Create a new sprite from the modified texture and assign it back to the SpriteRenderer
        Sprite newSprite = Sprite.Create(tex, sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
        spriteRenderer.sprite = newSprite;
    }

    // private Vector2 WorldToTexturePoint(SpriteRenderer sr, Vector2 worldPos)
    // {
    //     Vector2 texturePoint = sr.transform.InverseTransformPoint(worldPos);

    //     // Position between -5 and 5
    //     texturePoint.x /= sr.bounds.size.x;
    //     texturePoint.y /= sr.bounds.size.y;

    //     // Position between 0 & 1
    //     texturePoint += Vector2.one / 2;

    //     // Offset in Texture space
    //     texturePoint.x *= sr.sprite.rect.width;
    //     texturePoint.y *= sr.sprite.rect.height;
    //     // Position in Texture Space
    //     texturePoint.x += sr.sprite.rect.x;
    //     texturePoint.y += sr.sprite.rect.y;

    //     return texturePoint;
    // }
    private Vector2 WorldToTexturePoint(SpriteRenderer sr, Vector2 worldPos)
    {
        // Convert world position to local space (relative to the SpriteRenderer)
        Vector2 localPoint = sr.transform.InverseTransformPoint(worldPos);

        // // Adjust for the sprite's pivot (pivot is in the range 0 to 1, adjust to local coordinate)
        // Vector2 pivotOffset = new Vector2(
        //     (sr.sprite.pivot.x / sr.sprite.rect.width) - 0.5f,
        //     (sr.sprite.pivot.y / sr.sprite.rect.height) - 0.5f
        // );
        // localPoint -= new Vector2(
        //     pivotOffset.x * sr.bounds.size.x,
        //     pivotOffset.y * sr.bounds.size.y
        // );

        // Adjust for the SpriteRenderer's scale
        localPoint.x /= sr.transform.localScale.x;
        localPoint.y /= sr.transform.localScale.y;

        // Normalize the local point to [0,1] range
        Vector2 normalizedPoint = new Vector2(
            (localPoint.x / sr.bounds.size.x) + 0.5f,
            (localPoint.y / sr.bounds.size.y) + 0.5f
        );

        // Convert normalized coordinates to texture coordinates
        Vector2 texturePoint = new Vector2(
            normalizedPoint.x * sr.sprite.rect.width,
            normalizedPoint.y * sr.sprite.rect.height
        );

        // Offset by the sprite's position in the texture atlas
        texturePoint += new Vector2(sr.sprite.rect.x, sr.sprite.rect.y);

        return texturePoint;
    }

    private void ColorSprite(SpriteRenderer spriteRenderer)
    {
        Sprite sprite = spriteRenderer.sprite;
        Texture2D texture = new Texture2D(sprite.texture.width, sprite.texture.height);

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                // 1 for black , 0 for white

                Color pixelColor = _color;
                Color spritePixel = sprite.texture.GetPixel(x, y);
                pixelColor.a = spritePixel.a;

                pixelColor *= spritePixel;

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        Sprite newSprite = Sprite.Create(texture, sprite.rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
        spriteRenderer.sprite = newSprite;
    }
}
