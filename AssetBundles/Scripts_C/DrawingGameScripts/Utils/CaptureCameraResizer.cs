using UnityEngine;

namespace DrawingGame
{
    public class CaptureCameraResizer : MonoBehaviour
    {
        [SerializeField] private float _phoneSize = 2f;
        [SerializeField] private float _tabletSize = 3.25f;
        [SerializeField] private GameMode _gameMode = GameMode.Draw;

        private void Awake()
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            float mainSize    = aspectRatio > 1.5f ? _phoneSize : _tabletSize;
            Camera.main.orthographicSize = mainSize;
        }
    }
}
