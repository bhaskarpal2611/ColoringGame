using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace ColorSwipeGame

{
    public class BezierFollow : MonoBehaviour
    {
        [SerializeField] private Transform _initialPathParent;
        [SerializeField] private Transform _coloringPathParent;
        [SerializeField] private InputHandler _inputHandler;
        [SerializeField] private float _speedModifier = 0.5f;
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private float _scaleDownFactor = 0.9f;


        private List<Vector3> _cachedPathPoints;
        private int _quarterIndex = 0;
        private Vector2 _originalScale;

        private bool _isPaused = false;
        private Tween _tween;

        public static event System.Action OnPathStart, OnPathEnd;


        private void Start()
        {
            _originalScale = transform.localScale;

        }

        public void StartTutorial()
        {
            // spawn hand and start move
            transform.DOScale(_originalScale, 0.75f).From(Vector3.zero);

            // 1. Move to Crayon, select & move to Start
            InitialMoveToPenLocation(_initialPathParent);
        }

        private void InitialMoveToPenLocation(Transform pathParent)
        {
            if (pathParent.position.x < transform.position.x)
            {
                Debug.Log("Passed - no revolve");
                return;
            }

            //get routes parent and pass as array.
            Transform[] temp = new Transform[2];

            temp[0] = pathParent.transform.GetChild(0) as Transform;
            temp[1] = pathParent.transform.GetChild(1) as Transform;

            StartPlayingRoute(temp);
        }

        private void ColoringAction(Transform pathParent)
        {
            if (pathParent.position.x < transform.position.x)
            {
                Debug.Log("Passed - no revolve");
                return;
            }

            //get routes parent and pass as array.
            Transform[] temp = new Transform[2];

            temp[0] = pathParent.transform.GetChild(0);
            temp[1] = pathParent.transform.GetChild(1);

            StartPlayingRoute(temp);
        }

        private void StartPlayingRoute(Transform[] routes)
        {
            if (routes == null || routes.Length == 0)
            {
                Debug.LogError("Routes are not set correctly");
                return;
            }

            // Precompute and cache the path points
            _cachedPathPoints = PrecomputeBezierPoints(routes);

            Debug.Log("Path Points: " + _cachedPathPoints.Count);

            // Use DOTween to follow the path
            OnPathStart?.Invoke();
            _quarterIndex = 0;

            int halfwaypoint = _cachedPathPoints.Count / 2;

            _tween = transform.DOPath(_cachedPathPoints.ToArray(), _cachedPathPoints.Count / _speedModifier, PathType.Linear)
                .SetEase(Ease.Linear)
                .OnWaypointChange(index =>
                {
                    if (index == halfwaypoint && !_isPaused)
                    {
                        _tween.Pause();
                        _isPaused = true;
                        Debug.Log("Tween Paused at Halfway Point");
                        PlayTapAnimation();
                    }
                })
                .OnComplete(() =>
                {
                    ColoringAction(_coloringPathParent);
                });
        }
        private void ColoringRoute(Transform[] routes)
        {
            if (routes == null || routes.Length == 0)
            {
                Debug.LogError("Routes are not set correctly");
                return;
            }

            // Precompute and cache the path points
            _cachedPathPoints = PrecomputeBezierPoints(routes);

            Debug.Log("Path Points: " + _cachedPathPoints.Count);

            // Use DOTween to follow the path
            OnPathStart?.Invoke();
            _quarterIndex = 0;

            int halfwaypoint = _cachedPathPoints.Count / 2;

            _tween = transform.DOPath(_cachedPathPoints.ToArray(), _cachedPathPoints.Count / _speedModifier, PathType.Linear)
                .SetEase(Ease.Linear)
                .OnWaypointChange(index =>
                {
                    if (index == halfwaypoint && !_isPaused)
                    {
                        _tween.Pause();
                        _isPaused = true;
                        Debug.Log("Tween Paused at Halfway Point");
                        PlayTapAnimation();
                    }
                })
                .OnStart(() =>
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
                    _inputHandler.BeginDrag(screenPos);
                })
                .OnUpdate(() =>
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
                    _inputHandler.HandleMove(screenPos);
                })
                .OnComplete(() =>
                {
                    _inputHandler.EndDrag();
                });
        }

        private List<Vector3> PrecomputeBezierPoints(Transform[] routes)
        {
            List<Vector3> pathPoints = new List<Vector3>();

            foreach (var route in routes)
            {
                if (route.childCount < 4)
                {
                    Debug.LogError("Each route must have exactly 4 control points");
                    continue;
                }

                // Get the control points in world space
                Vector3 p0 = route.GetChild(0).position;
                Vector3 p1 = route.GetChild(1).position;
                Vector3 p2 = route.GetChild(2).position;
                Vector3 p3 = route.GetChild(3).position;

                // Calculate intermediate points along the Bezier curve
                int segments = 50; // Increase for smoother paths

                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = Mathf.Pow(1 - t, 3) * p0 +
                                    3 * Mathf.Pow(1 - t, 2) * t * p1 +
                                    3 * (1 - t) * Mathf.Pow(t, 2) * p2 +
                                    Mathf.Pow(t, 3) * p3;
                    pathPoints.Add(point);
                }
            }

            return pathPoints;
        }

        public void PlayTapAnimation()
        {
            // Shrink and return to original size in a single tween
            transform.DOScale(_originalScale * _scaleDownFactor, _animationDuration / 2)
                    .OnComplete(() =>
                    {
                        transform.DOScale(_originalScale, _animationDuration / 2).OnComplete(() =>
                        {
                            ResumeTween();
                        });
                    });
        }

        public void ResumeTween()
        {
            if (_isPaused)
            {
                _tween.Play();
                _isPaused = false;
                Debug.Log("Tween Resumed");
            }
        }
    }
}
