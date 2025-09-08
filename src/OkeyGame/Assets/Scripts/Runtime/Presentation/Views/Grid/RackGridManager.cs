using System;
using System.Collections.Generic;
using UnityEngine;
using Runtime.Domain.ValueObjects;
using Runtime.Domain.Models;
using Cysharp.Threading.Tasks;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class RackGridManager : MonoBehaviour
    {
        public const int GRID_ROWS = 2;
        public const int GRID_COLUMNS = 15;
        public const int TOTAL_GRID_CELLS = GRID_ROWS * GRID_COLUMNS;

        [Header("Grid Configuration")]
        [SerializeField] private float _cellWidth = 60f;
        [SerializeField] private float _cellHeight = 80f;
        [SerializeField] private float _cellSpacing = 5f;
        [SerializeField] private float _rowSpacing = 10f;

        [Header("Grid Transforms")]
        [SerializeField] private Transform _topRowParent;
        [SerializeField] private Transform _bottomRowParent;
        [SerializeField] private Transform _gridContainer;

        private readonly GridCell[,] _gridCells = new GridCell[GRID_ROWS, GRID_COLUMNS];
        private readonly Dictionary<GridPosition, Vector3> _worldPositions = new Dictionary<GridPosition, Vector3>();
        private readonly List<GridPosition> _availablePositions = new List<GridPosition>();

        public event Action<GridPosition, OkeyPiece> OnPiecePlaced;
        public event Action<GridPosition, OkeyPiece> OnPieceRemoved;
        public event Action<GridPosition> OnCellHighlighted;
        public event Action<GridPosition> OnCellUnhighlighted;

        public int OccupiedCellCount { get; private set; }
        public int AvailableCellCount => TOTAL_GRID_CELLS - OccupiedCellCount;
        public bool IsFull => OccupiedCellCount >= TOTAL_GRID_CELLS;
        public bool IsEmpty => OccupiedCellCount == 0;

        private void Awake()
        {
            InitializeGrid();
            CalculateWorldPositions();
        }

        private void InitializeGrid()
        {
            _availablePositions.Clear();
            OccupiedCellCount = 0;

            for (int row = 0; row < GRID_ROWS; row++)
            {
                for (int column = 0; column < GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    _gridCells[row, column] = new GridCell(position);
                    _availablePositions.Add(position);
                }
            }
        }

        private void CalculateWorldPositions()
        {
            _worldPositions.Clear();

            for (int row = 0; row < GRID_ROWS; row++)
            {
                Transform parentTransform = row == 0 ? _topRowParent : _bottomRowParent;
                float yOffset = row == 0 ? _rowSpacing * 0.5f : -_rowSpacing * 0.5f;

                for (int column = 0; column < GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    
                    float xPosition = (column - (GRID_COLUMNS - 1) * 0.5f) * (_cellWidth + _cellSpacing);
                    Vector3 worldPosition = parentTransform.position + new Vector3(xPosition, yOffset, 0f);
                    
                    _worldPositions[position] = worldPosition;
                }
            }
        }

        public bool TryPlacePiece(OkeyPiece piece, GridPosition position)
        {
            if (!IsValidPosition(position))
            {
                return false;
            }

            GridCell cell = _gridCells[position.Row, position.Column];
            if (!cell.TryPlacePiece(piece))
            {
                return false;
            }

            _availablePositions.Remove(position);
            OccupiedCellCount++;
            OnPiecePlaced?.Invoke(position, piece);
            
            return true;
        }

        public bool TryRemovePiece(GridPosition position, out OkeyPiece removedPiece)
        {
            removedPiece = null;

            if (!IsValidPosition(position))
            {
                return false;
            }

            GridCell cell = _gridCells[position.Row, position.Column];
            if (cell.IsEmpty)
            {
                return false;
            }

            removedPiece = cell.RemovePiece();
            _availablePositions.Add(position);
            OccupiedCellCount--;
            OnPieceRemoved?.Invoke(position, removedPiece);
            
            return true;
        }

        public bool MovePiece(GridPosition fromPosition, GridPosition toPosition)
        {
            if (!TryRemovePiece(fromPosition, out OkeyPiece piece))
            {
                return false;
            }

            if (!TryPlacePiece(piece, toPosition))
            {
                TryPlacePiece(piece, fromPosition);
                return false;
            }

            return true;
        }

        public GridPosition GetNearestValidPosition(Vector3 worldPosition)
        {
            GridPosition nearestPosition = new GridPosition(0, 0);
            float nearestDistance = float.MaxValue;

            foreach (KeyValuePair<GridPosition, Vector3> kvp in _worldPositions)
            {
                float distance = Vector3.Distance(worldPosition, kvp.Value);
                if (distance < nearestDistance && IsPositionAvailable(kvp.Key))
                {
                    nearestDistance = distance;
                    nearestPosition = kvp.Key;
                }
            }

            return nearestPosition;
        }

        public Vector3 GetWorldPosition(GridPosition gridPosition)
        {
            return _worldPositions.TryGetValue(gridPosition, out Vector3 worldPos) ? worldPos : Vector3.zero;
        }

        public GridPosition GetGridPosition(Vector3 worldPosition)
        {
            GridPosition nearestPosition = new GridPosition(0, 0);
            float nearestDistance = float.MaxValue;

            foreach (KeyValuePair<GridPosition, Vector3> kvp in _worldPositions)
            {
                float distance = Vector3.Distance(worldPosition, kvp.Value);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPosition = kvp.Key;
                }
            }

            return nearestPosition;
        }

        public bool IsValidPosition(GridPosition position)
        {
            return position.Row >= 0 && position.Row < GRID_ROWS &&
                   position.Column >= 0 && position.Column < GRID_COLUMNS;
        }

        public bool IsPositionOccupied(GridPosition position)
        {
            if (!IsValidPosition(position))
            {
                return false;
            }

            return _gridCells[position.Row, position.Column].IsOccupied;
        }

        public bool IsPositionAvailable(GridPosition position)
        {
            return IsValidPosition(position) && !IsPositionOccupied(position);
        }

        public OkeyPiece GetPieceAt(GridPosition position)
        {
            if (!IsValidPosition(position))
            {
                return null;
            }

            return _gridCells[position.Row, position.Column].OccupyingPiece;
        }

        public void HighlightCell(GridPosition position, bool highlight)
        {
            if (!IsValidPosition(position))
            {
                return;
            }

            GridCell cell = _gridCells[position.Row, position.Column];
            cell.SetHighlighted(highlight);

            if (highlight)
            {
                OnCellHighlighted?.Invoke(position);
            }
            else
            {
                OnCellUnhighlighted?.Invoke(position);
            }
        }

        public void SetValidDropTarget(GridPosition position, bool isValid)
        {
            if (!IsValidPosition(position))
            {
                return;
            }

            _gridCells[position.Row, position.Column].SetValidDropTarget(isValid);
        }

        public void ClearAllHighlights()
        {
            for (int row = 0; row < GRID_ROWS; row++)
            {
                for (int column = 0; column < GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    HighlightCell(position, false);
                    SetValidDropTarget(position, false);
                }
            }
        }

        public List<GridPosition> GetAvailablePositions()
        {
            return new List<GridPosition>(_availablePositions);
        }

        public List<GridPosition> GetOccupiedPositions()
        {
            List<GridPosition> occupiedPositions = new List<GridPosition>();

            for (int row = 0; row < GRID_ROWS; row++)
            {
                for (int column = 0; column < GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    if (IsPositionOccupied(position))
                    {
                        occupiedPositions.Add(position);
                    }
                }
            }

            return occupiedPositions;
        }

        public void ClearGrid()
        {
            for (int row = 0; row < GRID_ROWS; row++)
            {
                for (int column = 0; column < GRID_COLUMNS; column++)
                {
                    GridPosition position = new GridPosition(row, column);
                    if (TryRemovePiece(position, out OkeyPiece removedPiece))
                    {
                        // Piece removed successfully
                    }
                }
            }

            ClearAllHighlights();
        }

        public async UniTask AnimateToPositionAsync(Transform tileTransform, GridPosition targetPosition, float duration = 0.3f)
        {
            if (!IsValidPosition(targetPosition))
            {
                return;
            }

            Vector3 targetWorldPosition = GetWorldPosition(targetPosition);
            Vector3 startPosition = tileTransform.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                // Ease-out curve for natural movement
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                
                tileTransform.position = Vector3.Lerp(startPosition, targetWorldPosition, easedProgress);
                await UniTask.Yield();
            }

            tileTransform.position = targetWorldPosition;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                CalculateWorldPositions();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_worldPositions.Count == 0)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            foreach (KeyValuePair<GridPosition, Vector3> kvp in _worldPositions)
            {
                Vector3 cellSize = new Vector3(_cellWidth, _cellHeight, 0.1f);
                Gizmos.DrawWireCube(kvp.Value, cellSize);
            }
        }
    }
}
