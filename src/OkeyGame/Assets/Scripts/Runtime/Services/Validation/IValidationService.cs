using System.Collections.Generic;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.Validation
{
    public interface IValidationService : IService
    {
        ValidationResult ValidateMove(PlayerAction action, GameState gameState);
        ValidationResult ValidateTileSet(List<TileData> tiles, TileData jokerTile);
        ValidationResult ValidateTileSequence(List<TileData> tiles, TileData jokerTile);
        ValidationResult ValidatePlayerHand(Player player, TileData jokerTile);
        ValidationResult ValidateGridPosition(GridPosition position);
        ValidationResult ValidateTilePlacement(TileData tileData, GridPosition position, GameState gameState);
        
        bool IsValidSet(List<TileData> tiles, TileData jokerTile);
        bool IsValidSequence(List<TileData> tiles, TileData jokerTile);
        bool IsValidPairsHand(List<TileData> tiles);
        bool CanPlayerWin(Player player, TileData jokerTile);
        bool HasValidWinningHand(Player player, TileData jokerTile);
    }
}
