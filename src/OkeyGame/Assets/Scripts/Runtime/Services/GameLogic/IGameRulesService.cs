using System.Collections.Generic;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.GameLogic
{
    public interface IGameRulesService : IService
    {
        bool ValidatePlayerMove(PlayerAction action, GameState gameState);
        bool ValidateWinCondition(Player player, WinType winType);
        bool ValidateSet(List<TileData> tiles, TileData jokerTile);
        bool ValidateSequence(List<TileData> tiles, TileData jokerTile);
        bool ValidatePairsWin(List<TileData> tiles);

        List<PlayerAction> GetValidActions(Player player, GameState gameState);
        WinType? CheckWinCondition(Player player, TileData jokerTile);
        int CalculateWinScore(WinType winType);
        int CalculatePlayerPenalty(Player player, WinType winnerWinType);

        bool CanDrawFromPile(GameState gameState);
        bool CanDrawFromDiscard(GameState gameState);
        bool CanDiscard(Player player, TileData tileData);
        bool CanShowIndicator(Player player, TileData indicatorTile);
    }
}