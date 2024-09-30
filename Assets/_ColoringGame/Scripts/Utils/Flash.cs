using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ColorSwipeGame
{
    public class Flash : MonoBehaviour
    {
        public float _flashTimelength = .2f;
        public bool _doCameraFlash = false;
        private Image _flashImage;
        private float _startTime;
        private bool _flashing = false;
        void Start()
        {
            _flashImage = GetComponent<Image>();
            Color col = _flashImage.color;
            col.a = 0.0f;
            _flashImage.color = col;
        }

        void Update()
        {
            if (_doCameraFlash && !_flashing)
            {
                CameraFlash();
            }
            else
            {
                _doCameraFlash = false;
            }
        }

        public void CameraFlash()
        {
            // initial color
            Color col = _flashImage.color;

            // start time to fade over time
            _startTime = Time.time;

            // so we can flash again
            _doCameraFlash = false;

            // start it as alpha = 1.0 (opaque)
            col.a = 1.0f;

            // flash image start color
            _flashImage.color = col;

            // flag we are flashing so user can't do 2 of them
            _flashing = true;

            StartCoroutine(FlashCoroutine());
        }

        IEnumerator FlashCoroutine()
        {
            bool done = false;

            while (!done)
            {
                float perc;
                Color col = _flashImage.color;

                perc = Time.time - _startTime;
                perc = perc / _flashTimelength;

                if (perc > 1.0f)
                {
                    perc = 1.0f;
                    done = true;
                }

                col.a = Mathf.Lerp(1.0f, 0.0f, perc);
                _flashImage.color = col;
                _flashing = true;

                yield return null;
            }

            _flashing = false;

            yield break;
        }
    }
}