using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class LevelImageHandler : MonoBehaviour
    {
        [SerializeField] private LevelImageSO _sprites;
        [SerializeField] private CaptureCameraRender _cameraScreenshotController;

        private Image[] _images;

        private void Awake()
        {
            _images = new Image[transform.childCount];
            int i = 0;
            foreach(Transform child in transform)
            {
                _images[i++] = child.GetChild(0).GetComponent<Image>();
            }
        }
        private void OnEnable()
        {
            LoadSprites();            
        }

        public void UpdateSprite()
        {
            Texture2D texture = _cameraScreenshotController.SaveTextureCopy();
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            _sprites.data.LevelSprites[0] = Sprite.Create(texture, rect, Vector2.one * 0.5f);
        }

        private void LoadSprites()
        {
            for(int i = 0; i < _sprites.data.LevelSprites.Length; i++)
            {
                _images[i].sprite = _sprites.data.LevelSprites[i];
            }
        }
    }
}
