using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;
using Runtime.Infrastructure.Persistence;
using Zenject;

namespace Runtime.Services.GameLogic
{
    public sealed class GameStatePersistenceService : IGameStatePersistenceService, IInitializableService, IDisposableService
    {
        // Events (kept for interface compatibility)
        public event Action<GameStateSaveData> OnGameStateSaved;
        public event Action<GameStateSaveData> OnGameStateLoaded;
        public event Action<RoundHistoryData> OnRoundHistorySaved;

        [Inject]
        public GameStatePersistenceService()
        {
            // No dependencies needed since we're not actually persisting anything
        }

        public async UniTask InitializeAsync()
        {
            await UniTask.Yield();
        }

        // Stub implementation - no actual persistence
        public async UniTask<bool> SaveCurrentGameStateAsync(GameStateSaveData gameStateData, int saveSlot = 0)
        {
            // No-op since we don't want to save
            OnGameStateSaved?.Invoke(gameStateData);
            await UniTask.Yield();
            return true;
        }

        // Stub implementation - no actual persistence
        public async UniTask<GameStateSaveData> LoadGameStateAsync(int saveSlot = 0)
        {
            // Return null since we don't have saved data
            await UniTask.Yield();
            return null;
        }

        // Stub implementation - no actual persistence
        public async UniTask<bool> SaveRoundHistoryAsync(RoundHistoryData roundHistory, int saveSlot = 0)
        {
            // No-op since we don't want to save round history
            OnRoundHistorySaved?.Invoke(roundHistory);
            await UniTask.Yield();
            return true;
        }

        // Stub implementation - no actual persistence
        public async UniTask<RoundHistoryData> LoadRoundHistoryAsync(int saveSlot = 0)
        {
            // Return null since we don't have saved data
            await UniTask.Yield();
            return null;
        }

        // Stub implementation - no actual persistence
        public async UniTask<bool> SavePlayerStatisticsAsync(PlayerStatisticsData statistics, int saveSlot = 0)
        {
            // No-op since we don't want to save
            await UniTask.Yield();
            return true;
        }

        // Stub implementation - no actual persistence
        public async UniTask<PlayerStatisticsData> LoadPlayerStatisticsAsync(int saveSlot = 0)
        {
            // Return null since we don't have saved data
            await UniTask.Yield();
            return null;
        }

        // Stub implementation - no actual persistence
        public async UniTask<bool> HasSavedGameAsync(int saveSlot = 0)
        {
            // Always return false since we don't save games
            await UniTask.Yield();
            return false;
        }

        // Stub implementation - no actual persistence
        public async UniTask<bool> DeleteSavedGameAsync(int saveSlot = 0)
        {
            // No-op since we don't save games
            await UniTask.Yield();
            return true;
        }

        // Stub implementation - no actual persistence
        public async UniTask<List<int>> GetAvailableSaveSlotsAsync()
        {
            // Return empty list since we don't save games
            await UniTask.Yield();
            return new List<int>();
        }

        // Factory methods still needed for in-memory data structures
        public GameStateSaveData CreateGameStateSaveData(
            GameConfiguration configuration,
            List<Player> players,
            GameStateType currentState,
            OkeyPiece indicatorTile,
            List<OkeyPiece> discardPile,
            int remainingTilesCount)
        {
            return new GameStateSaveData
            {
                SaveTimestamp = DateTime.UtcNow,
                GameConfiguration = configuration,
                Players = new List<Player>(players),
                CurrentGameState = currentState,
                IndicatorTile = indicatorTile,
                DiscardPile = new List<OkeyPiece>(discardPile ?? new List<OkeyPiece>()),
                RemainingTilesCount = remainingTilesCount,
                CurrentPlayerIndex = GetCurrentPlayerIndex(players),
                RoundNumber = 1
            };
        }

        public RoundHistoryData CreateRoundHistoryData(
            int roundNumber,
            Player winner,
            WinType winType,
            Dictionary<Player, int> roundScores,
            Dictionary<Player, ScoreBreakdown> scoreBreakdowns)
        {
            return new RoundHistoryData
            {
                RoundNumber = roundNumber,
                RoundTimestamp = DateTime.UtcNow,
                Winner = winner,
                WinType = winType,
                RoundScores = new Dictionary<Player, int>(roundScores ?? new Dictionary<Player, int>()),
                ScoreBreakdowns = new Dictionary<Player, ScoreBreakdown>(scoreBreakdowns ?? new Dictionary<Player, ScoreBreakdown>()),
                RoundDurationSeconds = 0
            };
        }

        public PlayerStatisticsData CreatePlayerStatisticsData(List<Player> players)
        {
            PlayerStatisticsData statistics = new PlayerStatisticsData
            {
                TotalGamesPlayed = 1,
                PlayerStats = new Dictionary<int, PlayerStats>()
            };

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                Player player = players[playerIndex];
                statistics.PlayerStats[player.Id] = new PlayerStats
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    GamesWon = 0,
                    GamesLost = 0,
                    TotalScore = player.Score,
                    AverageScore = player.Score,
                    BestWinType = WinType.Normal,
                    FastestWinTime = TimeSpan.Zero
                };
            }

            return statistics;
        }

        private int GetCurrentPlayerIndex(List<Player> players)
        {
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                Player player = players[playerIndex];
                if (player != null)
                {
                    return playerIndex;
                }
            }
            return 0;
        }

        public void Dispose()
        {
            // Nothing to dispose since we're not using any resources
        }
    }

    // Keep the data classes for in-memory usage
    [Serializable]
    public sealed class GameStateSaveData
    {
        public DateTime SaveTimestamp { get; set; }
        public GameConfiguration GameConfiguration { get; set; }
        public List<Player> Players { get; set; }
        public GameStateType CurrentGameState { get; set; }
        public OkeyPiece IndicatorTile { get; set; }
        public List<OkeyPiece> DiscardPile { get; set; }
        public int RemainingTilesCount { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int RoundNumber { get; set; }

        public GameStateSaveData()
        {
            SaveTimestamp = DateTime.UtcNow;
            Players = new List<Player>();
            DiscardPile = new List<OkeyPiece>();
            CurrentGameState = GameStateType.None;
            RemainingTilesCount = 0;
            CurrentPlayerIndex = 0;
            RoundNumber = 1;
        }
    }

    [Serializable]
    public sealed class RoundHistoryData
    {
        public int RoundNumber { get; set; }
        public DateTime RoundTimestamp { get; set; }
        public Player Winner { get; set; }
        public WinType WinType { get; set; }
        public Dictionary<Player, int> RoundScores { get; set; }
        public Dictionary<Player, ScoreBreakdown> ScoreBreakdowns { get; set; }
        public float RoundDurationSeconds { get; set; }

        public RoundHistoryData()
        {
            RoundNumber = 0;
            RoundTimestamp = DateTime.UtcNow;
            RoundScores = new Dictionary<Player, int>();
            ScoreBreakdowns = new Dictionary<Player, ScoreBreakdown>();
            RoundDurationSeconds = 0f;
        }
    }

    [Serializable]
    public sealed class PlayerStatisticsData
    {
        public int TotalGamesPlayed { get; set; }
        public Dictionary<int, PlayerStats> PlayerStats { get; set; }

        public PlayerStatisticsData()
        {
            TotalGamesPlayed = 0;
            PlayerStats = new Dictionary<int, PlayerStats>();
        }
    }

    [Serializable]
    public sealed class PlayerStats
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int TotalScore { get; set; }
        public float AverageScore { get; set; }
        public WinType BestWinType { get; set; }
        public TimeSpan FastestWinTime { get; set; }

        public PlayerStats()
        {
            PlayerId = 0;
            PlayerName = string.Empty;
            GamesWon = 0;
            GamesLost = 0;
            TotalScore = 0;
            AverageScore = 0f;
            BestWinType = WinType.Normal;
            FastestWinTime = TimeSpan.Zero;
        }
    }
}
