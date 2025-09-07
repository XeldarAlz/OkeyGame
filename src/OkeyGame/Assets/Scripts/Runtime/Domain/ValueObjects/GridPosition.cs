using System;
using Runtime.Domain.Enums;

namespace Runtime.Domain.ValueObjects
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int Row;
        public readonly int Column;

        public GridPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public GridPosition(RackRow rackRow, int column)
        {
            Row = rackRow == RackRow.Top ? 0 : 1;
            Column = column;
        }

        public RackRow GetRackRow()
        {
            return Row == 0 ? RackRow.Top : RackRow.Bottom;
        }

        public bool IsValid()
        {
            return Row is >= 0 and <= 1 && Column is >= 0 and < 15;
        }

        public bool Equals(GridPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({Row}, {Column})";
        }
    }
}