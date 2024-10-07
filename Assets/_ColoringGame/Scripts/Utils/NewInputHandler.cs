using UnityEngine;
using System;

namespace ColorSwipeGame
{
    public class NewInputHandler : MonoBehaviour
    {
        [Header("Touch Sensitivity")]
        [Tooltip("Minimum distance (in pixels) the touch must move to register as a drag")]
        [Range(1f, 20f)]
        [SerializeField] private float minDragDistance = 5f;

        [Tooltip("Maximum distance (in pixels) to smooth out fast swipes")]
        [Range(10f, 100f)]
        [SerializeField] private float maxSmoothDistance = 50f;

        [Header("Speed Settings")]
        [Tooltip("The speed (in pixels per second) considered as maximum swipe speed")]
        [Range(500f, 5000f)]
        [SerializeField] private float maxSwipeSpeed = 2000f;

        [Header("Prediction and Smoothing")]
        [Tooltip("How far ahead to predict touch position (0 = no prediction, 1 = full prediction)")]
        [Range(0f, 1f)]
        [SerializeField] private float predictionStrength = 0.5f;

        [Tooltip("How much to smooth out the touch movement (0 = no smoothing, 10 = max smoothing)")]
        [Range(0f, 10f)]
        [SerializeField] private float smoothingStrength = 5f;

        public Action<Vector2> OnBeginDrag, OnDragging;
        public Action OnDragEnd, OnDragStationary;

        private Vector2 lastPosition;
        private Vector2 lastVelocity;
        private float lastTime;

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                float currentTime = Time.time;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch.position);
                        break;
                    case TouchPhase.Moved:
                        HandleTouchMoved(touch.position, currentTime);
                        break;
                    case TouchPhase.Stationary:
                        OnDragStationary?.Invoke();
                        break;
                    case TouchPhase.Ended:
                        HandleTouchEnded(touch.position);
                        break;
                }
            }
        }

        private void HandleTouchBegan(Vector2 position)
        {
            lastPosition = position;
            lastTime = Time.time;
            lastVelocity = Vector2.zero;
            OnBeginDrag?.Invoke(position);
        }

        private void HandleTouchMoved(Vector2 currentPosition, float currentTime)
        {
            float deltaTime = currentTime - lastTime;
            if (deltaTime > 0)
            {
                Vector2 currentVelocity = (currentPosition - lastPosition) / deltaTime;
                float speed = currentVelocity.magnitude;

                // Adaptive thresholding
                float minDistanceSquared = minDragDistance * minDragDistance;
                float maxDistanceSquared = maxSmoothDistance * maxSmoothDistance;
                float adaptiveThreshold = Mathf.Lerp(minDistanceSquared, maxDistanceSquared, speed / maxSwipeSpeed);

                // Smoothing
                Vector2 smoothedPosition = Vector2.Lerp(lastPosition, currentPosition, smoothingStrength * deltaTime);

                // Check if we've moved far enough to record a new point
                if ((smoothedPosition - lastPosition).sqrMagnitude >= adaptiveThreshold)
                {
                    // Touch prediction
                    Vector2 predictedPosition = PredictNextPosition(smoothedPosition, currentVelocity);

                    OnDragging?.Invoke(predictedPosition);

                    lastPosition = smoothedPosition;
                    lastVelocity = currentVelocity;
                    lastTime = currentTime;
                }
            }
        }

        private void HandleTouchEnded(Vector2 position)
        {
            OnDragEnd?.Invoke();
        }

        private Vector2 PredictNextPosition(Vector2 currentPosition, Vector2 currentVelocity)
        {
            Vector2 acceleration = (currentVelocity - lastVelocity) / (Time.time - lastTime);
            return currentPosition + currentVelocity * Time.deltaTime * predictionStrength
                                    + 0.5f * acceleration * Time.deltaTime * Time.deltaTime * predictionStrength;
        }
    }
}