using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public interface IAIPlayer
    {
        int Id { get; }
        string Name { get; }
        AIDifficulty Difficulty { get; }
        bool IsAI { get; }
        
        UniTask<PlayerAction> DecideActionAsync(GameState gameState);
        void UpdateStrategy(GameState gameState);
    }
}