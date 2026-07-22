using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

namespace ColorSwipeGame
{
    public class InputHandler : MonoBehaviour
    {
        public Action<Vector2> OnBeginDrag;
        public Action<Vector2> OnDragging;
        public Action OnDragEnd, OnDragStationary;

        public bool IsTouchEnabled { get; set; } = true;

        [SerializeField] private float _minDistance = 2f; 

        private Vector2 _lastPosition;
        private int _activeFingerId = -1;

        private void Update()
        {
            if (Input.touchCount <= 0) return;

            if (IsTouchEnabled && Input.touchCount > 0)
            {
                // Find and process the correct touch
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    if (touch.phase == TouchPhase.Began && _activeFingerId == -1)
                    {
                        // Lock to the first finger that touches the screen
                        _activeFingerId = touch.fingerId;
                        BeginDrag(touch.position);
                    }
                    else if (touch.fingerId == _activeFingerId)
                    {
                        // Only process this specific finger's movements to prevent palm snapping
                        switch (touch.phase)
                        {
                            case TouchPhase.Moved:
                            case TouchPhase.Stationary: // Handle stationary as move to update position continuously if needed, though usually standard
                                HandleMove(touch.position);
                                break;
                            case TouchPhase.Ended:
                            case TouchPhase.Canceled:
                                EndDrag();
                                break;
                        }
                    }
                }
            }
        }

        public void EndDrag()
        {
            _activeFingerId = -1;
            OnDragEnd?.Invoke();
        }

        public void BeginDrag(Vector2 currentPosition)
        {
            _lastPosition = currentPosition;
            OnBeginDrag?.Invoke(currentPosition);
        }

        public void HandleMove(Vector2 currentPosition)
        {
            // Use sqrMagnitude for fast distance checking instead of Vector2.Distance (which uses expensive square roots)
            float distanceSqr = (_lastPosition - currentPosition).sqrMagnitude;
            
            if (distanceSqr >= (_minDistance * _minDistance))
            {
                OnDragging?.Invoke(currentPosition);
                _lastPosition = currentPosition;
            }
            else
            {
                OnDragStationary?.Invoke();
            }
        }
    }
}