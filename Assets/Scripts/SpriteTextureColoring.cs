using UnityEngine;
using System.Collections.Generic;

public enum PaintMode
{
    None,
    Paint,
    Erase,
}

public class SpriteTextureColoring : MonoBehaviour
{
    public SpriteRenderer[] layers;          // Array of SpriteRenderers representing different parts/layers
    public Texture2D brushTexture;           // The texture used as a brush
    public float brushSize = 5f;             // Size of the painting brush
    public PaintMode _paintMode = PaintMode.Paint;

    private Dictionary<SpriteRenderer, Texture2D> spriteTextures = new Dictionary<SpriteRenderer, Texture2D>();  // Stores textures for each layer
    private Dictionary<SpriteRenderer, bool[,]> spriteAlphaMasks = new Dictionary<SpriteRenderer, bool[,]>();  // Stores alpha masks for each layer
    private Camera _mainCamera;

    private SpriteRenderer _activeLayer;

    private void Start()
    {
        _mainCamera = Camera.main;

        // Initialize textures for each layer and set up alpha masks
        for (int i = 0; i < layers.Length; i++)
        {
            SpriteRenderer layer = layers[i];
            Texture2D alphaMask = layer.sprite.texture; // Corresponding alpha mask for the layer

            // Get the sprite from the SpriteRenderer
            Sprite sprite = layer.sprite;

            // Check if the sprite's texture is readable
            if (!sprite.texture.isReadable || !alphaMask.isReadable)
            {
                Debug.LogError("Texture or AlphaMask is not readable. Enable Read/Write in the texture import settings.");
                return;
            }

            // Create a new writable Texture2D with the same size as the sprite's texture
            Texture2D newTexture = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);


            // Copy the pixel data to pixels array and store alpha mask in 2d bool array
            spriteAlphaMasks[layer] = GenerateAlphaMask(alphaMask, out Color32[] pixels);

            for (int j = 0; j < pixels.Length; j++)
            {
                pixels[j].a = 0;  // Set the initial alpha to 0 for transparency
            }

            newTexture.SetPixels32(pixels);
            newTexture.Apply();

            // Create a new sprite using the writable texture and assign it to the SpriteRenderer
            Sprite newSprite = Sprite.Create(newTexture, sprite.rect,
                new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
            layer.sprite = newSprite;

            // Store the texture and alpha mask in dictionaries for this sprite
            spriteTextures[layer] = newTexture;
        }
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

    private bool[,] GenerateAlphaMask(Texture2D texture, out Color32[] pixels)
    {
        pixels = texture.GetPixels32();

        bool[,] mask = new bool[texture.width, texture.height];

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                // Calculate the correct index in the 1D array
                int index = y * texture.width + x;

                // Set true if the pixel is non-transparent (alpha > threshold)
                mask[x, y] = pixels[index].a > 0.1f;
            }
        }
        return mask;
    }

    private void PaintAt(Vector2 screenPosition)
    {
        // Convert screen position to world position
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Ensure z is set to 0 for 2D

        // Convert world position to local position of the sprite (relative to its pivot)
        Vector2 localPosition = _activeLayer.transform.InverseTransformPoint(worldPosition);

        // Convert local position to normalized UV coordinates (0 to 1)
        Sprite sprite = _activeLayer.sprite;
        float pivotOffsetX = sprite.pivot.x / sprite.rect.width;
        float pivotOffsetY = sprite.pivot.y / sprite.rect.height;
        float normalizedX = (localPosition.x / sprite.bounds.size.x) + pivotOffsetX;
        float normalizedY = (localPosition.y / sprite.bounds.size.y) + pivotOffsetY;

        // Convert normalized UV coordinates to pixel coordinates
        Texture2D activeTexture = spriteTextures[_activeLayer];
        int centerX = Mathf.FloorToInt(normalizedX * activeTexture.width);
        int centerY = Mathf.FloorToInt(normalizedY * activeTexture.height);

        // Ensure the pixel is inside the texture bounds
        if (centerX >= 0 && centerX < activeTexture.width && centerY >= 0 && centerY < activeTexture.height)
        {
            // Check the alpha mask to see if the area is paintable
            if (spriteAlphaMasks[_activeLayer][centerX, centerY])
            {
                // Paint the brush texture
                PaintWithBrush(centerX, centerY, activeTexture);

                // Apply the changes to the texture
                activeTexture.Apply();
            }
        }
    }

    private void PaintWithBrush(int centerX, int centerY, Texture2D activeTexture)
    {
        int brushRadius = Mathf.FloorToInt(brushSize / 2);
        int brushWidth = brushTexture.width;
        int brushHeight = brushTexture.height;
        int activeTextureWidth = activeTexture.width;
        int activeTextureHeight = activeTexture.height;

        // Get the pixels of both the main texture and the brush texture
        Color32[] texturePixels = activeTexture.GetPixels32();
        Color32[] brushPixels = brushTexture.GetPixels32();

        // Iterate over the brush area
        for (int i = -brushRadius; i <= brushRadius; i++)
        {
            for (int j = -brushRadius; j <= brushRadius; j++)
            {
                int px = centerX + i;
                int py = centerY + j;

                // Check if the pixel is within bounds of the main texture
                if (px >= 0 && px < activeTextureWidth && py >= 0 && py < activeTextureHeight)
                {
                    // Calculate brush position
                    int brushX = Mathf.FloorToInt((i / (float)brushRadius) * brushWidth / 2 + brushWidth / 2);
                    int brushY = Mathf.FloorToInt((j / (float)brushRadius) * brushHeight / 2 + brushHeight / 2);

                    // Check if the brush texture coordinate is within bounds
                    if (brushX >= 0 && brushX < brushWidth && brushY >= 0 && brushY < brushHeight)
                    {
                        // Calculate the index in the arrays for both the main texture and the brush
                        int texturePixelIndex = py * activeTextureWidth + px;
                        int brushPixelIndex = brushY * brushWidth + brushX;

                        // If the brush pixel is not transparent, apply it to the texture
                        if (brushPixels[brushPixelIndex].a > 0)
                        {
                            if (_paintMode == PaintMode.Paint)
                            {
                                texturePixels[texturePixelIndex].a = 255;  // Make it visible
                            }
                            else if (_paintMode == PaintMode.Erase)
                            {
                                texturePixels[texturePixelIndex].a = 0;  // Make it transparent
                            }
                        }
                    }
                }
            }
        }

        // Apply the modified pixel data back to the texture
        activeTexture.SetPixels32(texturePixels);
    }

    private void SelectLayer(Vector3 screenPosition)
    {
        // Convert screen position to world position
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Ensure z is set to 0 for 2D

        // Check which sprite is under the touch
        foreach (var layer in layers)
        {
            if (IsClickOnSprite(layer, worldPosition))
            {
                _activeLayer = layer;
                Debug.Log("Selected layer: " + _activeLayer.name);
                return;
            }
        }

        // If no layer is selected, clear the active layer
        _activeLayer = null;
        Debug.Log("No layer selected.");
    }

    private bool IsClickOnSprite(SpriteRenderer spriteRenderer, Vector2 worldPosition)
    {
        // Convert world position to local position within the sprite
        Vector2 localPos = spriteRenderer.transform.InverseTransformPoint(worldPosition);

        // Convert local position to texture space
        Vector2 pivotBasedPos = new Vector2(
            (localPos.x + spriteRenderer.sprite.bounds.extents.x) / spriteRenderer.sprite.bounds.size.x,
            (localPos.y + spriteRenderer.sprite.bounds.extents.y) / spriteRenderer.sprite.bounds.size.y
        );

        Texture2D texture = spriteRenderer.sprite.texture;
        Vector2Int pixelPos = new Vector2Int(
            Mathf.RoundToInt(pivotBasedPos.x * texture.width),
            Mathf.RoundToInt(pivotBasedPos.y * texture.height)
        );

        // Check if the pixel is inside the texture bounds
        if (pixelPos.x < 0 || pixelPos.x >= texture.width || pixelPos.y < 0 || pixelPos.y >= texture.height)
        {
            return false;
        }

        // Use the pre-generated alpha mask for hit detection
        return spriteAlphaMasks[spriteRenderer][pixelPos.x, pixelPos.y];    

    }
}
        
