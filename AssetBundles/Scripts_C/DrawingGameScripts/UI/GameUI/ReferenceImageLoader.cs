using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
{
    public class ReferenceImageLoader : MonoBehaviour
    {
        [SerializeField] private Sprite[] _referenceImages;

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        public void SetReferenceImage(int index)
        {
            if (index < 0 || index >= _referenceImages.Length)
            {
                Debug.LogError("Reference image array : index not found");
                return;
            }

            image.sprite = _referenceImages[index];
        }
    }
}
