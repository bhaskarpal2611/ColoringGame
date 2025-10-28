using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ColorSwipeGame
{
    public class UI_TextureItem : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void OnDestroy()
        {
            Button.onClick.RemoveAllListeners();
        }

        public Button Button { get { return _button; } }

        public void OnTextureSelected()
        {
            transform.DOScale(1.25f, 0.25f);
            AudioManager.Instance.PlayClickSound();
        }

        public void UnselectedTexture() => transform.DOScale(01f, 0.25f);
    }
}
