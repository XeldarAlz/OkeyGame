using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;

namespace Runtime.Services.GameLogic
{
    public interface IScoreCalculationService
    {
        event Action<Player, int, ScoreBreakdown> OnScoreCalculated;
        event Action<List<Player>, Dictionary<Player, int>> OnRoundScoresCalculated;

        UniTask<ScoreBreakdown> CalculateWinnerScoreAsync(Player winner, WinType winType, List<Player> allPlayers);
        UniTask<Dictionary<Player, int>> CalculateRoundScoresAsync(List<Player> players, Player winner, WinType winType);
        UniTask<int> CalculatePlayerPenaltyAsync(Player player, Player winner, WinType winType);
        UniTask<int> CalculateIndicatorBonusAsync(Player player);
        int CalculateRemainingTilesPenalty(List<OkeyPiece> remainingTiles);
        UniTask<int> CalculateFalseJokerPenaltyAsync(List<OkeyPiece> tiles);
        UniTask<bool> ShouldPlayerPayDoubleAsync(Player player, WinType winType);
        int GetWinTypeMultiplier(WinType winType);
        UniTask<ScoreBreakdown> GetDetailedScoreBreakdownAsync(Player player, WinType winType, List<Player> allPlayers);
    }
}
