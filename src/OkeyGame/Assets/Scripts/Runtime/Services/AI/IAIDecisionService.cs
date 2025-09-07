using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public interface IAIDecisionService : IService
    {
        UniTask<PlayerAction> DecideActionAsync(Player aiPlayer, GameState gameState, AIDifficulty difficulty);
        float CalculateActionValue(PlayerAction action, GameState gameState);
        PlayerAction GetBestAction(Player aiPlayer, GameState gameState, AIDifficulty difficulty);
    }
}