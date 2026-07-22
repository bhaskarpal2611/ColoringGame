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
        private int _stationaryFrames = 0;
        private const int StationaryStopFrames = 10; // ~10 frames of no movement before cutting audio

        [SerializeField] private Camera _mainCamera;

        [Header("Helpers")]
        [SerializeField] private bool _loadOnStart, _forceOrthoCamera;

        private int _touchCount = 0;
        private int _maxTouchCount = 0;
        private bool _levelInitialized = false;

        public bool CanPaint { get; set; } = true;

        private void Awake()
        {

            if (_mainCamera == null)
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

        private void Start()
        {
            if (_loadOnStart && !_levelInitialized)
                OnLevelLoad();
        }

        private void OnEnable()
        {
            if (_forceOrthoCamera)
                _mainCamera.orthographic = true;

            _inputHandler.OnBeginDrag += BeginDrag;
            _inputHandler.OnDragging += OnDrag;
            _inputHandler.OnDragEnd += EndDrag;
            _inputHandler.OnDragStationary += OnDragStationary;

            UI_PencilItem.OnPenSelected += ChangePenColor;
        }
        private void OnDisable()
        {
            if (_mainCamera)
                _mainCamera.orthographic = false;

            _inputHandler.OnBeginDrag -= BeginDrag;
            _inputHandler.OnDragging -= OnDrag;
            _inputHandler.OnDragEnd -= EndDrag;
            _inputHandler.OnDragStationary -= OnDragStationary;

            UI_PencilItem.OnPenSelected -= ChangePenColor;
        }

        private void OnDestroy()
        {
            _paintController?.ClearMemory();
        }

        public void OnLevelLoad()
        {
            _levelInitialized = true;
            try
            {
                _paintController.InitializeLevel(_spritesContainer.GetChild(0).GetChild(0));
            }
            catch (Exception e)
            {
                Debug.Log($"Exception: {e}");
            }
        }

        public void OnEditedLevelLoad(LevelTextures levelTextures)
        {
            _levelInitialized = true;
            _paintController.InitializeLevel(_spritesContainer.GetChild(0).GetChild(0), levelTextures);
        }

        // SwipeToDraw - Load Blank or Level
        public void LoadDrawPaintLevel(Sprite originalSprite)
        {
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

        private void OnDrag(Vector2 touchPosition)
        {
            if (CanPaint)
            {
                _stationaryFrames = 0;
                Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(touchPosition);
                _paintController.ContinueDrag(worldPosition);

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
                _stationaryFrames++;
                if (_stationaryFrames >= StationaryStopFrames)
                    AudioManager.Instance.StopPaintingSound();
            }
        }

        private void EndDrag()
        {
            _stationaryFrames = 0;
            _paintController.EndDrag(); // Always clean up regardless of CanPaint — prevents stuck disabled colliders
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


        private void ChangePenColor(Color color)
        {
            _paintController.SetColor(color);
            _defaultBrushColor = color;
        }


        // on individual pens
        public void SetColor(Color color) => _paintController.SetColor(color);
        public void SetTexture(int index) => _paintController.SetTexture(index);

        public void ClearPainting() => _paintController.ClearPainting();
        public void ClearDrawing() => _paintController.ClearDrawing();

        public void OnBackButtonPressed()
        {
            _levelInitialized = false;
            _paintController.ClearMemory();
        }

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