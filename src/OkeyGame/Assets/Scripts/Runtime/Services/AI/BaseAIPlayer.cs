using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public abstract class BaseAIPlayer : Player, IAIPlayer
    {
        protected readonly IAIPlayerService _aiPlayerService;
        
        protected readonly IAIDecisionService _aiDecisionService;
        
        public abstract AIDifficulty Difficulty { get; }
        public new bool IsAI => true;

        protected BaseAIPlayer(
            int playerId, 
            string playerName, 
            IAIPlayerService aiPlayerService, 
            IAIDecisionService aiDecisionService) 
            : base(playerId, playerName, PlayerType.AI)
        {
            _aiPlayerService = aiPlayerService;
            _aiDecisionService = aiDecisionService;
        }

        public virtual async UniTask<PlayerAction> DecideActionAsync(GameState gameState)
        {
            if (gameState == null)
            {
                Debug.LogError($"[{GetType().Name}] GameState is null in DecideActionAsync");
                return default;
            }

            try
            {
                return await _aiPlayerService.GetStrategicActionAsync(this, gameState, Difficulty);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[{GetType().Name}] Error in DecideActionAsync: {exception.Message}");
                return await _aiPlayerService.GetRandomValidActionAsync(this, gameState);
            }
        }

        public virtual void UpdateStrategy(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            // Base implementation - can be overridden by derived classes
            OnStrategyUpdated(gameState);
        }

        protected virtual void OnStrategyUpdated(GameState gameState)
        {
            // Hook for derived classes to implement custom strategy updates
        }

        protected abstract AIDifficulty GetAIDifficulty();

        protected virtual async UniTask<PlayerAction> GetFallbackActionAsync(GameState gameState)
        {
            return await _aiPlayerService.GetRandomValidActionAsync(this, gameState);
        }

        protected virtual bool ShouldDrawFromDiscard(GameState gameState)
        {
            return _aiPlayerService.ShouldDrawFromDiscard(this, gameState);
        }

        protected virtual TileData SelectBestDiscard(GameState gameState)
        {
            return _aiPlayerService.SelectBestDiscard(this, gameState);
        }

        protected virtual bool ShouldDeclareWin(GameState gameState)
        {
            return _aiPlayerService.ShouldDeclareWin(this, gameState);
        }

        protected virtual float EvaluateHandStrength(GameState gameState)
        {
            if (gameState == null)
            {
                return 0.0f;
            }

            return _aiPlayerService.EvaluateHandStrength(this, gameState.JokerTile);
        }
    }
}
