using System;
using System.Collections.Generic;
using UnityEngine;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Presentation.Views.Grid;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Runtime.Presentation.Views
{
    public sealed class PlayerRackView : BaseView
    {
        [Header("Grid Components")]
        [SerializeField] private RackGridManager _gridManager;
        [SerializeField] private SnapController _snapController;

        [Header("Tile Prefab")]
        [SerializeField] private GameObject _tilePrefab;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _dropZoneHighlight;
        [SerializeField] private Color _validDropZoneColor = Color.green;
        [SerializeField] private Color _invalidDropZoneColor = Color.red;

        private readonly Dictionary<OkeyPiece, GameObject> _tileGameObjects = new Dictionary<OkeyPiece, GameObject>();
        private readonly Dictionary<GridPosition, GameObject> _dropZoneIndicators = new Dictionary<GridPosition, GameObject>();
        private readonly List<OkeyPiece> _currentTiles = new List<OkeyPiece>();

        public event Action<OkeyPiece, GridPosition> OnTilePlaced;
        public event Action<OkeyPiece, GridPosition, GridPosition> OnTileMoved;
        public event Action<OkeyPiece> OnTileSelected;
        public event Action<OkeyPiece> OnTileDeselected;
        public event Action OnRackCleared;

        public RackGridManager GridManager => _gridManager;
        public int TileCount => _currentTiles.Count;
        public bool IsFull => _gridManager != null && _gridManager.IsFull;
        public bool IsEmpty => _currentTiles.Count == 0;

        protected override void Initialize()
        {
            base.Initialize();
            
            if (_gridManager != null)
            {
                _gridManager.OnPiecePlaced += HandlePiecePlaced;
                _gridManager.OnPieceRemoved += HandlePieceRemoved;
                _gridManager.OnCellHighlighted += HandleCellHighlighted;
                _gridManager.OnCellUnhighlighted += HandleCellUnhighlighted;
            }

            if (_snapController != null)
            {
                _snapController.Initialize(_gridManager);
                _snapController.OnSnapPreview += HandleSnapPreview;
                _snapController.OnSnapPreviewClear += HandleSnapPreviewClear;
                _snapController.OnSnapComplete += HandleSnapComplete;
            }

            InitializeDropZoneIndicators();
        }

        private void InitializeDropZoneIndicators()
        {
            if (_dropZoneHighlight == null || _gridManager == null)
            {
                return;
            }

            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    GameObject indicator = Instantiate(_dropZoneHighlight, _gridManager.transform);
                    indicator.transform.position = _gridManager.GetWorldPosition(position);
                    indicator.SetActive(false);
                    _dropZoneIndicators[position] = indicator;
                }
            }
        }

        public async UniTask AddTileAsync(OkeyPiece piece, GridPosition? preferredPosition = null)
        {
            if (piece == null || _tileGameObjects.ContainsKey(piece))
            {
                return;
            }

            GridPosition targetPosition;
            if (preferredPosition.HasValue && _gridManager.IsPositionAvailable(preferredPosition.Value))
            {
                targetPosition = preferredPosition.Value;
            }
            else
            {
                List<GridPosition> availablePositions = _gridManager.GetAvailablePositions();
                if (availablePositions.Count == 0)
                {
                    return;
                }
                targetPosition = availablePositions[0];
            }

            GameObject tileObject = CreateTileGameObject(piece, targetPosition);
            if (tileObject != null)
            {
                _tileGameObjects[piece] = tileObject;
                _currentTiles.Add(piece);

                if (_gridManager.TryPlacePiece(piece, targetPosition))
                {
                    await AnimateTileToPosition(tileObject, targetPosition);
                }
            }
        }

        public async UniTask RemoveTileAsync(OkeyPiece piece)
        {
            if (piece == null || !_tileGameObjects.TryGetValue(piece, out GameObject tileObject))
            {
                return;
            }

            // Find the tile's current position
            GridPosition currentPosition = new GridPosition(-1, -1);
            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition pos = new GridPosition(row, column);
                    if (_gridManager.GetPieceAt(pos) == piece)
                    {
                        currentPosition = pos;
                        break;
                    }
                }
            }

            // Remove from grid
            if (currentPosition.Row >= 0 && _gridManager.TryRemovePiece(currentPosition, out OkeyPiece removedPiece))
            {
                // Animate tile removal
                await AnimateTileRemoval(tileObject);
            }

            // Clean up
            _tileGameObjects.Remove(piece);
            _currentTiles.Remove(piece);
            
            if (tileObject != null)
            {
                Destroy(tileObject);
            }
        }

        public async UniTask MoveTileAsync(OkeyPiece piece, GridPosition newPosition)
        {
            if (piece == null || !_tileGameObjects.TryGetValue(piece, out GameObject tileObject))
            {
                return;
            }

            GridPosition oldPosition = FindTilePosition(piece);
            if (oldPosition.Row < 0 || !_gridManager.IsPositionAvailable(newPosition))
            {
                return;
            }

            if (_gridManager.MovePiece(oldPosition, newPosition))
            {
                await AnimateTileToPosition(tileObject, newPosition);
                OnTileMoved?.Invoke(piece, oldPosition, newPosition);
            }
        }

        public void SetTiles(List<OkeyPiece> tiles)
        {
            ClearAllTiles();
            
            for (int index = 0; index < tiles.Count && index < RackGridManager.TOTAL_GRID_CELLS; index++)
            {
                OkeyPiece piece = tiles[index];
                GridPosition position = GetPositionForIndex(index);
                AddTileAsync(piece, position).Forget();
            }
        }

        public List<OkeyPiece> GetTiles()
        {
            return new List<OkeyPiece>(_currentTiles);
        }

        public void ClearAllTiles()
        {
            foreach (KeyValuePair<OkeyPiece, GameObject> kvp in _tileGameObjects)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }

            _tileGameObjects.Clear();
            _currentTiles.Clear();
            _gridManager?.ClearGrid();
            
            OnRackCleared?.Invoke();
        }

        public void HighlightValidDropZones(bool highlight)
        {
            if (_gridManager == null)
            {
                return;
            }

            List<GridPosition> availablePositions = _gridManager.GetAvailablePositions();
            
            foreach (GridPosition position in availablePositions)
            {
                if (_dropZoneIndicators.TryGetValue(position, out GameObject indicator))
                {
                    indicator.SetActive(highlight);
                    if (highlight)
                    {
                        SetDropZoneColor(indicator, _validDropZoneColor);
                    }
                }
            }
        }

        public void HighlightInvalidDropZones(List<GridPosition> invalidPositions)
        {
            foreach (GridPosition position in invalidPositions)
            {
                if (_dropZoneIndicators.TryGetValue(position, out GameObject indicator))
                {
                    indicator.SetActive(true);
                    SetDropZoneColor(indicator, _invalidDropZoneColor);
                }
            }
        }

        public void ClearAllHighlights()
        {
            foreach (GameObject indicator in _dropZoneIndicators.Values)
            {
                indicator.SetActive(false);
            }
            
            _gridManager?.ClearAllHighlights();
        }

        public GridPosition GetNearestAvailablePosition(Vector3 worldPosition)
        {
            return _gridManager?.GetNearestValidPosition(worldPosition) ?? new GridPosition(-1, -1);
        }

        public bool IsPositionAvailable(GridPosition position)
        {
            return _gridManager?.IsPositionAvailable(position) ?? false;
        }

        public OkeyPiece GetTileAtPosition(GridPosition position)
        {
            return _gridManager?.GetPieceAt(position);
        }

        private GameObject CreateTileGameObject(OkeyPiece piece, GridPosition position)
        {
            if (_tilePrefab == null || _gridManager == null)
            {
                return null;
            }

            Vector3 worldPosition = _gridManager.GetWorldPosition(position);
            GameObject tileObject = Instantiate(_tilePrefab, worldPosition, Quaternion.identity, _gridManager.transform);

            // Initialize TileView component
            TileView tileView = tileObject.GetComponent<TileView>();
            if (tileView != null)
            {
                tileView.Initialize(piece);
            }

            // Initialize DraggableTile component
            DraggableTile draggableTile = tileObject.GetComponent<DraggableTile>();
            if (draggableTile != null)
            {
                draggableTile.Initialize(_gridManager, piece, position);
                draggableTile.OnTileSelected += HandleTileSelected;
                draggableTile.OnTileDeselected += HandleTileDeselected;
                draggableTile.OnTileDropped += HandleTileDropped;
            }

            return tileObject;
        }

        private async UniTask AnimateTileToPosition(GameObject tileObject, GridPosition targetPosition)
        {
            if (tileObject == null || _gridManager == null)
            {
                return;
            }

            await _gridManager.AnimateToPositionAsync(tileObject.transform, targetPosition);
        }

        private async UniTask AnimateTileRemoval(GameObject tileObject)
        {
            if (tileObject == null)
            {
                return;
            }

            // Simple fade out animation
            CanvasGroup canvasGroup = tileObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float duration = 0.3f;
                float elapsedTime = 0f;
                float initialAlpha = canvasGroup.alpha;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, progress);
                    await UniTask.Yield();
                }

                canvasGroup.alpha = 0f;
            }
        }

        private GridPosition FindTilePosition(OkeyPiece piece)
        {
            if (_gridManager == null)
            {
                return new GridPosition(-1, -1);
            }

            for (int row = 0; row < RackGridManager.GRID_ROWS; row++)
            {
                for (int column = 0; column < RackGridManager.GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    if (_gridManager.GetPieceAt(position) == piece)
                    {
                        return position;
                    }
                }
            }

            return new GridPosition(-1, -1);
        }

        private GridPosition GetPositionForIndex(int index)
        {
            int row = index / RackGridManager.GRID_COLUMNS;
            int column = index % RackGridManager.GRID_COLUMNS;
            return new GridPosition(row, column);
        }

        private void SetDropZoneColor(GameObject indicator, Color color)
        {
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            else
            {
                Image image = indicator.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }

        private void HandlePiecePlaced(GridPosition position, OkeyPiece piece)
        {
            OnTilePlaced?.Invoke(piece, position);
        }

        private void HandlePieceRemoved(GridPosition position, OkeyPiece piece)
        {
            // Handle piece removal if needed
        }

        private void HandleCellHighlighted(GridPosition position)
        {
            if (_dropZoneIndicators.TryGetValue(position, out GameObject indicator))
            {
                indicator.SetActive(true);
                SetDropZoneColor(indicator, _validDropZoneColor);
            }
        }

        private void HandleCellUnhighlighted(GridPosition position)
        {
            if (_dropZoneIndicators.TryGetValue(position, out GameObject indicator))
            {
                indicator.SetActive(false);
            }
        }

        private void HandleTileSelected(DraggableTile tile)
        {
            OnTileSelected?.Invoke(tile.AssociatedPiece);
        }

        private void HandleTileDeselected(DraggableTile tile)
        {
            OnTileDeselected?.Invoke(tile.AssociatedPiece);
        }

        private void HandleTileDropped(DraggableTile tile, GridPosition position)
        {
            OnTilePlaced?.Invoke(tile.AssociatedPiece, position);
        }

        private void HandleSnapPreview(GridPosition position)
        {
            HandleCellHighlighted(position);
        }

        private void HandleSnapPreviewClear()
        {
            ClearAllHighlights();
        }

        private void HandleSnapComplete(Transform tileTransform, GridPosition position)
        {
            // Handle snap completion if needed
        }

        protected override void Cleanup()
        {
            if (_gridManager != null)
            {
                _gridManager.OnPiecePlaced -= HandlePiecePlaced;
                _gridManager.OnPieceRemoved -= HandlePieceRemoved;
                _gridManager.OnCellHighlighted -= HandleCellHighlighted;
                _gridManager.OnCellUnhighlighted -= HandleCellUnhighlighted;
            }

            if (_snapController != null)
            {
                _snapController.OnSnapPreview -= HandleSnapPreview;
                _snapController.OnSnapPreviewClear -= HandleSnapPreviewClear;
                _snapController.OnSnapComplete -= HandleSnapComplete;
            }

            ClearAllTiles();
            base.Cleanup();
        }
    }
}
