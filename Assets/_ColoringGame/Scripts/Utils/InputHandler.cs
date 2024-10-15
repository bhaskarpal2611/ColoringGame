using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

namespace ColorSwipeGame
{
    public class InputHandler : MonoBehaviour
    {
        public Action<Vector2> OnBeginDrag;
        public Action<Vector2, bool> OnDragging;
        public Action OnDragEnd, OnDragStationary;

        [SerializeField] private float _minDistance = 5f;
        [SerializeField] private float _maxFrequency = 0.2f;
        [SerializeField] private float _fastSwipeThreshold = 1000f; // Units per second
        [SerializeField] private float _velocityMeasurementPeriod = 0.1f; // Seconds
        [SerializeField] private TextMeshProUGUI _textMeshProUGUI;


        private Touch _touch;
        private Vector2 _lastPosition;
        private float _lastUpdateTime;
        private Queue<Vector2> _positionBuffer = new();
        private const int BufferSize = 3;

        private Vector2 _velocityStartPosition;
        private float _velocityStartTime;
        private bool _fastSwipeFlag;

        private void Update()
        {
            if (Input.touchCount <= 0) return;

            _touch = Input.GetTouch(0);

            switch (_touch.phase)
            {
                case TouchPhase.Began:
                    OnBeginDrag?.Invoke(_touch.position);
                    ResetTracking(_touch.position);
                    break;
                case TouchPhase.Moved:
                    HandleMove(_touch.position);
                    break;
                case TouchPhase.Stationary:
                    OnDragStationary?.Invoke();
                    break;
                case TouchPhase.Ended:
                    OnDragEnd?.Invoke();
                    break;
            }
        }

        private void ResetTracking(Vector2 position)
        {
            _lastPosition = position;
            _lastUpdateTime = Time.time;
            _positionBuffer.Clear();
            for (int i = 0; i < BufferSize; i++)
            {
                _positionBuffer.Enqueue(position);
            }
            _velocityStartPosition = position;
            _velocityStartTime = Time.time;
            _fastSwipeFlag = false;
        }

        private void HandleMove(Vector2 currentPosition)
        {
            float distance = Vector2.Distance(_lastPosition, currentPosition);
            float timeSinceLastUpdate = Time.time - _lastUpdateTime;

            // Calculate velocity
            float timeSinceVelocityStart = Time.time - _velocityStartTime;
            if (timeSinceVelocityStart >= _velocityMeasurementPeriod)
            {
                Vector2 displacement = currentPosition - _velocityStartPosition;
                float velocity = displacement.magnitude / timeSinceVelocityStart;
                _fastSwipeFlag = velocity >= _fastSwipeThreshold;

                // Reset velocity measurement
                _velocityStartPosition = currentPosition;
                _velocityStartTime = Time.time;

                if(_textMeshProUGUI)
                _textMeshProUGUI.text = $"Velocity: {velocity:F2}, Fast: {_fastSwipeFlag}";

            }

            if (distance >= _minDistance || timeSinceLastUpdate >= _maxFrequency)
            {
                // Update the position buffer
                if (_positionBuffer.Count >= BufferSize)
                    _positionBuffer.Dequeue();
                _positionBuffer.Enqueue(currentPosition);

                // Calculate the average position
                Vector2 averagePosition = Vector2.zero;
                foreach (Vector2 pos in _positionBuffer)
                {
                    averagePosition += pos;
                }
                averagePosition /= BufferSize;

                OnDragging?.Invoke(averagePosition, true);
                _lastPosition = currentPosition;
                _lastUpdateTime = Time.time;
            }
        }
    }
}