using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ColorSwipeGame
{
    public class PaintService : MonoBehaviour
    {
        [SerializeField] private Transform _spritesContainer;
        [SerializeField] private Texture2D[] _textures;
        [SerializeField] private InputHandler _inputHandler;
        [SerializeField] private PenSelectionHandler _penPanelHandler;
        [SerializeField] private TimeKeeper _timer;

        [Header("Brush Settings")]
        [SerializeField] private int _maxHitColliders = 10;
        [SerializeField, Range(0f, 5f)] private int _brushSize = 1;
        [SerializeField, Range(0.01f, 0.001f)] private float _brushScaleFactor = 0.01f;
        [SerializeField] private Color _defaultBrushColor = Color.white;
        [SerializeField, Tooltip("Fill in order: PaintColor, then erase and then texturePaint")]
        private Material[] _customMaterials;

        private PaintController _paintController;
        private Camera _mainCamera;

        private int _touchCount = 0;
        private int _maxTouchCount = 0;

        [PreviewField(256)]
        public Texture2D _preTex;
        [PreviewField(256)]
        public Texture2D _postTex;

        public bool CanPaint { get; set; } = false;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Application.targetFrameRate = 60;
            _maxTouchCount = Random.Range(10, 20);

            InitPaintData newData = new InitPaintData(_maxHitColliders, _brushSize * _brushScaleFactor, _defaultBrushColor, _customMaterials, _textures);
            _paintController = new PaintController(newData, _penPanelHandler, _timer);

            if (_inputHandler == null)
            {
                Debug.LogError("InputHandler not found. Please assign it in the inspector or add it to this GameObject.");
                return;
            }
        }

        private void OnEnable()
        {
            _inputHandler.OnBeginDrag += BeginDrag;
            _inputHandler.OnDragging += OnDrag;
            _inputHandler.OnDragEnd += EndDrag;
            _inputHandler.OnDragStationary += OnDragStationary;
        }
        private void OnDisable()
        {
            _inputHandler.OnBeginDrag -= BeginDrag;
            _inputHandler.OnDragging -= OnDrag;
            _inputHandler.OnDragEnd -= EndDrag;
            _inputHandler.OnDragStationary -= OnDragStationary;
        }

        private void OnDestroy()
        {
            _paintController?.ClearMemory();
        }

        public void OnLevelLoad()
        {
            _paintController.InitializeLevel(_spritesContainer.GetChild(0).GetChild(0));
        }

        public void OnEditedLevelLoad(LevelTextures levelTextures)
        {
            _paintController.InitializeLevel(_spritesContainer.GetChild(0).GetChild(0), levelTextures);
        }

        // SwipeToDraw - Load Blank or Level
        public void LoadDrawPaintLevel(Sprite originalSprite)
        {
            Debug.Log("Chek call to new ");

            Transform drawSprite = _spritesContainer.GetChild(0);

            _paintController.InitializeLevel(drawSprite, originalSprite);
        }

        public void OnEditedLevelLoad(DrawnTexture drawnTexture)
        {
            _paintController.InitializeLevel(_spritesContainer.GetChild(0), drawnTexture);
        }
        public Dictionary<int, Sprite> SaveCurrentState()
        {
            return _paintController.GetLastEditState();
        }

        public Sprite SaveDrawnState()
        {
            var obj = _paintController.GetDrawingSprite();

            return obj;
        }

        private void BeginDrag(Vector2 touchPosition)
        {
            if (CanPaint)
            {
                Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(touchPosition);
                _paintController.BeginDrag(worldPosition);
                _touchCount++;
            }
        }

        private void OnDrag(Vector2 touchPosition, bool isFastSwipe)
        {
            if (CanPaint)
            {

                Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(touchPosition);
                _paintController.ContinueDrag(worldPosition, isFastSwipe);

                if (_touchCount > _maxTouchCount)
                {
                    AudioManager.Instance.PlayCheeringAudio();
                    _touchCount = 0;
                    _maxTouchCount = Random.Range(10, 20);
                }
            }
        }

        private void OnDragStationary()
        {
            if (CanPaint)
            {

            }
        }

        private void EndDrag()
        {
            if (CanPaint)
            {
                _paintController.EndDrag();
            }
        }

        // Public methods for other scripts to interact with PaintController
        public void SetBrushSize(float size) => _paintController.SetBrushScale(size * _brushScaleFactor);

        // on main pen selection or erase button
        public void SetDefaultColorMode()
        {
            AudioManager.Instance.ChangeBrushSound_Paint();
            _paintController.SetDefaultColor();
        }

        public void SetDefaultTextureMode(int index = 0)
        {
            AudioManager.Instance.ChangeBrushSound_Paint();
            _paintController.SetDefaultTexture(index);
        }

        public void SetErase()
        {
            AudioManager.Instance.ChangeBrushSound_Erase();
            _paintController.SetErase();
        }

        // on individual pens
        public void SetColor(Color color) => _paintController.SetColor(color);
        public void SetTexture(int index) => _paintController.SetTexture(index);

        public void ClearPainting() => _paintController.ClearPainting();
        public void ClearDrawing() => _paintController.ClearDrawing();

        public void OnBackButtonPressed() => _paintController.ClearMemory();

        public bool IsDrawingEdited() => _paintController.IsDrawingEdited();
    }

    [System.Serializable]
    public struct InitPaintData
    {
        public int MaxHitColliders;
        public float BrushSize;
        public Color DefaultBrushColor;
        public Material[] BrushMaterials;
        public Texture2D[] Textures;

        public InitPaintData(int maxHitColliders, float brushSize, Color defaultBrushColor, Material[] brushMaterials, Texture2D[] textures)
        {
            MaxHitColliders = maxHitColliders;
            BrushSize = brushSize;
            DefaultBrushColor = defaultBrushColor;
            BrushMaterials = brushMaterials;
            Textures = textures;
        }
    }
}