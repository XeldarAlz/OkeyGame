using System;
using Runtime.Domain.Enums;

namespace Runtime.Domain.ValueObjects
{
    [Serializable]
    public readonly struct TileData : IEquatable<TileData>
    {
        public readonly int Number;
        public readonly OkeyColor Color;
        public readonly OkeyPieceType PieceType;
        public readonly bool IsJoker;

        public TileData(int number, OkeyColor color, OkeyPieceType pieceType = OkeyPieceType.Normal)
        {
            Number = number;
            Color = color;
            PieceType = pieceType;
            IsJoker = pieceType == OkeyPieceType.Joker || pieceType == OkeyPieceType.FalseJoker;
        }

        public bool CanFormSequenceWith(TileData other)
        {
            if (IsJoker || other.IsJoker)
            {
                return true;
            }

            return Color == other.Color && Math.Abs(Number - other.Number) == 1;
        }

        public bool CanFormSetWith(TileData other)
        {
            if (IsJoker || other.IsJoker)
            {
                return true;
            }

            return Number == other.Number && Color != other.Color;
        }

        public bool Equals(TileData other)
        {
            return Number == other.Number && Color == other.Color && PieceType == other.PieceType;
        }

        public override bool Equals(object obj)
        {
            return obj is TileData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, (int)Color, (int)PieceType);
        }

        public static bool operator ==(TileData left, TileData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TileData left, TileData right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return IsJoker 
                ? $"Joker({PieceType})" 
                : $"{Number}{Color}";
        }
    }
}