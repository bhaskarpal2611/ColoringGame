//using TMKOC.Cases_of_Popatlal;
using UnityEngine;
using UnityEngine.UI;

public class BreadPainter : MonoBehaviour
{
    [Header("Setup")]
    //public BunMinigame bunMinigame;
    public float percentage = 80f;
    public Texture2D maskTexture;       // Assign your cutout PNG
    public Camera uiCamera;
    public RawImage paintSurface;
    public Texture2D brushTexture;
    public Color brushColor = Color.black;
    public int brushSize = 32;

    private RenderTexture rt;
    private RectTransform rectTransform;

    // Tracking arrays
    private bool[,] paintedMap;
    private int totalMaskPixels;
    private int paintedPixels;

    private int texSize = 512; // analysis resolution (not the paint RT size)

    void Start()
    {
        uiCamera = Camera.main;
        rectTransform = paintSurface.rectTransform;

        // Create paint RenderTexture (for visuals)
        rt = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        paintSurface.material.SetTexture("_PaintTex", rt);

        // Initialize tracking grid
        paintedMap = new bool[texSize, texSize];
        totalMaskPixels = 0;
        paintedPixels = 0;

        // Count paintable mask pixels once
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                Color maskPixel = maskTexture.GetPixelBilinear((float)x / texSize, (float)y / texSize);
                if (maskPixel.a > 0.1f)
                    totalMaskPixels++;
            }
        }

        Debug.Log("Total mask pixels: " + totalMaskPixels);
    }

    public void ClearPaint()
    {
        if (paintedMap == null)
            return;
       
        // 1. Clear the RenderTexture (visual)
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        // 2. Reset tracking map (logic)
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                paintedMap[x, y] = false;
            }
        }

        paintedPixels = 0;

        Debug.Log("Paint cleared!");
    }

    void Update()
    {
        //if (Knife.KnifeTaken == false)
            //return;
        if (Input.GetMouseButton(0))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition, uiCamera, out localPoint))
            {
                Vector2 uv = new Vector2(
                    (localPoint.x + rectTransform.rect.width * 0.5f) / rectTransform.rect.width,
                    (localPoint.y + rectTransform.rect.height * 0.5f) / rectTransform.rect.height
                );

                int px = (int)(uv.x * texSize);
                int py = (int)(uv.y * texSize);

                // Draw to RT (visual)
                int rx = (int)(uv.x * rt.width);
                int ry = (int)(uv.y * rt.height);
                DrawBrush(rx, ry);

                // Update painted map (logic)
                UpdatePaintedMap(px, py);
            }
        }
    }

    void DrawBrush(int x, int y)
    {
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, rt.width, rt.height, 0);

        Graphics.DrawTexture(
            new Rect(x - brushSize / 2, rt.height - y - brushSize / 2, brushSize, brushSize),
            brushTexture,
            new Material(Shader.Find("Unlit/Transparent")) { color = brushColor }
        );

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    void UpdatePaintedMap(int cx, int cy)
    {
        int radius = Mathf.CeilToInt((float)brushSize * texSize / rt.width / 2f);

        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || y < 0 || x >= texSize || y >= texSize) continue;

                if (!paintedMap[x, y])
                {
                    Color maskPixel = maskTexture.GetPixelBilinear((float)x / texSize, (float)y / texSize);
                    if (maskPixel.a > 0.1f)
                    {
                        paintedMap[x, y] = true;
                        paintedPixels++;
                    }
                }
            }
        }

        float percent = (float)paintedPixels / totalMaskPixels * 100f;
        Debug.Log("Painted: " + percent.ToString("F2") + "%");

        if (percent >= percentage)
        {
            //bunMinigame.ActivateEat();
            Debug.Log("✅ Painted!");
        }
    }

}
