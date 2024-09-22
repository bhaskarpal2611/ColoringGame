using UnityEngine;
using System.Collections.Generic;

namespace ColorSwipeGame
{
    // old
    public class PaintController
    {
        private InitPaintData _paintData;
        private Stack<UndoData> _lastEditedTextures = new();
        private Material _brushMaterial;
        private Vector2 _lastTouchPosition;
        private bool _firstTouch = true;
        private Transform _spritesParent;
        private SpriteRenderer _currentSpriteRenderer;
        private Collider2D _currentCollider;
        private int _brushTextureIndex;
        private RaycastHit2D[] _hits;
        private List<bool> _isEdited = new();
        private Dictionary<int, Texture2D> _editedTextures = new Dictionary<int, Texture2D>();
        private Dictionary<int, Sprite> _originalSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

        private readonly int BrushColorProperty = Shader.PropertyToID("_BrushColor");
        private readonly int BrushSizeProperty = Shader.PropertyToID("_BrushSize");
        private readonly int BrushTextureProperty = Shader.PropertyToID("_BrushTexture");
        private readonly int UVPositionProperty = Shader.PropertyToID("_UVPosition");
        private readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private readonly int OriginalProperty = Shader.PropertyToID("_Original");


        // CONSTRUCTOR
        public PaintController(InitPaintData paintData)
        {
            _paintData = paintData;
            _hits = new RaycastHit2D[paintData.MaxHitColliders];
            PreWarmShaders();
            SetDefaultColor();

            // _currentRenderTex = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGB32);
        }

        public void BeginDrag(Vector2 worldPosition)
        {
            _firstTouch = true;
            RaycastSprites(worldPosition);
        }

        public void ContinueDrag(Vector2 worldPosition)
        {
            if (!_currentSpriteRenderer) return;
            DrawLines(worldPosition);
        }

        public void EndDrag()
        {
            if (_currentSpriteRenderer)
            {
                if (_currentCollider != null)
                {
                    _currentCollider.enabled = true;
                    _currentCollider = null;
                }
                _currentSpriteRenderer = null;
            }
        }

        public void ClearMemory()
        {
            CleanupResources();
        }

        public void SetDefaultColor()
        {
            SetColorMode();
            _brushMaterial.SetColor(BrushColorProperty, _paintData.DefaultBrushColor);
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
        }

        public void SetColor(Color color)
        {
            CurrentBrushColor = color;
            SetColorMode();
        }

        public void SetTexture(int index, Color color)
        {
            BrushTextureIndex = index;
            CurrentBrushColor = color;
            SetTextureMode();
        }

        public void SetErase() => SetEraseMode();

        public void SetDefaultTexture(int index)
        {
            SetTextureMode();
            BrushTextureIndex = index;
            _brushMaterial.SetColor(BrushColorProperty, Color.white);
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
        }

        // Properties
        public Color CurrentBrushColor
        {
            get => _paintData.DefaultBrushColor;
            set
            {
                _paintData.DefaultBrushColor = value;
                _brushMaterial.SetColor(BrushColorProperty, _paintData.DefaultBrushColor);
            }
        }

        public int BrushTextureIndex
        {
            get => _brushTextureIndex;
            set
            {
                _brushTextureIndex = value;
                _brushMaterial.SetTexture(BrushTextureProperty, _paintData.Textures[_brushTextureIndex]);
            }
        }

        public void SetBrushScale(float value)
        {
            _paintData.BrushSize = value;
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
        }

        public void ClearPainting()
        {
            RestoreOriginalTextures();
            // ReapplySpritesToRenderers();

            // also clear undo stack
            _lastEditedTextures.Clear();
        }

        public void PerformUndo()
        {
            if (_lastEditedTextures.Count > 0)
            {
                UndoData undoData = _lastEditedTextures.Pop();
                Graphics.CopyTexture(undoData.Texture, _editedTextures[undoData.Index]);
                Sprite newSprite = Sprite.Create(_editedTextures[undoData.Index], undoData.Rect, new(0.5f, 0.5f), undoData.PPU);
                _sprites[undoData.Index] = newSprite;
                // Ensure that the currentSpriteRenderer is updated to the sprite that was just restored
                _currentSpriteRenderer = _spritesParent.GetChild(undoData.Index).GetComponent<SpriteRenderer>();
                _currentSpriteRenderer.sprite = newSprite;
            }
        }

        public void InitializeLevel(Transform spritesParent)
        {
            _spritesParent = spritesParent;
            foreach (Transform spriteTransform in _spritesParent)
            {
                InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>());
            }
        }

        /* PRIVATE METHODS */

        private void SetColorMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Color]);
        private void SetEraseMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Erase]);
        private void SetTextureMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Texture]);

        private void SetPaintMode(Material material)
        {
            _brushMaterial = material;
            _brushMaterial.SetColor(BrushColorProperty, CurrentBrushColor);
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
        }

        private void InitializeSprite(SpriteRenderer spriteRenderer)
        {
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = spriteRenderer.sprite;
            Texture2D originalTexture = originalSprite.texture;

            _originalSprites.Add(spriteIndex, originalSprite);
            _isEdited.Add(false);
            _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount, false));
            // _renderTextures.Add(spriteIndex, new RenderTexture(originalTexture.width, originalTexture.height, 0, RenderTextureFormat.ARGB32, originalTexture.mipmapCount)
            // {
            //     useMipMap = false,
            //     autoGenerateMips = false
            // });
        }

        private void PreWarmShaders()
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            RenderTexture tempDestRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            RenderTexture prevActiveRT = RenderTexture.active;

            foreach (Material material in _paintData.BrushMaterials)
            {
                PreWarmMaterial(material, tempRT, tempDestRT);
            }

            RenderTexture.active = prevActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.ReleaseTemporary(tempDestRT);
        }

        private void PreWarmMaterial(Material material, RenderTexture source, RenderTexture destination)
        {
            SetPaintMode(material);
            Graphics.Blit(source, destination, _brushMaterial);
        }

        private void RaycastSprites(Vector2 worldPosition)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPosition, Vector2.zero, _hits);
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

            SaveCurrentTextureState();

            ColorSpriteAtPosition(_hits[topIndex].point);
        }

        private void DrawLines(Vector2 currentTouchPosition)
        {
            if (_firstTouch)
            {
                _lastTouchPosition = currentTouchPosition;
                _firstTouch = false;
            }

            float distance = Vector2.Distance(_lastTouchPosition, currentTouchPosition);
            int steps = Mathf.CeilToInt(distance / (_paintData.BrushSize * 0.5f));
            int currentSteps = 0;
            const int blitThreshold = 10;

            for (int i = 0; i <= steps; i++)
            {
                currentSteps++;
                if (currentSteps >= blitThreshold)
                {
                    currentSteps = 0;
                    Vector2 interpolatedPoint = Vector2.Lerp(_lastTouchPosition, currentTouchPosition, i / (float)steps);
                    ColorSpriteAtPosition(interpolatedPoint);
                }
            }
            _lastTouchPosition = currentTouchPosition;
        }

        private void ColorSpriteAtPosition(Vector2 worldPosition)
        {
            if (_currentSpriteRenderer == null) return;

            Vector2 texturePoint = WorldToTexturePoint(worldPosition);
            Sprite sprite = _currentSpriteRenderer.sprite;
            int key = _currentSpriteRenderer.transform.GetSiblingIndex();

            if (!_editedTextures.TryGetValue(key, out Texture2D editedTexture))
            {
                editedTexture = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);
                _editedTextures[key] = editedTexture;
                _isEdited[key] = true;
            }

            if (sprite.texture != editedTexture)
                Graphics.CopyTexture(sprite.texture, editedTexture);

            _brushMaterial.SetVector(UVPositionProperty, texturePoint / sprite.texture.width);
            _brushMaterial.SetTexture(MainTexProperty, editedTexture);
            _brushMaterial.SetTexture(OriginalProperty, _originalSprites[key].texture);

            RenderTexture renderTexture = RenderTexture.GetTemporary(editedTexture.width, editedTexture.height, 0, RenderTextureFormat.ARGB32);

            Graphics.Blit(editedTexture, renderTexture, _brushMaterial);
            Graphics.CopyTexture(renderTexture, editedTexture);

            // ReturnRenderTextureToPool(renderTexture);
            renderTexture.DiscardContents();
            RenderTexture.ReleaseTemporary(renderTexture);

            if (!_sprites.TryGetValue(key, out Sprite _))
            {
                Sprite newSprite = Sprite.Create(editedTexture, sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
                _sprites.Add(key, newSprite);

                // Create original clean version of the sprite
                Texture2D newTex = new Texture2D(_originalSprites[key].texture.width, _originalSprites[key].texture.height, TextureFormat.RGBA32, false);
                Graphics.CopyTexture(_originalSprites[key].texture, newTex);
            }
            _currentSpriteRenderer.sprite = _sprites[key];
        }


        // POOL - GET and RETURN
        // private RenderTexture GetRenderTextureFromPool(int width, int height)
        // {
        //     if (_renderTexturePool.Count > 0)
        //     {
        //         RenderTexture rt = _renderTexturePool.Pop();
        //         if (rt.width == width && rt.height == height)
        //             return rt;
        //         else
        //             rt.Release();
        //     }
        //     return new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        // }

        // private void ReturnRenderTextureToPool(RenderTexture rt)
        // {
        //     _renderTexturePool.Push(rt);
        // }

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


        // Save State - UNDO _ FUNCTION
        private void SaveCurrentTextureState()
        {
            int index = _currentSpriteRenderer.transform.GetSiblingIndex();

            if (_isEdited[index])
            {
                Texture2D newTex = new(_editedTextures[index].width, _editedTextures[index].height, TextureFormat.RGBA32, 1, false);
                Graphics.CopyTexture(_editedTextures[index], newTex);
                _lastEditedTextures.Push(new UndoData(index, newTex, _currentSpriteRenderer.sprite.rect, _currentSpriteRenderer.sprite.pixelsPerUnit));
            }
        }

        // CLEAR _ Function
        private void CleanupResources()
        {
            foreach (var texture in _editedTextures.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
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
            Debug.Log(_spritesParent);
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
    }

    [System.Serializable]
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