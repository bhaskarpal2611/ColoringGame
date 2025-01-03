using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace ColorSwipeGame
{
    public class PaintController
    {
        private int _brushTextureIndex;
        private bool _firstTouch = true;
        private bool _isDragging = false;
        private Vector2 _lastTouchPosition;

        private Material _brushMaterial;
        private Transform _spritesParent;
        private SpriteRenderer _drawingSR;
        private SpriteRenderer _currentSpriteRenderer;
        private Collider2D _currentCollider;
        private RenderTexture _currentRT;

        private InitPaintData _paintData;
        private PenSelectionHandler _penSelectionHandler;
        private TimeKeeper _timeKeeper;

        private List<bool> _isEdited = new();
        private Dictionary<int, Texture2D> _editedTextures = new Dictionary<int, Texture2D>();
        private Dictionary<int, Sprite> _originalSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _editedSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _savedSprites = new();
        private int _currentSRIndex;
        private const int BLIT_THRESHOLD = 10;
        private readonly RaycastHit2D[] _hits;
        private readonly int BrushColorProperty = Shader.PropertyToID("_BrushColor");
        private readonly int BrushSizeProperty = Shader.PropertyToID("_BrushSize");
        private readonly int BrushTextureProperty = Shader.PropertyToID("_BrushTexture");
        private readonly int UVPositionProperty = Shader.PropertyToID("_UVPosition");
        private readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private readonly int OriginalProperty = Shader.PropertyToID("_Original");

        // CONSTRUCTOR
        public PaintController(InitPaintData paintData, PenSelectionHandler penPanelHandler, TimeKeeper timer)
        {
            _paintData = paintData;
            _hits = new RaycastHit2D[paintData.MaxHitColliders];
            _penSelectionHandler = penPanelHandler;
            _timeKeeper = timer;
            PreWarmShaders();
            SetDefaultColor();
        }

        public void BeginDrag(Vector2 worldPosition)
        {
            _firstTouch = true;
            //_currentRT = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGB32);
            RaycastSprites(worldPosition);
            AudioManager.Instance.PlayPaintingSound();
        }

        public void ContinueDrag(Vector2 worldPosition, bool isFastSwipe)
        {
            if (!_currentSpriteRenderer) return;

            _timeKeeper.AddTime();

            _isDragging = true;

            DrawLines(worldPosition, isFastSwipe);
        }

        public void EndDrag()
        {
            _isDragging = false;
            AudioManager.Instance.StopPaintingSound();
            if (_currentSpriteRenderer)
            {
                if (_currentCollider != null)
                {
                    _currentCollider.enabled = true;
                    _currentCollider = null;
                }
                _currentSpriteRenderer = null;
            }

            if (_currentRT)
            {
                //SaveTextureChanges(_currentRT);
                _currentRT.DiscardContents();
                RenderTexture.ReleaseTemporary(_currentRT);
            }

            // SaveCurrentTextureState();
        }

        public void ClearMemory()
        {
            CleanupResources();
        }

        public void SetDefaultColor()
        {
            _brushMaterial.SetColor(BrushColorProperty, _paintData.DefaultBrushColor);
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
            SetColorMode();
        }

        public void SetColor(Color color)
        {
            CurrentBrushColor = color;
            SetColorMode();
        }

        public void SetTexture(int index)
        {
            BrushTextureIndex = index;
            CurrentBrushColor = Color.white;
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

        public bool IsDrawingEdited()
        {
            for (int i = 0; i < _isEdited.Count; i++)
            {
                if (_isEdited[i])
                {
                    return true;
                }
            }
            return false;
        }
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

        public void ClearPainting() => RestoreOriginalTextures();

        public void ClearDrawing() => RestoreEmptyCanvas();

        public Dictionary<int, Sprite> GetLastEditState()
        {
            Dictionary<int, Sprite> dictionary = new Dictionary<int, Sprite>();

            int i = 0;
            foreach (Transform tf in _spritesParent)
            {
                SpriteRenderer sr = tf.GetComponent<SpriteRenderer>();
                dictionary[i++] = sr.sprite;
            }
            return dictionary;
        }

        public Sprite GetDrawingSprite()
        {
            // SpriteRenderer curr = _currentSpriteRenderer;
            // Debug.Log("current Sprite: " + curr.name);

            Debug.Log("sprite texture: " + _drawingSR.name);
            return _drawingSR.sprite;
        }

        // initializers for DrawPaint - single texture

        public void InitializeLevel(Transform sprite, Sprite originalSprite)
        {
            _timeKeeper.StartTimer();

            int spriteIndex = sprite.GetSiblingIndex();
            _drawingSR = sprite.GetComponent<SpriteRenderer>();
            Debug.Log("sprite texture: " + _drawingSR.sprite.name);
            InitializeSprite(spriteIndex, originalSprite);
        }

        // initializers for New/Fresh ColoringSwipe - Multiple sprites => many textures
        public void InitializeLevel(Transform spritesParent)
        {
            _timeKeeper.StartTimer();

            _spritesParent = spritesParent;

            _originalSprites = new();
            _isEdited = new();
            _editedTextures = new();

            foreach (Transform spriteTransform in _spritesParent)
            {
                InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>());
            }
        }

        // initializer for Loading ColoringSwipe - Multiple sprites => many textures
        public void InitializeLevel(Transform spritesParent, LevelTextures levelTextures)
        {
            _timeKeeper.StartTimer();

            _spritesParent = spritesParent;
            foreach (Transform spriteTransform in _spritesParent)
            {
                InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>(), levelTextures);
            }
        }

        public void InitializeLevel(Transform spritesParent, DrawnTexture drawnTextures)
        {
            _timeKeeper.StartTimer();

            _drawingSR = spritesParent.GetComponent<SpriteRenderer>();

            LevelTextures levelTextures = new();
            levelTextures.EditedTextures[0] = drawnTextures.CurrentTexture;
            levelTextures.OriginalSprites[0] = drawnTextures.OriginalSprite;

            InitializeSprite(_drawingSR, levelTextures);
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

        // private void SaveTextureChanges(RenderTexture source)
        // {
        //     // Create a temporary RenderTexture
        //     RenderTexture temp = RenderTexture.GetTemporary(
        //         source.width,
        //         source.height,
        //         0,
        //         RenderTextureFormat.ARGB32,
        //         RenderTextureReadWrite.Linear
        //     );

        //     // Blit from the source to the temporary RenderTexture
        //     Graphics.Blit(source, temp);

        //     // Set the active RenderTexture to the temporary one
        //     RenderTexture previous = RenderTexture.active;
        //     RenderTexture.active = temp;

        //     if (!_savedSprites.ContainsKey(_currentSRIndex))
        //     {
        //         _savedSprites.Add(_currentSRIndex, Sprite.Create(new Texture2D(source.width, source.height), _originalSprites[_currentSRIndex].rect, Vector2.one * .5f));
        //     }

        //     // Read the pixels from the temporary RenderTexture to the destination Texture2D
        //     _savedSprites[_currentSRIndex].texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        //     _savedSprites[_currentSRIndex].texture.Apply();

        //     // Restore the previous active RenderTexture
        //     RenderTexture.active = previous;

        //     // Release the temporary RenWderTexture
        //     RenderTexture.ReleaseTemporary(temp);
        // }

        //drawing specific
        private void InitializeSprite(int spriteIndex, Sprite originalSprite)
        {
            _drawingSR.sprite = originalSprite;
            Texture2D originalTexture = originalSprite.texture;

            _originalSprites.Add(spriteIndex, originalSprite);

            _isEdited.Add(false);
            if (_editedTextures == null)
            {
                _editedTextures = new();
            }

            _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, false));
            Graphics.CopyTexture(originalTexture, _editedTextures[spriteIndex]);
        }

        // coloring - specific only
        private void InitializeSprite(SpriteRenderer spriteRenderer)
        {
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = spriteRenderer.sprite;
            Texture2D originalTexture = originalSprite.texture;


            // if (!_originalSprites.ContainsKey(spriteIndex))
            // {
            //     _originalSprites.Add(spriteIndex, originalSprite);
            // }

            _originalSprites.Add(spriteIndex, originalSprite);
            _isEdited.Add(false);

            _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount, false));

            Graphics.CopyTexture(originalSprite.texture, _editedTextures[spriteIndex]);
        }

        // used both by coloring and drawing
        private void InitializeSprite(SpriteRenderer spriteRenderer, LevelTextures levelTextures)
        {
            _currentSRIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = levelTextures.OriginalSprites[_currentSRIndex];

            _originalSprites[_currentSRIndex] = originalSprite;

            if (levelTextures.EditedTextures.ContainsKey(_currentSRIndex))
            {
                _isEdited.Add(true);
                _editedTextures[_currentSRIndex] = levelTextures.EditedTextures[_currentSRIndex];
            }
            else
            {
                _isEdited.Add(false);
                _editedTextures.Add(_currentSRIndex, new Texture2D(originalSprite.texture.width, originalSprite.texture.height, originalSprite.texture.format, originalSprite.texture.mipmapCount, false));
                Graphics.CopyTexture(originalSprite.texture, _editedTextures[_currentSRIndex]);
            }

            Sprite newSprite = Sprite.Create(_editedTextures[_currentSRIndex], originalSprite.rect, Vector2.one / 2, originalSprite.pixelsPerUnit);
            _editedSprites[_currentSRIndex] = newSprite;
            spriteRenderer.sprite = newSprite;

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
            Debug.Log("CHk _ Call ->  Raycasting Sprites");

            int hitCount = Physics2D.RaycastNonAlloc(worldPosition, Vector2.zero, _hits);
            if (hitCount <= 0) return;

            int maxSortingLayer = -1000;
            int topIndex = -1000;

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

        private void DrawLines(Vector2 currentTouchPosition, bool isFastSwipe)
        {
            if (_firstTouch)
            {
                _lastTouchPosition = currentTouchPosition;
                _firstTouch = false;

                // 1f is waitTime for delay in hiding panel
                _penSelectionHandler.HideMainPanel(.5f);
                ColorSpriteAtPosition(currentTouchPosition);
            }

            float distanceSqr = (currentTouchPosition - _lastTouchPosition).sqrMagnitude;
            float stepSize = _paintData.BrushSize * 0.5f;

            if (isFastSwipe)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(distanceSqr) / stepSize));
                for (int i = 1; i <= steps; i++)
                {
                    if (i % BLIT_THRESHOLD == 0)
                    {
                        Debug.Log("_current SR: " + _currentSpriteRenderer.gameObject.name);
                        float t = i / (float)steps;
                        Vector2 interpolatedPoint = Vector2.Lerp(_lastTouchPosition, currentTouchPosition, t);
                        ColorSpriteAtPosition(interpolatedPoint);
                    }
                }
            }
            else // Slow drawing
            {
                Vector2 direction = (currentTouchPosition - _lastTouchPosition).normalized;
                Vector2 currentPoint = _lastTouchPosition;

                while ((currentPoint - _lastTouchPosition).sqrMagnitude < distanceSqr)
                {
                    ColorSpriteAtPosition(currentPoint);
                    currentPoint += direction * stepSize;
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
            _currentSRIndex = key;

            if (!_isEdited[key] && sprite.texture != _editedTextures[key])
            {
                Graphics.CopyTexture(sprite.texture, _editedTextures[key]);
                _isEdited[key] = true;
            }

            _brushMaterial.SetVector(UVPositionProperty, texturePoint / sprite.texture.width);
            _brushMaterial.SetTexture(MainTexProperty, _editedTextures[key]);
            _brushMaterial.SetTexture(OriginalProperty, _originalSprites[key].texture);

            if (!_isDragging)
            {
                _currentRT = RenderTexture.GetTemporary(_editedTextures[key].width, _editedTextures[key].height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = _currentRT;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = null;
            }

            Graphics.Blit(_editedTextures[key], _currentRT, _brushMaterial);

            Graphics.CopyTexture(_currentRT, _editedTextures[key]);

            if (!_editedSprites.TryGetValue(key, out Sprite _))
            {
                Sprite newSprite = Sprite.Create(_editedTextures[key], sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
                _editedSprites.Add(key, newSprite);
            }
            _currentSpriteRenderer.sprite = _editedSprites[key];
        }

        private Vector2 WorldToTexturePoint(Vector2 worldPos)
        {
            Vector2 texturePoint = _currentSpriteRenderer.transform.InverseTransformPoint(worldPos);
            Vector2 spriteSize = _currentSpriteRenderer.bounds.size;
            Rect spriteRect = _currentSpriteRenderer.sprite.rect;

            return new Vector2(
                (texturePoint.x / spriteSize.x + 0.5f) * spriteRect.width + spriteRect.x,
                (texturePoint.y / spriteSize.y + 0.5f) * spriteRect.height + spriteRect.y
            );
        }

        private void RestoreOriginalTextures()
        {
            if (_spritesParent == null || _originalSprites == null || _editedTextures == null || _isEdited == null) return;

            for (int i = 0; i < _spritesParent.childCount && i < _originalSprites.Count && i < _isEdited.Count; i++)
            {
                var childRenderer = _spritesParent.GetChild(i)?.GetComponent<SpriteRenderer>();
                if (childRenderer == null) continue;

                // Restore original sprite
                if (_originalSprites[i] != null)
                {
                    childRenderer.sprite = _originalSprites[i];
                }

                // Cleanup old texture
                if (_editedTextures.TryGetValue(i, out var oldTexture))
                {
                    if (oldTexture != null)
                    {
                        Object.Destroy(oldTexture);
                    }
                    _editedTextures.Remove(i);
                }

                // Create new texture
                try
                {
                    if (_originalSprites[i]?.texture != null)
                    {
                        var originalTexture = _originalSprites[i].texture;
                        var newTexture = new Texture2D(
                            originalTexture.width,
                            originalTexture.height,
                            originalTexture.format,
                            false
                        );
                        _editedTextures[i] = newTexture;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to create new texture for sprite {i}: {e.Message}");
                }

                // Set edited flag in list
                if (i < _isEdited.Count)
                {
                    _isEdited[i] = false;
                }
            }

            _editedSprites?.Clear();
        }

        private void CleanupResources()
        {
            try
            {
                // Clean up edited textures
                if (_editedTextures != null)
                {
                    var texturesToDestroy = _editedTextures.Values.ToList();
                    foreach (var texture in texturesToDestroy)
                    {
                        if (texture != null)
                        {
                            Object.Destroy(texture);
                        }
                    }
                    _editedTextures.Clear();
                }

                // Clean up edited sprites references
                if (_editedSprites != null)
                {
                    _editedSprites.Clear();
                }

                // Clear original sprite references
                _originalSprites?.Clear();

                // Clear edited flags
                _isEdited?.Clear();

                // Force cleanup on iOS
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            catch (System.OutOfMemoryException oom)
            {
                Debug.LogError($"Out of memory during cleanup: {oom.Message}");
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during cleanup: {e.Message}");
            }
        }

        private void RestoreEmptyCanvas()
        {
            _drawingSR.sprite = _originalSprites[0];
            Object.Destroy(_editedTextures[0]);
            _editedTextures.Remove(0);
            _editedTextures.Add(0, new Texture2D(_originalSprites[0].texture.width, _originalSprites[0].texture.height, _originalSprites[0].texture.format, false));
            _isEdited[0] = false;
            _editedSprites.Clear();
        }
    }
}