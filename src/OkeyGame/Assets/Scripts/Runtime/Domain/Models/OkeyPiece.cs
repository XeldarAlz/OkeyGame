using System;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using UnityEngine;

namespace Runtime.Domain.Models
{
    [Serializable]
    public sealed class OkeyPiece
    {
        [SerializeField] private int _number;
        [SerializeField] private OkeyColor _color;
        [SerializeField] private OkeyPieceType _pieceType;
        [SerializeField] private bool _isJoker;
        [SerializeField] private GridPosition _gridPosition;

        private readonly int _uniqueId;
        public int Number => _number;
        public OkeyColor Color => _color;
        public OkeyPieceType PieceType => _pieceType;
        public bool IsJoker => _isJoker;
        public int UniqueId => _uniqueId;
        public GridPosition GridPosition => _gridPosition;
        public TileData TileData => new TileData(_number, _color, _pieceType);

        public OkeyPiece(int number, OkeyColor color, OkeyPieceType pieceType, int uniqueId)
        {
            _number = number;
            _color = color;
            _pieceType = pieceType;
            _uniqueId = uniqueId;
            _isJoker = pieceType == OkeyPieceType.Joker || pieceType == OkeyPieceType.FalseJoker;
            _gridPosition = default;
        }

        public bool CanFormSequenceWith(OkeyPiece other)
        {
            if (other == null)
            {
                return false;
            }

            return TileData.CanFormSequenceWith(other.TileData);
        }

        public bool CanFormSetWith(OkeyPiece other)
        {
            if (other == null)
            {
                return false;
            }

            return TileData.CanFormSetWith(other.TileData);
        }

        public override string ToString()
        {
            return TileData.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is OkeyPiece other)
            {
                return _uniqueId == other._uniqueId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _uniqueId.GetHashCode();
        }

        public void SetGridPosition(GridPosition position)
        {
            _gridPosition = position;
        }
    }
}