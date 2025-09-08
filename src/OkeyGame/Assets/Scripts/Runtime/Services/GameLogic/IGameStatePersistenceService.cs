using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;

namespace Runtime.Services.GameLogic
{
    public interface IGameStatePersistenceService
    {
        event Action<GameStateSaveData> OnGameStateSaved;
        event Action<GameStateSaveData> OnGameStateLoaded;
        event Action<RoundHistoryData> OnRoundHistorySaved;

        UniTask<bool> SaveCurrentGameStateAsync(GameStateSaveData gameStateData, int saveSlot = 0);
        UniTask<GameStateSaveData> LoadGameStateAsync(int saveSlot = 0);
        UniTask<bool> SaveRoundHistoryAsync(RoundHistoryData roundHistory, int saveSlot = 0);
        UniTask<RoundHistoryData> LoadRoundHistoryAsync(int saveSlot = 0);
        UniTask<bool> SavePlayerStatisticsAsync(PlayerStatisticsData statistics, int saveSlot = 0);
        UniTask<PlayerStatisticsData> LoadPlayerStatisticsAsync(int saveSlot = 0);
        UniTask<bool> HasSavedGameAsync(int saveSlot = 0);
        UniTask<bool> DeleteSavedGameAsync(int saveSlot = 0);
        UniTask<List<int>> GetAvailableSaveSlotsAsync();

        GameStateSaveData CreateGameStateSaveData(
            GameConfiguration configuration,
            List<Player> players,
            GameStateType currentState,
            OkeyPiece indicatorTile,
            List<OkeyPiece> discardPile,
            int remainingTilesCount);

        RoundHistoryData CreateRoundHistoryData(
            int roundNumber,
            Player winner,
            WinType winType,
            Dictionary<Player, int> roundScores,
            Dictionary<Player, ScoreBreakdown> scoreBreakdowns);

        PlayerStatisticsData CreatePlayerStatisticsData(List<Player> players);
    }
}
