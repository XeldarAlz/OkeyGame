using System;
using UnityEngine;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using Runtime.Domain.Models;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class DraggableTile : MonoBehaviour
    {
        [Header("Drag Configuration")]
        [SerializeField] private float _dragThreshold = 10f;
        [SerializeField] private float _snapAnimationDuration = 0.2f;
        [SerializeField] private LayerMask _dragLayer = -1;

        [Header("Visual Configuration")]
        [SerializeField] private float _dragElevation = 50f;
        [SerializeField] private Vector3 _dragScale = Vector3.one * 1.1f;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Camera _mainCamera;
        private Canvas _parentCanvas;
        private RackGridManager _gridManager;
        private Transform _originalParent;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private GridPosition _currentGridPosition;
        private DragState _currentDragState;
        private OkeyPiece _associatedPiece;

        private Vector2 _initialPointerPosition;
        private Vector2 _currentPointerPosition;
        private bool _isDragStarted;
        private bool _isPointerDown;

        public event Action<DraggableTile, DragState> OnDragStateChanged;
        public event Action<DraggableTile, GridPosition> OnTileDropped;
        public event Action<DraggableTile> OnTileSelected;
        public event Action<DraggableTile> OnTileDeselected;

        public DragState CurrentDragState => _currentDragState;
        public GridPosition CurrentGridPosition => _currentGridPosition;
        public OkeyPiece AssociatedPiece => _associatedPiece;
        public bool IsDragging => _currentDragState == DragState.Dragging;
        public bool IsSelected => _currentDragState == DragState.Selected;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _parentCanvas = GetComponentInParent<Canvas>();
            _originalScale = transform.localScale;
            
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            SetDragState(DragState.None);
        }

        public void Initialize(RackGridManager gridManager, OkeyPiece piece, GridPosition initialPosition)
        {
            _gridManager = gridManager;
            _associatedPiece = piece;
            _currentGridPosition = initialPosition;
            _originalParent = transform.parent;
            _originalPosition = transform.position;
        }

        public void OnPointerDown(InputAction.CallbackContext context)
        {
            if (!context.performed || _currentDragState == DragState.Dragging)
            {
                return;
            }

            _isPointerDown = true;
            _isDragStarted = false;
            
            Vector2 screenPosition = GetPointerScreenPosition();
            _initialPointerPosition = screenPosition;
            _currentPointerPosition = screenPosition;

            SetDragState(DragState.Selected);
            OnTileSelected?.Invoke(this);
        }

        public void OnPointerUp(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            _isPointerDown = false;

            if (_currentDragState == DragState.Dragging)
            {
                HandleDrop();
            }
            else if (_currentDragState == DragState.Selected)
            {
                SetDragState(DragState.None);
                OnTileDeselected?.Invoke(this);
            }
        }

        public void OnPointerMove(InputAction.CallbackContext context)
        {
            if (!_isPointerDown)
            {
                return;
            }

            Vector2 screenPosition = GetPointerScreenPosition();
            _currentPointerPosition = screenPosition;

            float dragDistance = Vector2.Distance(_initialPointerPosition, _currentPointerPosition);

            if (!_isDragStarted && dragDistance > _dragThreshold)
            {
                StartDrag();
            }

            if (_isDragStarted && _currentDragState == DragState.Dragging)
            {
                UpdateDragPosition();
            }
        }

        private Vector2 GetPointerScreenPosition()
        {
            Vector2 pointerPosition = Vector2.zero;
            
            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            return pointerPosition;
        }

        private void StartDrag()
        {
            _isDragStarted = true;
            SetDragState(DragState.Dragging);

            // Move to drag layer for proper rendering order
            if (_parentCanvas != null)
            {
                transform.SetParent(_parentCanvas.transform, true);
                transform.SetAsLastSibling();
            }

            // Apply drag visual effects
            transform.localScale = _dragScale;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.8f;
            }

            // Remove from current grid position
            if (_gridManager != null)
            {
                _gridManager.TryRemovePiece(_currentGridPosition, out OkeyPiece removedPiece);
                _gridManager.ClearAllHighlights();
                HighlightValidDropZones();
            }
        }

        private void UpdateDragPosition()
        {
            if (_mainCamera == null)
            {
                return;
            }

            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(
                _currentPointerPosition.x, 
                _currentPointerPosition.y, 
                _mainCamera.WorldToScreenPoint(transform.position).z));

            transform.position = worldPosition;

            // Update hover state for grid cells
            if (_gridManager != null)
            {
                GridPosition nearestPosition = _gridManager.GetGridPosition(worldPosition);
                if (_gridManager.IsPositionAvailable(nearestPosition))
                {
                    SetDragState(DragState.Hovering);
                }
                else
                {
                    SetDragState(DragState.Dragging);
                }
            }
        }

        private void HandleDrop()
        {
            if (_gridManager == null)
            {
                ResetToOriginalPosition();
                return;
            }

            Vector3 worldPosition = transform.position;
            GridPosition targetPosition = _gridManager.GetNearestValidPosition(worldPosition);

            if (_gridManager.IsPositionAvailable(targetPosition))
            {
                DropAtPosition(targetPosition);
            }
            else
            {
                ResetToOriginalPosition();
            }
        }

        private async void DropAtPosition(GridPosition targetPosition)
        {
            SetDragState(DragState.Snapping);

            // Place piece in grid
            if (_gridManager.TryPlacePiece(_associatedPiece, targetPosition))
            {
                _currentGridPosition = targetPosition;
                
                // Animate to final position
                await _gridManager.AnimateToPositionAsync(transform, targetPosition, _snapAnimationDuration);
                
                // Reset visual state
                ResetVisualState();
                
                // Return to original parent
                if (_originalParent != null)
                {
                    transform.SetParent(_originalParent, true);
                }

                SetDragState(DragState.Dropped);
                OnTileDropped?.Invoke(this, targetPosition);
                
                // Clear highlights
                _gridManager.ClearAllHighlights();
                
                // Final state
                SetDragState(DragState.None);
            }
            else
            {
                ResetToOriginalPosition();
            }
        }

        private async void ResetToOriginalPosition()
        {
            SetDragState(DragState.Snapping);

            // Place piece back in original position
            if (_gridManager != null)
            {
                _gridManager.TryPlacePiece(_associatedPiece, _currentGridPosition);
                await _gridManager.AnimateToPositionAsync(transform, _currentGridPosition, _snapAnimationDuration);
            }
            else
            {
                // Fallback to original world position
                Vector3 startPosition = transform.position;
                float elapsedTime = 0f;

                while (elapsedTime < _snapAnimationDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / _snapAnimationDuration;
                    float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                    
                    transform.position = Vector3.Lerp(startPosition, _originalPosition, easedProgress);
                    await UniTask.Yield();
                }

                transform.position = _originalPosition;
            }

            // Reset visual state
            ResetVisualState();
            
            // Return to original parent
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, true);
            }

            // Clear highlights
            if (_gridManager != null)
            {
                _gridManager.ClearAllHighlights();
            }

            SetDragState(DragState.None);
        }

        private void ResetVisualState()
        {
            transform.localScale = _originalScale;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private void HighlightValidDropZones()
        {
            if (_gridManager == null)
            {
                return;
            }

            foreach (GridPosition position in _gridManager.GetAvailablePositions())
            {
                _gridManager.SetValidDropTarget(position, true);
                _gridManager.HighlightCell(position, true);
            }
        }

        private void SetDragState(DragState newState)
        {
            if (_currentDragState == newState)
            {
                return;
            }

            DragState previousState = _currentDragState;
            _currentDragState = newState;
            
            OnDragStateChanged?.Invoke(this, newState);
        }

        public void SetGridPosition(GridPosition newPosition)
        {
            _currentGridPosition = newPosition;
            if (_gridManager != null)
            {
                Vector3 worldPosition = _gridManager.GetWorldPosition(newPosition);
                transform.position = worldPosition;
                _originalPosition = worldPosition;
            }
        }

        public async UniTask MoveToGridPositionAsync(GridPosition targetPosition, float duration = 0.3f)
        {
            if (_gridManager == null)
            {
                return;
            }

            if (_gridManager.MovePiece(_currentGridPosition, targetPosition))
            {
                _currentGridPosition = targetPosition;
                await _gridManager.AnimateToPositionAsync(transform, targetPosition, duration);
                _originalPosition = transform.position;
            }
        }

        public void ForceSetPosition(GridPosition position)
        {
            _currentGridPosition = position;
            if (_gridManager != null)
            {
                Vector3 worldPosition = _gridManager.GetWorldPosition(position);
                transform.position = worldPosition;
                _originalPosition = worldPosition;
            }
        }

        private void OnDestroy()
        {
            if (_gridManager != null && _associatedPiece != null)
            {
                _gridManager.TryRemovePiece(_currentGridPosition, out OkeyPiece removedPiece);
            }
        }

        private void OnValidate()
        {
            if (_dragThreshold < 0f)
            {
                _dragThreshold = 0f;
            }

            if (_snapAnimationDuration < 0.1f)
            {
                _snapAnimationDuration = 0.1f;
            }
        }
    }
}
