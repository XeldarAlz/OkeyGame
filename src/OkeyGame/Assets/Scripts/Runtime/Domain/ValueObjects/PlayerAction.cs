using System;
using Runtime.Domain.Enums;

namespace Runtime.Domain.ValueObjects
{
    [Serializable]
    public readonly struct PlayerAction : IEquatable<PlayerAction>
    {
        public readonly TurnAction ActionType;
        public readonly TileData TileData;
        public readonly GridPosition FromPosition;
        public readonly GridPosition ToPosition;
        public readonly int PlayerId;

        public PlayerAction(TurnAction actionType, int playerId, TileData tileData = default,
            GridPosition fromPosition = default, GridPosition toPosition = default)
        {
            ActionType = actionType;
            PlayerId = playerId;
            TileData = tileData;
            FromPosition = fromPosition;
            ToPosition = toPosition;
        }

        public static PlayerAction CreateDrawAction(int playerId)
        {
            return new PlayerAction(TurnAction.Draw, playerId);
        }

        public static PlayerAction CreateDiscardAction(int playerId, TileData tileData, GridPosition fromPosition)
        {
            return new PlayerAction(TurnAction.Discard, playerId, tileData, fromPosition);
        }

        public static PlayerAction CreateMoveAction(int playerId, TileData tileData, GridPosition fromPosition,
            GridPosition toPosition)
        {
            return new PlayerAction(TurnAction.Draw, playerId, tileData, fromPosition, toPosition);
        }

        public static PlayerAction CreateWinDeclarationAction(int playerId)
        {
            return new PlayerAction(TurnAction.DeclareWin, playerId);
        }

        public static PlayerAction CreateShowIndicatorAction(int playerId, TileData indicatorTile)
        {
            return new PlayerAction(TurnAction.ShowIndicator, playerId, indicatorTile);
        }

        public static PlayerAction CreateDeclareWinAction(int playerId, WinType winType)
        {
            return new PlayerAction(TurnAction.DeclareWin, playerId);
        }

        public bool Equals(PlayerAction other)
        {
            return ActionType == other.ActionType && PlayerId == other.PlayerId &&
                   Nullable.Equals(TileData, other.TileData) && Nullable.Equals(FromPosition, other.FromPosition) &&
                   Nullable.Equals(ToPosition, other.ToPosition);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerAction other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)ActionType, PlayerId, TileData, FromPosition, ToPosition);
        }

        public static bool operator ==(PlayerAction left, PlayerAction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerAction left, PlayerAction right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Player{PlayerId}: {ActionType}";
        }
    }
}