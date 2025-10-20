using UnityEngine;
using System.Collections.Generic;

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
        private Dictionary<int, Texture2D> _editedTextures = new();
        private Dictionary<int, Sprite> _originalSprites = new();
        private Dictionary<int, Sprite> _editedSprites = new();
        private int _currentSRIndex;
        private readonly RaycastHit2D[] _hits;

        private readonly int BrushColorProperty = Shader.PropertyToID("_BrushColor");
        private readonly int BrushSizeProperty = Shader.PropertyToID("_BrushSize");
        private readonly int BrushTextureProperty = Shader.PropertyToID("_BrushTexture");
        private readonly int UVPositionProperty = Shader.PropertyToID("_UVPosition");
        private readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private readonly int OriginalProperty = Shader.PropertyToID("_Original");

        private const int BLIT_THRESHOLD = 10;

        private Dictionary<int, RenderTexture> _spriteRTs = new();

        public PaintController(InitPaintData paintData, PenSelectionHandler penPanelHandler, TimeKeeper timer)
        {
            _paintData = paintData;
            _hits = new RaycastHit2D[paintData.MaxHitColliders];
            _penSelectionHandler = penPanelHandler;
            _timeKeeper = timer;
            PreWarmShaders();
            SetDefaultColor();
        }

        #region Public API (for PaintService)

        public void BeginDrag(Vector2 worldPosition)
        {
            _firstTouch = true;
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
                _currentRT.DiscardContents();
                RenderTexture.ReleaseTemporary(_currentRT);
            }
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

        public bool IsDrawingEdited()
        {
            for (int i = 0; i < _isEdited.Count; i++)
            {
                if (_isEdited[i])
                    return true;
            }
            return false;
        }

        public Dictionary<int, Sprite> GetLastEditState()
        {
            Dictionary<int, Sprite> dict = new();
            for (int i = 0; i < _spritesParent.childCount; i++)
            {
                var sr = _spritesParent.GetChild(i).GetComponent<SpriteRenderer>();
                dict[i] = sr.sprite;
            }
            return dict;
        }

        public Sprite GetDrawingSprite()
        {
            return _drawingSR.sprite;
        }

        public void ClearPainting() => RestoreOriginalTextures();
        public void ClearDrawing() => RestoreEmptyCanvas();
        public void SetBrushScale(float value)
        {
            _paintData.BrushSize = value;
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
        }

        #endregion

        #region Initialization

        public void InitializeLevel(Transform sprite, Sprite originalSprite)
        {
            _timeKeeper.StartTimer();
            int spriteIndex = sprite.GetSiblingIndex();
            _drawingSR = sprite.GetComponent<SpriteRenderer>();
            InitializeSprite(spriteIndex, originalSprite);
        }

        public void InitializeLevel(Transform spritesParent)
        {
            _timeKeeper.StartTimer();
            _spritesParent = spritesParent;
            _originalSprites.Clear();
            _isEdited.Clear();
            _editedTextures.Clear();
            _editedSprites.Clear();

            foreach (Transform tf in _spritesParent)
            {
                InitializeSprite(tf.GetComponent<SpriteRenderer>());
            }
        }

        public void InitializeLevel(Transform spritesParent, LevelTextures levelTextures)
        {
            _timeKeeper.StartTimer();
            _spritesParent = spritesParent;
            foreach (Transform tf in _spritesParent)
            {
                InitializeSprite(tf.GetComponent<SpriteRenderer>(), levelTextures);
            }
        }

        public void InitializeLevel(Transform spritesParent, DrawnTexture drawnTexture)
        {
            _timeKeeper.StartTimer();
            _drawingSR = spritesParent.GetComponent<SpriteRenderer>();

            LevelTextures levelTextures = new()
            {
                OriginalSprites = new Dictionary<int, Sprite>() { { 0, drawnTexture.OriginalSprite } },
                EditedTextures = new Dictionary<int, Texture2D>() { { 0, drawnTexture.CurrentTexture } }
            };

            InitializeSprite(_drawingSR, levelTextures);
        }

        #endregion

        #region Private Helpers

        private void SetColorMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Color]);
        private void SetTextureMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Texture]);
        private void SetEraseMode() => SetPaintMode(_paintData.BrushMaterials[(int)PaintMode.Erase]);

        private void SetPaintMode(Material mat)
        {
            _brushMaterial = mat;
            _brushMaterial.SetColor(BrushColorProperty, CurrentBrushColor);
            _brushMaterial.SetFloat(BrushSizeProperty, _paintData.BrushSize);
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

        private void PreWarmShaders()
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            RenderTexture tempDestRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            RenderTexture prevActive = RenderTexture.active;

            foreach (Material mat in _paintData.BrushMaterials)
            {
                SetPaintMode(mat);
                Graphics.Blit(tempRT, tempDestRT, _brushMaterial);
            }

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.ReleaseTemporary(tempDestRT);
        }

        private void RaycastSprites(Vector2 worldPosition)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPosition, Vector2.zero, _hits);
            if (hitCount <= 0) return;

            int topSorting = -1000;
            int topIndex = -1;

            for (int i = 0; i < hitCount; i++)
            {
                if (!_hits[i].collider.TryGetComponent(out SpriteRenderer sr)) continue;
                if (sr.sortingOrder <= topSorting) continue;

                topSorting = sr.sortingOrder;
                topIndex = i;
                _currentSpriteRenderer = sr;
            }

            if (topIndex == -1) return;
            _currentCollider = _currentSpriteRenderer.GetComponent<Collider2D>();
            _currentCollider.enabled = false;
            ColorSpriteAtPosition(_hits[topIndex].point);
        }

        private void DrawLines(Vector2 currentPos, bool isFastSwipe)
        {
            if (_firstTouch)
            {
                _lastTouchPosition = currentPos;
                _firstTouch = false;
                _penSelectionHandler.HideMainPanel(.5f);
                ColorSpriteAtPosition(currentPos);
            }

            float distSqr = (currentPos - _lastTouchPosition).sqrMagnitude;
            float stepSize = _paintData.BrushSize * 0.5f;

            if (isFastSwipe)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(distSqr) / stepSize));
                for (int i = 1; i <= steps; i++)
                {
                    if (i % BLIT_THRESHOLD == 0)
                    {
                        float t = i / (float)steps;
                        Vector2 point = Vector2.Lerp(_lastTouchPosition, currentPos, t);
                        ColorSpriteAtPosition(point);
                    }
                }
            }
            else
            {
                Vector2 dir = (currentPos - _lastTouchPosition).normalized;
                Vector2 point = _lastTouchPosition;
                while ((point - _lastTouchPosition).sqrMagnitude < distSqr)
                {
                    ColorSpriteAtPosition(point);
                    point += dir * stepSize;
                }
            }

            _lastTouchPosition = currentPos;
        }

        private void ColorSpriteAtPosition(Vector2 worldPosition)
        {
            if (_currentSpriteRenderer == null) return;

            int key = _currentSpriteRenderer.transform.GetSiblingIndex();
            Vector2 uv = WorldToTexturePoint(worldPosition);

            // Ensure edited texture exists
            if (!_editedTextures.ContainsKey(key) || _editedTextures[key] == null)
            {
                Texture2D tex = _currentSpriteRenderer.sprite.texture;
                _editedTextures[key] = new Texture2D(tex.width, tex.height, tex.format, false);
                Graphics.CopyTexture(tex, _editedTextures[key]);
            }

            // Get or create persistent RenderTexture for this sprite
            if (!_spriteRTs.TryGetValue(key, out _currentRT) || _currentRT == null)
            {
                Texture2D tex = _editedTextures[key];
                _currentRT = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, _currentRT); // Start with the current edited texture
                _spriteRTs[key] = _currentRT;
            }

            // Assign shader properties
            _brushMaterial.SetVector("_UVPosition", uv / _editedTextures[key].width); // UV in 0-1 range
            _brushMaterial.SetTexture("_MainTex", _currentRT); // Accumulated paint
            _brushMaterial.SetTexture("_Original", _originalSprites[key].texture);

            // Temporary RT for double-buffered blit
            RenderTexture tempRT = RenderTexture.GetTemporary(_currentRT.width, _currentRT.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(_currentRT, tempRT, _brushMaterial); // Paint current dot
            Graphics.Blit(tempRT, _currentRT); // Copy back to main RT
            RenderTexture.ReleaseTemporary(tempRT);

            // Copy updated RT to Texture2D to persist changes
            Graphics.CopyTexture(_currentRT, _editedTextures[key]);

            // Update or create Sprite with the new texture
            if (!_editedSprites.TryGetValue(key, out Sprite s))
            {
                s = Sprite.Create(_editedTextures[key], _currentSpriteRenderer.sprite.rect,
                                  Vector2.one * 0.5f, _currentSpriteRenderer.sprite.pixelsPerUnit);
                _editedSprites[key] = s;
            }
            _currentSpriteRenderer.sprite = _editedSprites[key];
        }



        private Vector2 WorldToTexturePoint(Vector2 worldPos)
        {
            Vector2 local = _currentSpriteRenderer.transform.InverseTransformPoint(worldPos);
            Vector2 spriteSize = _currentSpriteRenderer.bounds.size;
            Rect rect = _currentSpriteRenderer.sprite.rect;

            return new Vector2(
                (local.x / spriteSize.x + 0.5f) * rect.width + rect.x,
                (local.y / spriteSize.y + 0.5f) * rect.height + rect.y
            );
        }

        private void RestoreOriginalTextures()
        {
            if (_spritesParent == null) return;

            for (int i = 0; i < _spritesParent.childCount; i++)
            {
                var sr = _spritesParent.GetChild(i).GetComponent<SpriteRenderer>();
                if (sr == null || !_originalSprites.ContainsKey(i)) continue;
                sr.sprite = _originalSprites[i];

                if (_editedTextures.TryGetValue(i, out var tex))
                    Object.Destroy(tex);

                Texture2D newTex = new Texture2D(_originalSprites[i].texture.width, _originalSprites[i].texture.height, _originalSprites[i].texture.format, false);
                _editedTextures[i] = newTex;
                _isEdited[i] = false;
            }

            _editedSprites.Clear();
        }

        private void RestoreEmptyCanvas()
        {
            if (_drawingSR == null) return;
            _drawingSR.sprite = _originalSprites[0];

            if (_editedTextures.TryGetValue(0, out var tex)) Object.Destroy(tex);

            _editedTextures[0] = new Texture2D(_originalSprites[0].texture.width, _originalSprites[0].texture.height, _originalSprites[0].texture.format, false);
            _isEdited[0] = false;
            _editedSprites.Clear();
        }

        private void CleanupResources()
        {
            foreach (var tex in _editedTextures.Values) if (tex != null) Object.Destroy(tex);
            _editedTextures.Clear();
            _editedSprites.Clear();
            _originalSprites.Clear();
            _isEdited.Clear();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        #endregion

        #region Sprite Initialization Helpers

        private void InitializeSprite(int spriteIndex, Sprite originalSprite)
        {
            _drawingSR.sprite = originalSprite;
            _originalSprites[spriteIndex] = originalSprite;

            _isEdited.Add(false);

            Texture2D tex = new Texture2D(originalSprite.texture.width, originalSprite.texture.height, originalSprite.texture.format, false);
            _editedTextures[spriteIndex] = tex;
            Graphics.CopyTexture(originalSprite.texture, _editedTextures[spriteIndex]);
        }

        private void InitializeSprite(SpriteRenderer sr)
        {
            int spriteIndex = sr.transform.GetSiblingIndex();
            Sprite originalSprite = sr.sprite;

            _originalSprites[spriteIndex] = originalSprite;
            _isEdited.Add(false);

            Texture2D tex = new Texture2D(originalSprite.texture.width, originalSprite.texture.height, originalSprite.texture.format, false);
            _editedTextures[spriteIndex] = tex;
            Graphics.CopyTexture(originalSprite.texture, _editedTextures[spriteIndex]);
        }

        private void InitializeSprite(SpriteRenderer sr, LevelTextures levelTextures)
        {
            int index = sr.transform.GetSiblingIndex();
            _currentSRIndex = index;

            _originalSprites[index] = levelTextures.OriginalSprites[index];

            if (levelTextures.EditedTextures.ContainsKey(index))
            {
                _isEdited.Add(true);
                _editedTextures[index] = levelTextures.EditedTextures[index];
            }
            else
            {
                _isEdited.Add(false);
                Texture2D tex = new Texture2D(_originalSprites[index].texture.width, _originalSprites[index].texture.height, _originalSprites[index].texture.format, false);
                _editedTextures[index] = tex;
                Graphics.CopyTexture(_originalSprites[index].texture, _editedTextures[index]);
            }

            Sprite newSprite = Sprite.Create(_editedTextures[index], _originalSprites[index].rect, Vector2.one * 0.5f, sr.sprite.pixelsPerUnit);
            _editedSprites[index] = newSprite;
            sr.sprite = newSprite;
        }

        #endregion
    }
}
