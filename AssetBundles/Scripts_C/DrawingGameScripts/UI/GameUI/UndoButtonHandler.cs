using UnityEngine;
using UnityEngine.UI;

namespace DrawingGame
{
    public class UndoButtonHandler : MonoBehaviour
    {
        [SerializeField] private PaintService _paintService;
        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (_paintService != null && _paintService.CanUndo)
            {
                _paintService.Undo();
            }
        }
    }
}
