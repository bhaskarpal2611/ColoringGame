using UnityEngine;

namespace ColoringGame.UI
{
    public class UI_PencilColorGenerator : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;

        [SerializeField] private UI_PencilItem _pencilPrefab;

        [SerializeField] private GPU_SpriteColoring _spriteSelectionHandler;

        private void Start()
        {
            GeneratePencils();
        }

        private void GeneratePencils()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                UI_PencilItem pencil = Instantiate(_pencilPrefab, transform);
                Color color = _colors[i];
                color.a = 1f;
                pencil.PickColor(color);
                pencil.Button.onClick.AddListener(() =>
                {
                    Debug.Log("color: " + color);
                    _spriteSelectionHandler.CurrentBrushColor = color;
                });
            }
        }

    }
}