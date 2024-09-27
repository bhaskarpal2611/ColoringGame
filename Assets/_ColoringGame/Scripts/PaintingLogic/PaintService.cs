using UnityEngine;
using System;

namespace ColorSwipeGame
{
    public class PaintService : MonoBehaviour
    {
        [SerializeField] private Transform _spritesContainer;
        [SerializeField] private Texture2D[] _textures;
        [SerializeField] private InputHandler _inputHandler;
        [SerializeField] private PenSelectionHandler _penPanelHandler;
        [SerializeField] private AudioManager _audioHandler;

        [Header("Brush Settings")]
        [SerializeField] private int _maxHitColliders = 10;
        [SerializeField, Range(0f, 5f)] private int _brushSize = 1;
        [SerializeField, Range(0.01f, 0.001f)] private float _brushScaleFactor = 0.01f;
        [SerializeField] private Color _defaultBrushColor = Color.white;
        [SerializeField, Tooltip("Fill in order: PaintColor, then erase and then texturePaint")]
        private Material[] _customMaterials;

        private PaintController _paintController;
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Application.targetFrameRate = 60;

            InitPaintData newData = new InitPaintData(_maxHitColliders, _brushSize * _brushScaleFactor, _defaultBrushColor, _customMaterials, _textures);
            _paintController = new PaintController(newData, _penPanelHandler, _audioHandler);

            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<InputHandler>();
                if (_inputHandler == null)
                {
                    Debug.LogError("InputHandler not found. Please assign it in the inspector or add it to this GameObject.");
                    return;
                }
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

        private void BeginDrag(Vector2 touchPosition)
        {
            Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(touchPosition);
            _paintController.BeginDrag(worldPosition);
        }

        private void OnDrag(Vector2 touchPosition)
        {
            Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(touchPosition);
            _paintController.ContinueDrag(worldPosition);
        }

        private void OnDragStationary()
        {
            //_audioHandler.StopPaintingSound();
        }

        private void EndDrag()
        {
            _paintController.EndDrag();
        }

        // Public methods for other scripts to interact with PaintController
        public void SetBrushSize(int size) => _paintController.SetBrushScale(size * _brushScaleFactor);

        // on main pen selection or erase button
        public void SetDefaultColorMode() => _paintController.SetDefaultColor();
        public void SetDefaultTextureMode(int index = 0) => _paintController.SetDefaultTexture(index);
        public void SetErase() => _paintController.SetErase();

        // on individual pens
        public void SetColor(Color color) => _paintController.SetColor(color);
        public void SetTexture(int index) => _paintController.SetTexture(index);

        public void ClearPainting() => _paintController.ClearPainting();

        public void OnBackButtonPressed() => _paintController.ClearMemory();
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