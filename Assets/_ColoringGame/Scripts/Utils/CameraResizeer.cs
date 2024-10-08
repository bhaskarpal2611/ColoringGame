using UnityEngine;

namespace ColorSwipeGame
{
    public class CameraResizeer : MonoBehaviour
    {
        [SerializeField] private float _phoneSize = 2f;
        [SerializeField] private float _tabletSize = 3.25f;
        [SerializeField] private Camera _captureCamera;

        private Camera _camera; 

        private void Start()
        {
            _camera = GetComponent<Camera>();

            float aspectRatio = CalculateScreenAspectRatio();
            if (aspectRatio > 1.5f)
            {
                _camera.orthographicSize = _phoneSize;
                _captureCamera.orthographicSize = _phoneSize;
                _captureCamera.transform.position = new Vector3(-1f, 0f, -10f);
                _captureCamera.rect = new Rect(0, 0.465f, 1, 1);
            }
            else
            {
                _camera.orthographicSize = _tabletSize;
                _captureCamera.orthographicSize = _tabletSize;
                _captureCamera.transform.position = new Vector3(-1.25f, 0f, -10f);
                _captureCamera.rect = new Rect(0, 0.1f, 1, 1);
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
