using UnityEngine;
using System.Collections.Generic;

namespace DrawingGame
{
    public class PaintController
    {
        private const int MAX_UNDO_STEPS = 10;
        private readonly List<Texture2D> _undoHistory = new List<Texture2D>();

        public event System.Action OnStrokeBegin;
        public event System.Action OnStrokeEnd;

        public bool CanUndo => _undoHistory.Count > 0;

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

        private Dictionary<int, bool> _isEdited = new Dictionary<int, bool>();
        private Dictionary<int, Texture2D> _editedTextures = new Dictionary<int, Texture2D>();
        private Dictionary<int, Sprite> _originalSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _editedSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _savedSprites = new Dictionary<int, Sprite>();
        private int _currentSRIndex;
        private const int BLIT_THRESHOLD = 10;
        private RaycastHit2D[] _hits;
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
            _isDragging = false; // Reset to ensure new RenderTexture generation

            // Safeguard: If a previous touch was interrupted and dropped EndDrag, clean up the leaked RT
            if (_currentRT != null)
            {
                _currentRT.DiscardContents();
                RenderTexture.ReleaseTemporary(_currentRT);
                _currentRT = null;
            }

            // Snapshot state for undo BEFORE this stroke can modify anything.
            // Only meaningful for the single-sprite DrawPaint mode (_drawingSR set);
            // multi-sprite ColoringSwipe mode is untouched.
            if (_drawingSR != null)
            {
                PushUndoSnapshot(_drawingSR.transform.GetSiblingIndex());
            }

            RaycastSprites(worldPosition);
            //AudioManager.Instance.PlayPaintingSound();
        }

        public void ContinueDrag(Vector2 worldPosition)
        {
            if (!_currentSpriteRenderer) return;

            _timeKeeper.AddTime();

            _isDragging = true;

            DrawLines(worldPosition);
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
                OnStrokeEnd?.Invoke();
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
            ClearUndoHistory();
        }

        private void PushUndoSnapshot(int key)
        {
            if (!_editedTextures.ContainsKey(key)) return;

            Texture2D source = _editedTextures[key];
            Texture2D snapshot = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(source, snapshot);

            _undoHistory.Add(snapshot);

            if (_undoHistory.Count > MAX_UNDO_STEPS)
            {
                Object.Destroy(_undoHistory[0]);
                _undoHistory.RemoveAt(0);
            }
        }

        public void Undo()
        {
            if (_drawingSR == null || _undoHistory.Count == 0) return;

            int key = _drawingSR.transform.GetSiblingIndex();
            if (!_editedTextures.ContainsKey(key)) return;

            Texture2D snapshot = _undoHistory[_undoHistory.Count - 1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);

            Graphics.CopyTexture(snapshot, _editedTextures[key]);
            Object.Destroy(snapshot);

            Sprite baseSprite = _originalSprites.ContainsKey(key) ? _originalSprites[key] : _drawingSR.sprite;

            if (!_editedSprites.ContainsKey(key) || _editedSprites[key] == null)
            {
                _editedSprites[key] = Sprite.Create(_editedTextures[key], baseSprite.rect, Vector2.one / 2, baseSprite.pixelsPerUnit);
            }

            _drawingSR.sprite = _editedSprites[key];
            _isEdited[key] = _undoHistory.Count > 0;
        }

        private void ClearUndoHistory()
        {
            foreach (Texture2D snapshot in _undoHistory)
            {
                if (snapshot != null) Object.Destroy(snapshot);
            }
            _undoHistory.Clear();
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
            foreach (var edited in _isEdited.Values)
            {
                if (edited)
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

            RenderTexture rt = RenderTexture.GetTemporary(1024, 1024, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;

            int i = 0;

            foreach (Transform tf in _spritesParent)
            {
                SpriteRenderer sr = tf.GetComponent<SpriteRenderer>();
                Sprite sprite = sr.sprite;

                dictionary[i++] = sprite;
            }

            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.active = null;
            return dictionary;
        }

        public Sprite GetDrawingSprite()
        {
            Debug.Log("sprite texture: " + _drawingSR.name);
            return _drawingSR.sprite;
        }

        // initializers for DrawPaint - single texture

        public void InitializeLevel(Transform sprite, Sprite originalSprite)
        {
            _timeKeeper.StartTimer();

            int spriteIndex = sprite.GetSiblingIndex();
            _drawingSR = sprite.GetComponent<SpriteRenderer>();

            if (_drawingSR == null)
            {
                Debug.LogError($"[PaintController] SpriteRenderer not found on {sprite.name}!");
                return;
            }

            if (_drawingSR.sprite != null)
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
        // 
        //     // Blit from the source to the temporary RenderTexture
        //     Graphics.Blit(source, temp);
        // 
        //     // Set the active RenderTexture to the temporary one
        //     RenderTexture previous = RenderTexture.active;
        //     RenderTexture.active = temp;
        // 
        //     if (!_savedSprites.ContainsKey(_currentSRIndex))
        //     {
        //         _savedSprites.Add(_currentSRIndex, Sprite.Create(new Texture2D(source.width, source.height), _originalSprites[_currentSRIndex].rect, Vector2.one * .5f));
        //     }
        // 
        //     // Read the pixels from the temporary RenderTexture to the destination Texture2D
        //     _savedSprites[_currentSRIndex].texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        //     _savedSprites[_currentSRIndex].texture.Apply();
        // 
        //     // Restore the previous active RenderTexture
        //     RenderTexture.active = previous;
        // 
        //     // Release the temporary RenWderTexture
        //     RenderTexture.ReleaseTemporary(temp);
        // }

        //drawing specific
        private void InitializeSprite(int spriteIndex, Sprite originalSprite)
        {
            _drawingSR.sprite = originalSprite;
            ClearUndoHistory();

            // Auto-Add Collider if missing
            if (_drawingSR.GetComponent<Collider2D>() == null)
            {
                Debug.Log($"[PaintController] Automatically adding PolygonCollider2D to {_drawingSR.name}");
                _drawingSR.gameObject.AddComponent<PolygonCollider2D>();
            }

            _originalSprites[spriteIndex] = originalSprite;

            _isEdited[spriteIndex] = false;

            if (_editedTextures == null)
            {
                _editedTextures = new();
            }

            _editedTextures[spriteIndex] = CreateReadableTexture(originalSprite.texture);
        }

        // coloring - specific only
        private void InitializeSprite(SpriteRenderer spriteRenderer)
        {
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = spriteRenderer.sprite;

            // Auto-Add Collider if missing
            if (spriteRenderer.GetComponent<Collider2D>() == null)
            {
                Debug.Log($"[PaintController] Automatically adding PolygonCollider2D to {spriteRenderer.name}");
                spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();
            }

            _originalSprites[spriteIndex] = originalSprite;
            
            _isEdited[spriteIndex] = false;

            _editedTextures[spriteIndex] = CreateReadableTexture(originalSprite.texture);
        }

        // used both by coloring and drawing
        private void InitializeSprite(SpriteRenderer spriteRenderer, LevelTextures levelTextures)
        {
            _currentSRIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = levelTextures.OriginalSprites[_currentSRIndex];

             // Auto-Add Collider if missing
            if (spriteRenderer.GetComponent<Collider2D>() == null)
            {
                Debug.Log($"[PaintController] Automatically adding PolygonCollider2D to {spriteRenderer.name}");
                spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();
            }

            _originalSprites[_currentSRIndex] = originalSprite;

            if (levelTextures.EditedTextures.ContainsKey(_currentSRIndex))
            {
                _isEdited[_currentSRIndex] = true;
                _editedTextures[_currentSRIndex] = levelTextures.EditedTextures[_currentSRIndex];
            }
            else
            {
                _isEdited[_currentSRIndex] = false;
                _editedTextures[_currentSRIndex] = CreateReadableTexture(originalSprite.texture);
            }

            Sprite newSprite = Sprite.Create(_editedTextures[_currentSRIndex], originalSprite.rect, Vector2.one / 2, originalSprite.pixelsPerUnit);
            _editedSprites[_currentSRIndex] = newSprite;
            spriteRenderer.sprite = newSprite;

        }

        private Texture2D CreateReadableTexture(Texture original)
        {
            // Create a temporary RenderTexture of the same size
            RenderTexture tmp = RenderTexture.GetTemporary(
                original.width,
                original.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(original, tmp);

            // Set the current RenderTexture to the temporary one we created
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);

            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;
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
            _hits = Physics2D.RaycastAll(worldPosition, Vector2.zero);


            int hitCount = _hits.Length;
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
            OnStrokeBegin?.Invoke();
            ColorSpriteAtPosition(_hits[topIndex].point);
        }

        private void DrawLines(Vector2 currentTouchPosition)
        {
            if (_firstTouch)
            {
                _lastTouchPosition = currentTouchPosition;
                _firstTouch = false;

                // 1f is waitTime for delay in hiding panel
                if (_penSelectionHandler)
                    _penSelectionHandler.HideMainPanel(.5f);
                ColorSpriteAtPosition(currentTouchPosition);
            }

            float distance = Vector2.Distance(_lastTouchPosition, currentTouchPosition);
            float baseStepSize = Mathf.Max(0.01f, _paintData.BrushSize * 0.5f);

            int requestedSteps = Mathf.CeilToInt(distance / baseStepSize);
            
            // Limit absolute max steps per frame to protect the GPU
            // 40 steps per frame is high enough for a continuous line but safe for older iPhones
            int maxSafeSteps = 40; 
            int actualSteps = Mathf.Min(requestedSteps, maxSafeSteps);

            if (actualSteps > 0)
            {
                for (int i = 1; i <= actualSteps; i++)
                {
                    float t = i / (float)actualSteps; 
                    Vector2 interpolatedPoint = Vector2.Lerp(_lastTouchPosition, currentTouchPosition, t);
                    ColorSpriteAtPosition(interpolatedPoint);
                }
            }

            _lastTouchPosition = currentTouchPosition;
        }

        public Sprite CurrentSprite() => _currentSpriteRenderer.sprite;

        private void ColorSpriteAtPosition(Vector2 worldPosition)
        {
            if (_currentSpriteRenderer == null) return;

            Vector2 texturePoint = WorldToTexturePoint(worldPosition);
            Sprite sprite = _currentSpriteRenderer.sprite;
            int key = _currentSpriteRenderer.transform.GetSiblingIndex();
            _currentSRIndex = key;

            if (!_isEdited.ContainsKey(key) || !_editedTextures.ContainsKey(key) || !_originalSprites.ContainsKey(key)) return;

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
                if (_currentRT != null) 
                {
                    RenderTexture.ReleaseTemporary(_currentRT);
                }

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
                _editedSprites[key] = newSprite;
            }
            _currentSpriteRenderer.sprite = _editedSprites[key];
        }

        private Vector2 WorldToTexturePoint(Vector2 worldPos)
        {
            Vector2 texturePoint = _currentSpriteRenderer.transform.InverseTransformPoint(worldPos);
            Sprite sprite = _currentSpriteRenderer.sprite;
            // Use sprite.bounds.size (local-space) so scaling the GO doesn't offset paint position
            Vector2 spriteSize = sprite.bounds.size;
            Rect spriteRect = sprite.rect;

            return new Vector2(
                (texturePoint.x / spriteSize.x + 0.5f) * spriteRect.width + spriteRect.x,
                (texturePoint.y / spriteSize.y + 0.5f) * spriteRect.height + spriteRect.y
            );
        }


        // Save State - UNDO _ FUNCTION
        // private void SaveCurrentTextureState()
        // {
        //     int index = _currentSpriteRenderer.transform.GetSiblingIndex();
        // 
        //     if (_isEdited[index])
        //     {
        //         Texture2D newTex = new(_editedTextures[index].width, _editedTextures[index].height, TextureFormat.RGBA32, 1, false);
        //         Graphics.CopyTexture(_editedTextures[index], newTex);
        //     }
        // }

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

            foreach (var sprite in _editedSprites.Values)
            {
                if (sprite != null)
                {
                    Object.Destroy(sprite);
                }
            }
            _editedSprites.Clear();
            _originalSprites.Clear();
        }


        private void RestoreOriginalTextures()
        {
            for (int i = 0; i < _spritesParent.childCount; i++)
            {
                if (!_originalSprites.ContainsKey(i)) continue;

                _spritesParent.GetChild(i).GetComponent<SpriteRenderer>().sprite = _originalSprites[i];

                if (_editedTextures.ContainsKey(i))
                {
                    Object.Destroy(_editedTextures[i]);
                }
                _editedTextures[i] = CreateReadableTexture(_originalSprites[i].texture);
                _isEdited[i] = false;
            }

            foreach (var sprite in _editedSprites.Values)
            {
                if (sprite != null) Object.Destroy(sprite);
            }
            _editedSprites.Clear();
        }

        private void RestoreEmptyCanvas()
        {
            int index = _drawingSR.transform.GetSiblingIndex();
            if (!_originalSprites.ContainsKey(index)) return;

            _drawingSR.sprite = _originalSprites[index];
            if (_editedTextures.ContainsKey(index))
            {
                Object.Destroy(_editedTextures[index]);
            }
            _editedTextures[index] = CreateReadableTexture(_originalSprites[index].texture);
            _isEdited[index] = false;

            if (_editedSprites.ContainsKey(0) && _editedSprites[0] != null)
            {
                Object.Destroy(_editedSprites[0]);
            }
            _editedSprites.Clear();
            ClearUndoHistory();
        }
    }
}