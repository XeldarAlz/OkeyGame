using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public interface IAIPlayerService : IService
    {
        UniTask<PlayerAction> GetBestActionAsync(Player aiPlayer, GameState gameState);
        UniTask<PlayerAction> GetRandomValidActionAsync(Player aiPlayer, GameState gameState);
        UniTask<PlayerAction> GetStrategicActionAsync(Player aiPlayer, GameState gameState, AIDifficulty difficulty);
        
        float EvaluateHandStrength(Player player, TileData jokerTile);
        bool ShouldDrawFromDiscard(Player player, GameState gameState);
        TileData SelectBestDiscard(Player player, GameState gameState);
        bool ShouldDeclareWin(Player player, GameState gameState);
    }
}
