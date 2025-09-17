using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;

namespace Runtime.Infrastructure.Persistence
{
    public interface IPersistenceService : IInitializableService
    {
        // Game State Persistence
        UniTask<bool> SaveGameStateAsync(GameState gameState, string saveSlot);
        UniTask<GameState> LoadGameStateAsync(string saveSlot);
        UniTask<bool> DeleteSaveAsync(string saveSlot);
        UniTask<List<string>> GetAvailableSavesAsync();
        bool HasSave(string saveSlot);

        // Player Data Persistence
        UniTask<bool> SavePlayerDataAsync(Player player);
        UniTask<Player> LoadPlayerDataAsync(int playerId);

        // Game Configuration Persistence
        UniTask<bool> SaveGameConfigurationAsync(GameConfiguration configuration);
        UniTask<GameConfiguration> LoadGameConfigurationAsync();

        // Score and Statistics Persistence
        UniTask<bool> SavePlayerScoresAsync(Dictionary<int, int> scores);
        UniTask<Dictionary<int, int>> LoadPlayerScoresAsync();

        // Settings Persistence
        UniTask<bool> SaveSettingsAsync(Dictionary<string, object> settings);
        UniTask<Dictionary<string, object>> LoadSettingsAsync();

        // Utility Methods
        UniTask<bool> ClearAllDataAsync();
        UniTask<long> GetStorageSizeAsync();
        bool IsStorageAvailable();
    }
}