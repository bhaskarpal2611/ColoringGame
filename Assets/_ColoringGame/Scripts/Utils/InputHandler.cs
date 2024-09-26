using UnityEngine;
using System;
using System.Collections.Generic;

namespace ColorSwipeGame
{
    public class InputHandler : MonoBehaviour
    {
        public Action<Vector2> OnBeginDrag, OnDragging;
        public Action OnDragEnd, OnDragStationary;

        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxFrequency = 0.2f;

        private Touch _touch;
        private Vector2 _lastPosition;
        private float _lastUpdateTime;
        private Queue<Vector2> _positionBuffer = new();
        private const int BufferSize = 3;

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
        }

        private void HandleMove(Vector2 currentPosition)
        {
            float distance = Vector2.Distance(_lastPosition, currentPosition);
            float timeSinceLastUpdate = Time.time - _lastUpdateTime;

            if (distance >= minDistance || timeSinceLastUpdate >= maxFrequency)
            {
                // Update the position buffer
                _positionBuffer.Dequeue();
                _positionBuffer.Enqueue(currentPosition);

                // Calculate the average position
                Vector2 averagePosition = Vector2.zero;
                foreach (Vector2 pos in _positionBuffer)
                {
                    averagePosition += pos;
                }
                averagePosition /= BufferSize;

                OnDragging?.Invoke(averagePosition);
                _lastPosition = currentPosition;
                _lastUpdateTime = Time.time;
            }
        }
    }
}