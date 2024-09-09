using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;


public enum PaintMode
{
    None,
    Paint,
    Erase,
}

public class SpriteTextureColoring : MonoBehaviour
{
    public SpriteRenderer image;         // The SpriteRenderer to paint on
    public SpriteRenderer[] layers;
    public Texture2D brushTexture;       // The texture used as a brush
    public float brushSize = 5f;         // Size of the painting brush
    public PaintMode _paintMode = PaintMode.Paint;

    private Texture2D texture;           // Texture2D to modify
    private Sprite sprite;               // Original sprite

    private Camera _mainCamera;

    private Color[] _pixels;
    private SpriteRenderer _activeLayer;

    private void Start()
    {
        _mainCamera = Camera.main;

        // Get the sprite from the SpriteRenderer
        sprite = image.sprite;

        Debug.Log($"layer: {image.sortingLayerName}");

        // Check if the sprite's texture is readable
        if (!sprite.texture.isReadable)
        {
            Debug.LogError("Texture is not readable. Enable Read/Write in the texture import settings.");
            return;
        }

        // Create a new writable Texture2D with the same size as the sprite's texture
        texture = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);

        // Copy the pixel data from the original texture to the new one
        _pixels = sprite.texture.GetPixels();
        //for (int i = 0; i < _pixels.Length; i++)
        //{
        //    _pixels[i].a = 0f;
        //}

        texture.SetPixels(_pixels);
        texture.Apply();

        // Create a new sprite using the writable texture and assign it to the SpriteRenderer
        Sprite newSprite = Sprite.Create(texture, sprite.rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
        image.sprite = newSprite;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only select the layer when the touch begins
            if (touch.phase == TouchPhase.Began)
            {
                SelectLayer(touch.position);
            }

            // Only paint if the active layer is selected and touch is moving or stationary
            if (_activeLayer != null && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Vector3 screenPosition = touch.position;
                PaintAt(screenPosition);
            }
        }
    }

    private void PaintAt(Vector2 screenPosition)
    {
        // Convert screen position to world position
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Ensure z is set to 0 for 2D

        // Convert world position to local position of the sprite (relative to its pivot)
        Vector2 localPosition = image.transform.InverseTransformPoint(worldPosition);

        // Convert local position to normalized UV coordinates (0 to 1)
        float pivotOffsetX = sprite.pivot.x / sprite.rect.width;
        float pivotOffsetY = sprite.pivot.y / sprite.rect.height;
        float normalizedX = (localPosition.x / sprite.bounds.size.x) + pivotOffsetX;
        float normalizedY = (localPosition.y / sprite.bounds.size.y) + pivotOffsetY;

        // Convert normalized UV coordinates to pixel coordinates
        int centerX = Mathf.FloorToInt(normalizedX * texture.width);
        int centerY = Mathf.FloorToInt(normalizedY * texture.height);

        // Ensure the pixel is inside the texture bounds
        if (centerX >= 0 && centerX < texture.width && centerY >= 0 && centerY < texture.height)
        {
            // Paint the brush texture
            PaintWithBrush(centerX, centerY);

            // Apply the changes to the texture
            texture.Apply();
        }
    }

    private void PaintWithBrush(int centerX, int centerY)
    {
        Debug.Log($"Painting on layer: {_activeLayer.name}");

        int brushRadius = Mathf.FloorToInt(brushSize / 2);
        int brushWidth = brushTexture.width;
        int brushHeight = brushTexture.height;

        // Get the pixels of both the main texture and the brush texture at once
        Color32[] texturePixels = texture.GetPixels32();
        Color32[] brushPixels = brushTexture.GetPixels32();

        // Iterate over the brush area
        for (int i = -brushRadius; i <= brushRadius; i++)
        {
            for (int j = -brushRadius; j <= brushRadius; j++)
            {
                int px = centerX + i;
                int py = centerY + j;

                // Check if the pixel is within bounds of the main texture
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    // Calculate brush position
                    int brushX = Mathf.FloorToInt((i / (float)brushRadius) * brushWidth / 2 + brushWidth / 2);
                    int brushY = Mathf.FloorToInt((j / (float)brushRadius) * brushHeight / 2 + brushHeight / 2);

                    // Check if the brush texture coordinate is within bounds
                    if (brushX >= 0 && brushX < brushWidth && brushY >= 0 && brushY < brushHeight)
                    {
                        // Calculate the index in the arrays for both the main texture and the brush
                        int texturePixelIndex = py * texture.width + px;
                        int brushPixelIndex = brushY * brushWidth + brushX;

                        // Get the brush pixel color
                        Color32 brushColor = brushPixels[brushPixelIndex];

                        // If the brush pixel is not transparent, apply it to the texture
                        if (brushColor.a > 0)
                        {
                            // Set the alpha to 255 for visibility, you can blend colors here if needed
                            if (_paintMode == PaintMode.Paint)
                            {
                                texturePixels[texturePixelIndex].a = 255;
                            }
                            if (_paintMode == PaintMode.Erase)
                            {
                                texturePixels[texturePixelIndex].a = 0;
                            }
                        }
                    }
                }
            }
        }

        // Apply the modified pixel data back to the texture
        texture.SetPixels32(texturePixels);
        texture.Apply();
    }
    private void SelectLayer(Vector3 screenPosition)
    {
        //Debug.Log("check");
        // Convert screen position to world position
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Ensure z is set to 0 for 2D

        // Check which sprite is under the touch
        foreach (var layer in layers)
        {
            bool value = IsPointInSprite(worldPosition, layer);
            if (value)
            {
                Debug.Log("bool: " + value);
                // Set this layer as the active layer to paint on
                _activeLayer = layer;
                texture = _activeLayer.sprite.texture;
                Debug.Log("Selected layer: " + _activeLayer.name);
                return;
            }
        }

        // If no layer is selected, clear the active layer
        _activeLayer = null; 
        Debug.Log("No layer selected.");
    }

        //private bool IsPointInSprite(Vector3 worldPosition, SpriteRenderer spriteRenderer)
        //{
        //    // Convert world position to local position of the sprite (relative to its pivot)
        //    Vector2 localPosition = spriteRenderer.transform.InverseTransformPoint(worldPosition);

        //    // Check if the local position is within the sprite bounds
        //    Sprite sprite = spriteRenderer.sprite;
        //    Rect spriteRect = sprite.rect;
        //    Vector2 pivot = sprite.pivot;
        //    Vector2 size = spriteRect.size;
        //    Vector2 spriteSize = spriteRenderer.bounds.size;

        //    // Normalize localPosition
        //    Vector2 normalizedPosition = new Vector2(
        //        (localPosition.x + spriteSize.x / 2) / spriteSize.x,
        //        (localPosition.y + spriteSize.y / 2) / spriteSize.y
        //    );

        //    // Convert normalized position to pixel coordinates
        //    int textureX = Mathf.FloorToInt(normalizedPosition.x * texture.width);
        //    int textureY = Mathf.FloorToInt(normalizedPosition.y * texture.height);

        //    // Check if the pixel is within bounds of the texture
        //    if (textureX >= 0 && textureX < texture.width && textureY >= 0 && textureY < texture.height)
        //    {
        //        // Check if the texture pixel is not transparent
        //        Color pixelColor = sprite.texture.GetPixel(textureX, textureY);
        //        return pixelColor.a > 0; // If alpha is greater than 0, it means it's not transparent
        //    }

        //    return false;
        //}

    private bool IsPointInSprite(Vector3 worldPosition, SpriteRenderer spriteRenderer)
    {
        // Convert world position   to local position relative to the sprite's transform
        Vector2 localPosition = spriteRenderer.transform.InverseTransformPoint(worldPosition);

        // Get the sprite's rect and size
        Sprite sprite = spriteRenderer.sprite;
        Rect spriteRect = sprite.rect;

        // Get the bounds of the sprite
        Vector2 spriteSize = spriteRenderer.bounds.size;

        // Check if the point is within the local bounds of the sprite
        if (localPosition.x < -spriteSize.x / 2 || localPosition.x > spriteSize.x / 2 ||
            localPosition.y < -spriteSize.y / 2 || localPosition.y > spriteSize.y / 2)
        {
            return false; // Point is outside the sprite bounds
        }

        // Normalize local position to UV coordinates (0 to 1 range)
        float normalizedX = (localPosition.x + spriteSize.x / 2) / spriteSize.x;
        float normalizedY = (localPosition.y + spriteSize.y / 2) / spriteSize.y;

        // Convert normalized UV coordinates to pixel coordinates in the sprite's texture
        int textureX = Mathf.FloorToInt(normalizedX * sprite.texture.width);
        int textureY = Mathf.FloorToInt(normalizedY * sprite.texture.height);

        // Ensure the pixel is within the bounds of the texture
        if (textureX >= 0 && textureX < sprite.texture.width && textureY >= 0 && textureY < sprite.texture.height)
        {
            // Now instead of checking transparency, we return true because the point is in the sprite bounds
            return true;
        }

        return false;
    }


    //private Color BlendColor(Color baseColor, Color brushColor)
    //{
    //    // Blend the brush color with the base color (simple alpha blending)
    //    float alpha = brushColor.a;
    //    return new Color(
    //        brushColor.r * alpha + baseColor.r * (1 - alpha),
    //        brushColor.g * alpha + baseColor.g * (1 - alpha),
    //        brushColor.b * alpha + baseColor.b * (1 - alpha),
    //        Mathf.Max(baseColor.a, brushColor.a)
    //    );
    //}
}