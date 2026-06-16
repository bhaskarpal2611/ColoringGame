using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ColorSwipeGame
{
    public class UI_TextureItem : MonoBehaviour
    {
        [SerializeField] private Button _button;
        private Image _selectionBorder;

        private void OnDestroy()
        {
            Button.onClick.RemoveAllListeners();
        }

        public Button Button { get { return _button; } }

        // Called from PensHandler after instantiation
        public void SetSelectionBorder(Image border)
        {
            _selectionBorder = border;
            if (_selectionBorder != null)
                _selectionBorder.enabled = false;
        }

        public void OnTextureSelected()
        {
            transform.DOScale(1.25f, 0.25f);
            if (_selectionBorder != null)
                _selectionBorder.enabled = true;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayClickSound();
        }

        public void UnselectedTexture()
        {
            transform.DOScale(1f, 0.25f);
            if (_selectionBorder != null)
                _selectionBorder.enabled = false;
        }
    }
}
