using UnityEngine;

namespace ColorSwipeGame
{
    public class CaptureCameraResizer : MonoBehaviour
    {
        [SerializeField] private float _phoneSize = 2f;
        [SerializeField] private float _tabletSize = 3.25f;
        [SerializeField] private Camera _captureCamera;
        [SerializeField] private GameMode _gameMode = GameMode.Draw;

        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;

            float aspectRatio = CalculateScreenAspectRatio();

            if (_gameMode == GameMode.Draw)
            {
                if (aspectRatio > 1.5f)
                {
                    _camera.orthographicSize = _phoneSize;
                    _captureCamera.orthographicSize = _phoneSize;
                    _captureCamera.transform.position = new Vector3(-3.06f, -0.39f, -10f);
                    _captureCamera.rect = new Rect(0, 0.45f, 1, 1);
                }
                else
                {
                    _camera.orthographicSize = _tabletSize;
                    _captureCamera.orthographicSize = _tabletSize;
                    _captureCamera.transform.position = new Vector3(-3.06f, -0.39f, -10f);
                    _captureCamera.rect = new Rect(0f, 0f, 1, 1);
                }
            }

            if (_gameMode == GameMode.Color)
            {
                if (aspectRatio > 1.5f)
                {
                    _camera.orthographicSize = _phoneSize;
                }
                else
                {
                    _camera.orthographicSize = _tabletSize;
                }
            }
        }

        private float CalculateScreenAspectRatio()
        {
            float width = Screen.width;
            float height = Screen.height;
            return width / height;
        }
    }
}
