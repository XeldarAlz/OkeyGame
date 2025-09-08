using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public sealed class IntermediateAIPlayer : BaseAIPlayer
    {
        private float _lastHandStrength;
        private int _turnsWithoutImprovement;
        
        private const int MAX_TURNS_WITHOUT_IMPROVEMENT = 3;
        private const float HAND_STRENGTH_THRESHOLD = 5.0f;

        public override AIDifficulty Difficulty => AIDifficulty.Intermediate;

        [Inject]
        public IntermediateAIPlayer(
            int playerId, 
            string playerName, 
            IAIPlayerService aiPlayerService, 
            IAIDecisionService aiDecisionService) 
            : base(playerId, playerName, aiPlayerService, aiDecisionService)
        {
            _lastHandStrength = 0.0f;
            _turnsWithoutImprovement = 0;
        }

        public override async UniTask<PlayerAction> DecideActionAsync(GameState gameState)
        {
            if (gameState == null)
            {
                Debug.LogError("[IntermediateAIPlayer] GameState is null in DecideActionAsync");
                return default;
            }

            try
            {
                await UniTask.Delay(200); // Simulate moderate thinking time
                
                // Check if we should declare win first
                if (ShouldDeclareWin(gameState))
                {
                    Debug.Log($"[IntermediateAIPlayer] {Name} is declaring win!");
                    return PlayerAction.CreateWinDeclarationAction(Id);
                }

                // Use strategic decision making
                PlayerAction strategicAction = await _aiDecisionService.DecideActionAsync(this, gameState, Difficulty);
                
                if (strategicAction != null)
                {
                    return strategicAction;
                }

                // Fallback to random action if strategic decision fails
                return await GetFallbackActionAsync(gameState);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[IntermediateAIPlayer] Error in DecideActionAsync: {exception.Message}");
                return await GetFallbackActionAsync(gameState);
            }
        }

        public override void UpdateStrategy(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            float currentHandStrength = EvaluateHandStrength(gameState);
            
            // Track hand improvement
            if (currentHandStrength <= _lastHandStrength)
            {
                _turnsWithoutImprovement++;
            }
            else
            {
                _turnsWithoutImprovement = 0;
            }
            
            _lastHandStrength = currentHandStrength;
            
            base.UpdateStrategy(gameState);
        }

        protected override AIDifficulty GetAIDifficulty()
        {
            return AIDifficulty.Intermediate;
        }

        protected override void OnStrategyUpdated(GameState gameState)
        {
            float handStrength = EvaluateHandStrength(gameState);
            
            if (handStrength >= HAND_STRENGTH_THRESHOLD)
            {
                Debug.Log($"[IntermediateAIPlayer] {Name} has a strong hand (strength: {handStrength:F1})");
            }
            else if (_turnsWithoutImprovement >= MAX_TURNS_WITHOUT_IMPROVEMENT)
            {
                Debug.Log($"[IntermediateAIPlayer] {Name} needs to change strategy - no improvement for {_turnsWithoutImprovement} turns");
            }
        }

        protected override bool ShouldDrawFromDiscard(GameState gameState)
        {
            if (gameState == null)
            {
                return false;
            }

            // Intermediate AI considers hand strength when deciding to draw from discard
            float handStrength = EvaluateHandStrength(gameState);
            bool baseDecision = base.ShouldDrawFromDiscard(gameState);
            
            // More likely to draw from discard if hand is weak
            if (handStrength < HAND_STRENGTH_THRESHOLD / 2)
            {
                return baseDecision || (_turnsWithoutImprovement >= 2);
            }
            
            return baseDecision;
        }

        protected override TileData SelectBestDiscard(GameState gameState)
        {
            if (gameState == null)
            {
                return default;
            }

            TileData baseDiscard = base.SelectBestDiscard(gameState);
            
            // Intermediate AI considers not giving opponents useful tiles
            if (ShouldAvoidDiscardingUsefulTile(baseDiscard, gameState))
            {
                // Try to find a less useful tile to discard
                return FindAlternativeDiscard(gameState);
            }
            
            return baseDiscard;
        }

        private bool ShouldAvoidDiscardingUsefulTile(TileData tileData, GameState gameState)
        {
            if (tileData == null || gameState == null)
            {
                return false;
            }

            // Avoid discarding middle numbers (4-10) as they're more useful to opponents
            return tileData.Number >= 4 && tileData.Number <= 10;
        }

        private TileData FindAlternativeDiscard(GameState gameState)
        {
            if (Tiles == null || Tiles.Count == 0)
            {
                return default;
            }

            // Look for edge numbers (1-3, 11-13) to discard instead
            for (int tileIndex = 0; tileIndex < Tiles.Count; tileIndex++)
            {
                var tile = Tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                int number = tile.TileData.Number;
                if (number <= 3 || number >= 11)
                {
                    return tile.TileData;
                }
            }

            return default;
        }
    }
}
