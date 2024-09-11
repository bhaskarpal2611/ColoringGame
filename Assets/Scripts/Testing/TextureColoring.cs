using UnityEngine;

public class TextureColoring : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer

    void Start()
    {
        // Create a texture from the sprite, matching its pixel density
        Texture2D texture = CreateTextureFromSprite(spriteRenderer.sprite);

        // Color each pixel in the texture
        ColorEachPixel(texture);

        // Apply the texture back to the sprite
        ApplyTextureToSprite(texture);
    }

    Texture2D CreateTextureFromSprite(Sprite sprite)
    {
        // Get the original texture of the sprite
        Texture2D originalTexture = sprite.texture;

        // Create a new Texture2D with the same width and height as the original sprite's texture
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);

        // Copy the pixels from the original texture based on the sprite's textureRect
        Color[] pixels = originalTexture.GetPixels(
            (int)sprite.textureRect.x,
            (int)sprite.textureRect.y,
            (int)sprite.textureRect.width,
            (int)sprite.textureRect.height
        );

        // Set those pixels into the new texture
        texture.SetPixels(pixels);
        texture.Apply(); // Apply the pixel data

        return texture;
    }

    void ColorEachPixel(Texture2D texture)
    {
        // Loop through each pixel in the texture
        for (int x = 0; x < texture.width; x++)
        {
            // Generate a random color for each pixel vertical line
            Color randomColor = new Color(Random.value, Random.value, Random.value);
            for (int y = 0; y < texture.height; y++)
            {
                // Set the pixel color at the current coordinate
                texture.SetPixel(x, y, randomColor);
            }
        }

        // Apply the changes to the texture (this is important!)
        texture.Apply();
    }

    void ApplyTextureToSprite(Texture2D texture)
    {
        // Create a new sprite using the modified texture, with the same dimensions and pivot as the original sprite
        spriteRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), // Pivot point can match the original
            spriteRenderer.sprite.pixelsPerUnit // Maintain the original sprite's pixelsPerUnit
        );
    }
}
