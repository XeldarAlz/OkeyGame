using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;

namespace Runtime.Infrastructure.Persistence
{
    public interface IPersistenceService : IInitializableService
    {
        UniTask<bool> SaveGameStateAsync(GameState gameState, string saveSlot = "default");
        UniTask<GameState> LoadGameStateAsync(string saveSlot = "default");
        UniTask<bool> SavePlayerDataAsync(Player player);
        UniTask<Player> LoadPlayerDataAsync(int playerId);
        UniTask<bool> SaveGameConfigurationAsync(GameConfiguration config);
        UniTask<GameConfiguration> LoadGameConfigurationAsync();
        UniTask<bool> DeleteSaveAsync(string saveSlot);
        UniTask<string[]> GetAvailableSavesAsync();
        bool HasSave(string saveSlot);
    }
}
