using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;

namespace Runtime.Services.GameLogic.Score
{
    public interface IScoreService : IInitializableService
    {
        event Action<Player, int> OnScoreChanged;
        event Action<Player> OnPlayerEliminated;
        event Action<Player> OnGameWinner;
        
        UniTask<int> CalculateWinScoreAsync(WinType winType);
        UniTask<int> CalculatePlayerPenaltyAsync(Player player, WinType winnerWinType);
        UniTask ApplyWinScoreAsync(Player winner, WinType winType, List<Player> allPlayers);
        UniTask<bool> CheckGameEndConditionAsync(List<Player> players);
        
        int GetPlayerScore(int playerId);
        void SetPlayerScore(int playerId, int score);
        void AddPlayerScore(int playerId, int points);
        void SubtractPlayerScore(int playerId, int points);
        Player GetWinner(List<Player> players);
        bool IsPlayerEliminated(Player player);
    }
}
