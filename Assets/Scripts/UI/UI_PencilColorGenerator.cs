using UnityEngine;
using DG.Tweening;

namespace ColoringGame.UI
{
    public class UI_PencilColorGenerator : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;

        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private Transform _pencilParent;

        [SerializeField] private GPU_SpriteColoring _spriteSelectionHandler;
            
        private void Start()
        {
            GeneratePencils();
            MoveUpPens();
        }

        public void MoveDownPens() => _pencilParent.DOMoveY(-600, 0.75f);
        public void MoveUpPens() => _pencilParent.DOMoveY(-250, 0.75f);

        private void GeneratePencils()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                UI_PencilItem pencil = Instantiate(_pencilPrefab, _pencilParent);
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