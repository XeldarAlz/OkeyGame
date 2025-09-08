using System;
using UnityEngine;
using Runtime.Domain.ValueObjects;
using Cysharp.Threading.Tasks;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class SnapController : MonoBehaviour
    {
        [Header("Snap Configuration")]
        [SerializeField] private float _snapDistance = 50f;
        [SerializeField] private float _snapAnimationDuration = 0.2f;
        [SerializeField] private AnimationCurve _snapEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visual Feedback")]
        [SerializeField] private float _snapPreviewScale = 1.05f;
        [SerializeField] private Color _snapPreviewColor = Color.green;
        [SerializeField] private float _snapPreviewAlpha = 0.7f;

        private RackGridManager _gridManager;
        private Camera _mainCamera;

        public event Action<GridPosition> OnSnapPreview;
        public event Action OnSnapPreviewClear;
        public event Action<Transform, GridPosition> OnSnapComplete;

        public float SnapDistance => _snapDistance;
        public float SnapAnimationDuration => _snapAnimationDuration;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        public void Initialize(RackGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public bool IsWithinSnapDistance(Vector3 worldPosition, GridPosition gridPosition)
        {
            if (_gridManager == null)
            {
                return false;
            }

            Vector3 gridWorldPosition = _gridManager.GetWorldPosition(gridPosition);
            float distance = Vector3.Distance(worldPosition, gridWorldPosition);
            
            return distance <= _snapDistance;
        }

        public GridPosition GetSnapTarget(Vector3 worldPosition, bool onlyAvailable = true)
        {
            if (_gridManager == null)
            {
                return new GridPosition(-1, -1);
            }

            GridPosition nearestPosition = new GridPosition(-1, -1);
            float nearestDistance = float.MaxValue;

            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    
                    if (onlyAvailable && !_gridManager.IsPositionAvailable(position))
                    {
                        continue;
                    }

                    Vector3 gridWorldPosition = _gridManager.GetWorldPosition(position);
                    float distance = Vector3.Distance(worldPosition, gridWorldPosition);

                    if (distance < nearestDistance && distance <= _snapDistance)
                    {
                        nearestDistance = distance;
                        nearestPosition = position;
                    }
                }
            }

            return nearestPosition.Row >= 0 ? nearestPosition : new GridPosition(-1, -1);
        }

        public bool TryGetSnapTarget(Vector3 worldPosition, out GridPosition snapTarget, bool onlyAvailable = true)
        {
            snapTarget = GetSnapTarget(worldPosition, onlyAvailable);
            return snapTarget.Row >= 0 && snapTarget.Column >= 0;
        }

        public async UniTask<bool> SnapToPositionAsync(Transform target, GridPosition gridPosition)
        {
            if (_gridManager == null || target == null)
            {
                return false;
            }

            if (!_gridManager.IsValidPosition(gridPosition))
            {
                return false;
            }

            Vector3 targetWorldPosition = _gridManager.GetWorldPosition(gridPosition);
            Vector3 startPosition = target.position;
            Vector3 startScale = target.localScale;

            float elapsedTime = 0f;

            while (elapsedTime < _snapAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _snapAnimationDuration;
                float easedTime = _snapEaseCurve.Evaluate(normalizedTime);

                // Animate position
                target.position = Vector3.Lerp(startPosition, targetWorldPosition, easedTime);

                // Optional scale animation for feedback
                float scaleMultiplier = 1f + (Mathf.Sin(normalizedTime * Mathf.PI) * 0.05f);
                target.localScale = startScale * scaleMultiplier;

                await UniTask.Yield();
            }

            // Ensure final position and scale
            target.position = targetWorldPosition;
            target.localScale = startScale;

            OnSnapComplete?.Invoke(target, gridPosition);
            return true;
        }

        public async UniTask<bool> SnapWithBounceAsync(Transform target, GridPosition gridPosition, float bounceIntensity = 0.1f)
        {
            if (_gridManager == null || target == null)
            {
                return false;
            }

            if (!_gridManager.IsValidPosition(gridPosition))
            {
                return false;
            }

            Vector3 targetWorldPosition = _gridManager.GetWorldPosition(gridPosition);
            Vector3 startPosition = target.position;
            Vector3 startScale = target.localScale;

            float totalDuration = _snapAnimationDuration;
            float elapsedTime = 0f;

            while (elapsedTime < totalDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / totalDuration;

                // Position with overshoot and settle
                float positionProgress;
                if (normalizedTime < 0.7f)
                {
                    // Overshoot phase
                    float overshootTime = normalizedTime / 0.7f;
                    positionProgress = _snapEaseCurve.Evaluate(overshootTime) * (1f + bounceIntensity);
                }
                else
                {
                    // Settle phase
                    float settleTime = (normalizedTime - 0.7f) / 0.3f;
                    float overshootAmount = 1f + bounceIntensity;
                    positionProgress = Mathf.Lerp(overshootAmount, 1f, settleTime * settleTime);
                }

                target.position = Vector3.Lerp(startPosition, targetWorldPosition, positionProgress);

                // Scale bounce effect
                float scaleMultiplier = 1f + (Mathf.Sin(normalizedTime * Mathf.PI * 2f) * bounceIntensity * (1f - normalizedTime));
                target.localScale = startScale * scaleMultiplier;

                await UniTask.Yield();
            }

            // Ensure final position and scale
            target.position = targetWorldPosition;
            target.localScale = startScale;

            OnSnapComplete?.Invoke(target, gridPosition);
            return true;
        }

        public void ShowSnapPreview(GridPosition gridPosition)
        {
            if (!_gridManager.IsValidPosition(gridPosition))
            {
                return;
            }

            OnSnapPreview?.Invoke(gridPosition);
        }

        public void ClearSnapPreview()
        {
            OnSnapPreviewClear?.Invoke();
        }

        public Vector3 CalculateSnapVector(Vector3 currentPosition, GridPosition targetGridPosition)
        {
            if (_gridManager == null)
            {
                return Vector3.zero;
            }

            Vector3 targetWorldPosition = _gridManager.GetWorldPosition(targetGridPosition);
            return targetWorldPosition - currentPosition;
        }

        public float CalculateSnapDistance(Vector3 currentPosition, GridPosition targetGridPosition)
        {
            if (_gridManager == null)
            {
                return float.MaxValue;
            }

            Vector3 targetWorldPosition = _gridManager.GetWorldPosition(targetGridPosition);
            return Vector3.Distance(currentPosition, targetWorldPosition);
        }

        public bool IsValidSnapTarget(GridPosition gridPosition, bool checkAvailability = true)
        {
            if (_gridManager == null)
            {
                return false;
            }

            if (!_gridManager.IsValidPosition(gridPosition))
            {
                return false;
            }

            if (checkAvailability && !_gridManager.IsPositionAvailable(gridPosition))
            {
                return false;
            }

            return true;
        }

        public GridPosition[] GetNearbySnapTargets(Vector3 worldPosition, int maxTargets = 4)
        {
            if (_gridManager == null)
            {
                return new GridPosition[0];
            }

            System.Collections.Generic.List<(GridPosition position, float distance)> candidates = 
                new System.Collections.Generic.List<(GridPosition, float)>();

            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    
                    if (!_gridManager.IsPositionAvailable(position))
                    {
                        continue;
                    }

                    Vector3 gridWorldPosition = _gridManager.GetWorldPosition(position);
                    float distance = Vector3.Distance(worldPosition, gridWorldPosition);

                    if (distance <= _snapDistance)
                    {
                        candidates.Add((position, distance));
                    }
                }
            }

            // Sort by distance
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Return top candidates
            GridPosition[] result = new GridPosition[Mathf.Min(maxTargets, candidates.Count)];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = candidates[index].position;
            }

            return result;
        }

        public void SetSnapDistance(float newSnapDistance)
        {
            _snapDistance = Mathf.Max(0f, newSnapDistance);
        }

        public void SetSnapAnimationDuration(float newDuration)
        {
            _snapAnimationDuration = Mathf.Max(0.1f, newDuration);
        }

        private void OnValidate()
        {
            if (_snapDistance < 0f)
            {
                _snapDistance = 0f;
            }

            if (_snapAnimationDuration < 0.1f)
            {
                _snapAnimationDuration = 0.1f;
            }

            if (_snapPreviewScale < 0.1f)
            {
                _snapPreviewScale = 0.1f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_gridManager == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    Vector3 worldPosition = _gridManager.GetWorldPosition(position);
                    Gizmos.DrawWireSphere(worldPosition, _snapDistance);
                }
            }
        }
    }
}
