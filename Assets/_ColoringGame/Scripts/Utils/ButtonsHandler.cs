using UnityEngine;
using UnityEngine.UI;

namespace ColorSwipeGame
{
    public class ButtonsHandler : MonoBehaviour
    {
        [SerializeField] private AudioManager _audioManager;
        private Button[] _buttons;


        private void Start()
        {
            GetButtons();
            AddClickSoundToButtons();
        }
        private void GetButtons() {

            // Find all Button components in the scene and store them in the array
            _buttons = FindObjectsOfType<Button>();
        }

        private void AddClickSoundToButtons()
        {
            foreach (var button in _buttons)
            {
                button.onClick.AddListener(_audioManager.PlayClickSound);
            }
        }
    }
}
