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
        private SpriteRenderer _currentSpriteRenderer;
        private Collider2D _currentCollider;
        private RenderTexture _currentRT;

        private InitPaintData _paintData;
        private PenSelectionHandler _penSelectionHandler;
        private AudioManager _audioHandler;

        private List<bool> _isEdited = new();
        private Dictionary<int, Texture2D> _editedTextures = new Dictionary<int, Texture2D>();
        private Dictionary<int, Sprite> _originalSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> _savedSprites;
        private int _currentSRIndex;
        private const int BLIT_THRESHOLD = 20;
        private readonly RaycastHit2D[] _hits;
        private readonly int BrushColorProperty = Shader.PropertyToID("_BrushColor");
        private readonly int BrushSizeProperty = Shader.PropertyToID("_BrushSize");
        private readonly int BrushTextureProperty = Shader.PropertyToID("_BrushTexture");
        private readonly int UVPositionProperty = Shader.PropertyToID("_UVPosition");
        private readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private readonly int OriginalProperty = Shader.PropertyToID("_Original");

        // CONSTRUCTOR
        public PaintController(InitPaintData paintData, PenSelectionHandler penPanelHandler, AudioManager audioMan)
        {
            _paintData = paintData;
            _hits = new RaycastHit2D[paintData.MaxHitColliders];
            _penSelectionHandler = penPanelHandler;
            _audioHandler = audioMan;
            PreWarmShaders();
            SetDefaultColor();
        }

        public void BeginDrag(Vector2 worldPosition)
        {
            _firstTouch = true;
            //_currentRT = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGB32);
            RaycastSprites(worldPosition);
        }

        public void ContinueDrag(Vector2 worldPosition)
        {
            if (!_currentSpriteRenderer) return;
            _isDragging = true;
            DrawLines(worldPosition);
            _audioHandler.PlayPaintingSound();
        }

        public void EndDrag()
        {
            _isDragging = false;
            _audioHandler.StopPaintingSound();
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
                // SaveTextureChanges(_currentRT, _editedTextures[_currentSRIndex]);
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
            ReapplySpritesToRenderers();
        }

        public Texture2D GetTex()
        {
            return _spritesParent.GetChild(0).GetComponent<SpriteRenderer>().sprite.texture;
        }

        public Sprite GetSprite()
        {
            var sr = _spritesParent.GetChild(0).GetComponent<SpriteRenderer>();
            var texture = sr.sprite.texture;
            return Sprite.Create(texture, sr.sprite.rect, Vector2.one * 0.5f);
        }

        public Dictionary<int, Sprite> GetLastEditState()
        {
            Dictionary<int, Sprite> dictionary = new Dictionary<int, Sprite>();

            int i = 0;
            foreach (Transform tf in _spritesParent)
            {
                SpriteRenderer sr = tf.GetComponent<SpriteRenderer>();
                dictionary[i++] = sr.sprite;
            }

            // foreach (var kvp in _sprites)
            // {
            //     dictionary[kvp.Key] = kvp.Value.texture;
            // }

            return dictionary;
            // return new Dictionary<int, Texture2D>(_sprites);
        }

        public void InitializeLevel(Transform spritesParent)
        {
            Debug.Log("chk 1");
            _spritesParent = spritesParent;
            foreach (Transform spriteTransform in _spritesParent)
            {
                InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>());
            }
        }
        public void InitializeLevel(Transform spritesParent, Dictionary<int, Texture2D> editedTextures)
        {
            Debug.Log("chk 2");
            _spritesParent = spritesParent;
            foreach (Transform spriteTransform in _spritesParent)
            {
                InitializeSprite(spriteTransform.GetComponent<SpriteRenderer>(), editedTextures);
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

        private void SaveTextureChanges(RenderTexture source)
        {
            // Create a temporary RenderTexture
            RenderTexture temp = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );

            // Blit from the source to the temporary RenderTexture
            Graphics.Blit(source, temp);

            // Set the active RenderTexture to the temporary one
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = temp;

            // Read the pixels from the temporary RenderTexture to the destination Texture2D
            _savedSprites[_currentSRIndex].texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            _savedSprites[_currentSRIndex].texture.Apply();

            // Restore the previous active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(temp);
        }

        private void InitializeSprite(SpriteRenderer spriteRenderer)
        {
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();
            Sprite originalSprite = spriteRenderer.sprite;
            Texture2D originalTexture = originalSprite.texture;

            _originalSprites.Add(spriteIndex, originalSprite);
            _isEdited.Add(false);
            _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount, false));
             //Graphics.CopyTexture(originalTexture, _editedTextures[spriteIndex]);
        }
        private void InitializeSprite(SpriteRenderer spriteRenderer, Dictionary<int, Texture2D> editedTextures)
        {
            int spriteIndex = spriteRenderer.transform.GetSiblingIndex();

            Sprite originalSprite = spriteRenderer.sprite;
            Texture2D originalTexture = originalSprite.texture;

            _originalSprites.Add(spriteIndex, originalSprite);
            _isEdited.Add(true);
           
            if (editedTextures.ContainsKey(spriteIndex))
            {
                _editedTextures[spriteIndex] = editedTextures[spriteIndex];
            } else
            {
                _editedTextures.Add(spriteIndex, new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount, false));

            }

             //Graphics.CopyTexture(originalTexture, _editedTextures[spriteIndex]);

            Sprite newSprite = Sprite.Create(editedTextures[spriteIndex], originalSprite.rect, Vector2.one / 2, originalSprite.pixelsPerUnit);
            _sprites.Add(spriteIndex, newSprite);
            spriteRenderer.sprite = newSprite;
            Debug.Log(spriteRenderer.name);
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

            ColorSpriteAtPosition(_hits[topIndex].point);
        }

        private void DrawLines(Vector2 currentTouchPosition)
        {
            if (_firstTouch)
            {
                _lastTouchPosition = currentTouchPosition;
                _firstTouch = false;

                // 1f is waitTime for delay in hiding panel
                _penSelectionHandler.HideMainPanel(.5f);
            }

            float distance = Vector2.Distance(_lastTouchPosition, currentTouchPosition);
            int steps = Mathf.CeilToInt(distance / (_paintData.BrushSize * 0.5f));
            int currentSteps = 0;

            for (int i = 0; i <= steps; i++)
            {
                currentSteps++;
                if (currentSteps >= BLIT_THRESHOLD)
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
            _currentSRIndex = key;

            if (!_isEdited[key])
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
            // _editedTextures[key].ReadPixels(new Rect(0, 0, _editedTextures[key].width, _editedTextures[key].height), 0, 0);
            // _editedTextures[key].Apply();

            if (!_sprites.TryGetValue(key, out Sprite _))
            {
                Sprite newSprite = Sprite.Create(_editedTextures[key], sprite.rect, Vector2.one / 2, sprite.pixelsPerUnit);
                _sprites.Add(key, newSprite);
            }
            _currentSpriteRenderer.sprite = _sprites[key];
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


        // Save State - UNDO _ FUNCTION
        private void SaveCurrentTextureState()
        {
            int index = _currentSpriteRenderer.transform.GetSiblingIndex();

            if (_isEdited[index])
            {
                Texture2D newTex = new(_editedTextures[index].width, _editedTextures[index].height, TextureFormat.RGBA32, 1, false);
                Graphics.CopyTexture(_editedTextures[index], newTex);
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
}