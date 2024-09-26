using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace ColorSwipeGame.UI
{
    public class PensHandler : MonoBehaviour
    {
        [SerializeField] private Color[] _colors;
        [SerializeField] private UI_PencilItem _pencilPrefab;
        [SerializeField] private UI_PencilItem _texturedPencilPrefab;
        [SerializeField] private UI_PencilItem _texturedPencilType2Prefab;
        [SerializeField] private RectTransform _pencilParent;
        [SerializeField] private ScrollRect _scrollViewReference;
        [SerializeField] private PaintService _paintService;
        [SerializeField] private PenSelectionHandler _mainMenuHandler;

        private const float XPOS_LEFT = 900f;
        private const float XPOS_RIGHT = -300f;

        private int _currentSelectedIndex;
        private UI_PencilItem _currentSelectedPen;
        public SelectedPenData SelectedPenData;

        private void Start()
        {
            // Default active and color
            _pencilParent.GetChild(0).gameObject.SetActive(true);
            SelectedPenData = new SelectedPenData(_colors.Length);

            GeneratePencils();
            GenerateTexturedPens();
            GenerateSecondTexturedPens();
            MoveLeft();

        }

        // Dotween movements

        public void MoveRight()
        {
            // Tween to the right (e.g., to 300 on the X-axis in 1 second)
            _pencilParent.DOAnchorPosX(XPOS_LEFT, .75f);
        }

        public void MoveLeft()
        {
            // Tween to the left (e.g., to -300 on the X-axis in 1 second)
            _pencilParent.DOAnchorPosX(XPOS_RIGHT, .25f);
        }

        // Function to scale up (pop open) a UI image or any GameObject
        public void PopOpen(Transform targetTransform, float targetScale = 1.0f, float duration = 0.3f)
        {
            // Start by setting the scale to zero (or any initial scale)
            targetTransform.localScale = Vector3.zero;

            // Animate the scale to the target scale value
            targetTransform.DOScale(targetScale, duration).SetEase(Ease.OutBack);
        }

        public void OnPenCategorySelection(int index)
        {
            if (_currentSelectedIndex == index) return;

            _scrollViewReference.content = _pencilParent.GetChild(index) as RectTransform;

            // move right current pens.
            // then move left the new pens selected

            _pencilParent.DOAnchorPosX(XPOS_LEFT, .25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _pencilParent.GetChild(_currentSelectedIndex).gameObject.SetActive(false);
                _pencilParent.GetChild(index).gameObject.SetActive(true);
                _currentSelectedIndex = index;
                MoveLeft();
            });
        }

        private void GeneratePencils()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                int index = i;
                SelectedPenData.ColoredPens[i] = Instantiate(_pencilPrefab, _pencilParent.GetChild(0));
                Color color = _colors[i];
                color.a = 1f;
                SelectedPenData.ColoredPens[i].SetColorOnPencil(color);
                SelectedPenData.ColoredPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetColor(color);
                    SelectedPenData.ColoredPens[index].OnPenSelected();
                    SelectedPenData.ColoredPenSelection(index);
                });
            }
        }
        private void GenerateTexturedPens()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                SelectedPenData.TexturedPens[i] = Instantiate(_texturedPencilPrefab, _pencilParent.GetChild(1));
                Color color = _colors[i];
                color.a = 1f;
                SelectedPenData.TexturedPens[i].SetColorOnPencil(color);
                int index = i;
                SelectedPenData.TexturedPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetTexture(0, color);
                    SelectedPenData.TexturedPens[index].OnPenSelected();
                    SelectedPenData.TexPenSelection(index);
                });
            }
        }
        private void GenerateSecondTexturedPens()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                SelectedPenData.SecondTexturedPens[i] = Instantiate(_texturedPencilType2Prefab, _pencilParent.GetChild(2));
                Color color = _colors[i];
                color.a = 1f;
                SelectedPenData.SecondTexturedPens[i].SetColorOnPencil(color);
                int index = i;

                SelectedPenData.SecondTexturedPens[i].Button.onClick.AddListener(() =>
                {
                    _paintService.SetTexture(1, color);
                    SelectedPenData.SecondTexturedPens[index].OnPenSelected();
                    SelectedPenData.Tex2PenSelection(index);
                });
            }
        }
    }

    [System.Serializable]
    public struct SelectedPenData
    {
        // 3 arrays of pens
        public UI_PencilItem[] ColoredPens;
        public UI_PencilItem[] TexturedPens;
        public UI_PencilItem[] SecondTexturedPens;

        // 3 indexes to select the current selected ones
        public int CurrentSelectedPen_1;
        public int CurrentSelectedPen_2;
        public int CurrentSelectedPen_3;

        // function to call this pen's OnPenSelected
        // And call Unselected For rest.

        public SelectedPenData(int len)
        {
            CurrentSelectedPen_1 = -1;
            CurrentSelectedPen_2 = -1;
            CurrentSelectedPen_3 = -1;

            ColoredPens = new UI_PencilItem[len];
            TexturedPens = new UI_PencilItem[len];
            SecondTexturedPens = new UI_PencilItem[len];
        }

        public void ColoredPenSelection(int index)
        {
            if (CurrentSelectedPen_1 == index) return;

            for (int i = 0; i < ColoredPens.Length; i++)
            {
                if (i == index)
                {
                    ColoredPens[i].OnPenSelected();
                    CurrentSelectedPen_1 = i;
                }
                else 
                {
                    ColoredPens[i].UnselectedPen();
                }
            }
        }
        public void TexPenSelection(int index)
        {
            if (CurrentSelectedPen_2 == index) return;

            for (int i = 0; i < TexturedPens.Length; i++)
            {
                if (i == index)
                {
                    // call selected
                    TexturedPens[i].OnPenSelected();
                    CurrentSelectedPen_2 = i;
                }
                else
                {
                    TexturedPens[i].UnselectedPen();
                }
            }
        }
        public void Tex2PenSelection(int index)
        {
            if (CurrentSelectedPen_3 == index) return;

            for (int i = 0; i < SecondTexturedPens.Length; i++)
            {
                if (i == index)
                {
                    // call selected
                    SecondTexturedPens[i].OnPenSelected();
                    CurrentSelectedPen_3 = i;
                }
                else
                {
                    SecondTexturedPens[i].UnselectedPen();
                }
            }
        }
    }
}