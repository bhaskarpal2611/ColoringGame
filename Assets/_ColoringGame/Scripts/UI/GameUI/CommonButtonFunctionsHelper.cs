using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ColorSwipeGame.UI
{
    public class CommonButtonFunctionsHandler : MonoBehaviour
    {
        public UnityEvent OnButtonClick;
        private Button[] _buttons;

        private void AddClickSoundToButtons()
        {
            foreach (var button in _buttons)
            {
                button.onClick.AddListener(OnButtonClick.Invoke);
            }
        }

        public void LoadButtonsAll()
        {
            // Find all Button components in the scene and store them in the array
            _buttons = FindObjectsOfType<Button>();

            AddClickSoundToButtons();
        }

    }
}
