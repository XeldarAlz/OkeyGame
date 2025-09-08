using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public sealed class BeginnerAIPlayer : BaseAIPlayer
    {
        public override AIDifficulty Difficulty => AIDifficulty.Beginner;

        [Inject]
        public BeginnerAIPlayer(int playerId, string playerName, IAIPlayerService aiPlayerService,
            IAIDecisionService aiDecisionService) : base(playerId, playerName, aiPlayerService, aiDecisionService)
        {
        }

        public override async UniTask<PlayerAction> DecideActionAsync(GameState gameState)
        {
            if (gameState == null)
            {
                Debug.LogError("[BeginnerAIPlayer] GameState is null in DecideActionAsync");
                return default;
            }

            try
            {
                // Beginner AI uses simple random selection from valid moves
                await UniTask.Delay(100); // Simulate quick thinking time

                return await _aiPlayerService.GetRandomValidActionAsync(this, gameState);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[BeginnerAIPlayer] Error in DecideActionAsync: {exception.Message}");
                return await GetFallbackActionAsync(gameState);
            }
        }

        public override void UpdateStrategy(GameState gameState)
        {
            // Beginner AI doesn't update strategy - always uses random moves
            base.UpdateStrategy(gameState);
        }

        protected override AIDifficulty GetAIDifficulty()
        {
            return AIDifficulty.Beginner;
        }

        protected override void OnStrategyUpdated(GameState gameState)
        {
            // Beginner AI has no complex strategy to update
            Debug.Log($"[BeginnerAIPlayer] {Name} is thinking randomly...");
        }
    }
}