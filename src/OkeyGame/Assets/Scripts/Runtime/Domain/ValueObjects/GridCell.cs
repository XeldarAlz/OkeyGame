using System;
using Runtime.Domain.Models;

namespace Runtime.Domain.ValueObjects
{
    [Serializable]
    public sealed class GridCell
    {
        private readonly GridPosition _position;
        
        private OkeyPiece _occupyingPiece;
        
        private bool _isHighlighted;
        private bool _isValidDropTarget;

        public GridPosition Position => _position;
        public OkeyPiece OccupyingPiece => _occupyingPiece;
        
        public bool IsOccupied => _occupyingPiece != null;
        public bool IsEmpty => _occupyingPiece == null;
        public bool IsHighlighted => _isHighlighted;
        public bool IsValidDropTarget => _isValidDropTarget;

        public GridCell(GridPosition position)
        {
            _position = position;
            _occupyingPiece = null;
            _isHighlighted = false;
            _isValidDropTarget = false;
        }

        public bool TryPlacePiece(OkeyPiece piece)
        {
            if (IsOccupied)
            {
                return false;
            }

            _occupyingPiece = piece;
            return true;
        }

        public OkeyPiece RemovePiece()
        {
            OkeyPiece removedPiece = _occupyingPiece;
            _occupyingPiece = null;
            return removedPiece;
        }

        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
        }

        public void SetValidDropTarget(bool isValid)
        {
            _isValidDropTarget = isValid;
        }

        public void Clear()
        {
            _occupyingPiece = null;
            _isHighlighted = false;
            _isValidDropTarget = false;
        }

        public override string ToString()
        {
            string occupiedStatus = IsOccupied ? $"Occupied by {_occupyingPiece}" : "Empty";
            return $"GridCell at {_position} - {occupiedStatus}";
        }
    }
}