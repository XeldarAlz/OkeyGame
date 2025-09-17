using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public sealed class AdvancedAIPlayer : BaseAIPlayer
    {
        private readonly Dictionary<int, float> _opponentHandStrengths;
        private readonly List<TileData> _observedDiscards;
        private float _lastHandStrength;
        private int _turnsPlayed;
        private WinType _preferredWinType;
        private const float AGGRESSIVE_THRESHOLD = 7.0f;
        private const float DEFENSIVE_THRESHOLD = 3.0f;
        private const int MEMORY_TURNS = 10;
        public override AIDifficulty Difficulty => AIDifficulty.Advanced;

        [Inject]
        public AdvancedAIPlayer(int playerId, string playerName, IAIPlayerService aiPlayerService,
            IAIDecisionService aiDecisionService) : base(playerId, playerName, aiPlayerService, aiDecisionService)
        {
            _opponentHandStrengths = new Dictionary<int, float>();
            _observedDiscards = new List<TileData>();
            _lastHandStrength = 0.0f;
            _turnsPlayed = 0;
            _preferredWinType = WinType.Normal;
        }

        public override async UniTask<PlayerAction> DecideActionAsync(GameState gameState)
        {
            if (gameState == null)
            {
                Debug.LogError("[AdvancedAIPlayer] GameState is null in DecideActionAsync");
                return default;
            }

            try
            {
                await UniTask.Delay(300); // Simulate complex thinking time
                _turnsPlayed++;

                // Advanced analysis before making decision
                AnalyzeGameState(gameState);
                UpdateOpponentAnalysis(gameState);
                DetermineOptimalWinStrategy(gameState);

                // Check if we should declare win first
                if (ShouldDeclareWin(gameState))
                {
                    Debug.Log($"[AdvancedAIPlayer] {Name} is declaring win with {_preferredWinType}!");
                    return PlayerAction.CreateWinDeclarationAction(Id);
                }

                // Use advanced strategic decision making
                PlayerAction strategicAction = await MakeAdvancedDecision(gameState);
                return strategicAction;

                // Fallback to base strategic action
                return await _aiDecisionService.DecideActionAsync(this, gameState, Difficulty);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AdvancedAIPlayer] Error in DecideActionAsync: {exception.Message}");
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
            _lastHandStrength = currentHandStrength;

            // Advanced strategy updates
            AdaptToOpponents(gameState);
            OptimizeWinStrategy(gameState);
            base.UpdateStrategy(gameState);
        }

        protected override AIDifficulty GetAIDifficulty()
        {
            return AIDifficulty.Advanced;
        }

        protected override void OnStrategyUpdated(GameState gameState)
        {
            float handStrength = EvaluateHandStrength(gameState);
            string strategy = DetermineCurrentStrategy(handStrength);
            Debug.Log(
                $"[AdvancedAIPlayer] {Name} using {strategy} strategy (strength: {handStrength:F1}, preferred win: {_preferredWinType})");
        }

        private async UniTask<PlayerAction> MakeAdvancedDecision(GameState gameState)
        {
            float handStrength = EvaluateHandStrength(gameState);

            // Aggressive play when hand is strong
            if (handStrength >= AGGRESSIVE_THRESHOLD)
            {
                return await MakeAggressiveDecision(gameState);
            }

            // Defensive play when hand is weak
            if (handStrength <= DEFENSIVE_THRESHOLD)
            {
                return await MakeDefensiveDecision(gameState);
            }

            // Balanced play for moderate hands
            return await MakeBalancedDecision(gameState);
        }

        private async UniTask<PlayerAction> MakeAggressiveDecision(GameState gameState)
        {
            // Aggressive: Focus on completing hand quickly
            if (ShouldDrawFromDiscard(gameState))
            {
                return PlayerAction.CreateDrawAction(Id);
            }

            // Draw from pile to get new tiles
            return PlayerAction.CreateDrawAction(Id);
        }

        private async UniTask<PlayerAction> MakeDefensiveDecision(GameState gameState)
        {
            // Defensive: Avoid giving opponents useful tiles
            TileData safeDiscard = FindSafestDiscard(gameState);
            if (safeDiscard != null)
            {
                return PlayerAction.CreateDiscardAction(Id, safeDiscard, new GridPosition(0, 0));
            }

            // Default to drawing from pile
            return PlayerAction.CreateDrawAction(Id);
        }

        private async UniTask<PlayerAction> MakeBalancedDecision(GameState gameState)
        {
            // Balanced: Use standard strategic decision making
            return await _aiDecisionService.DecideActionAsync(this, gameState, Difficulty);
        }

        private void AnalyzeGameState(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            // Track discarded tiles for memory
            if (gameState.DiscardPile != null && gameState.DiscardPile.Count > 0)
            {
                OkeyPiece lastDiscard = gameState.DiscardPile[gameState.DiscardPile.Count - 1];
                if (lastDiscard != null && !_observedDiscards.Contains(lastDiscard.TileData))
                {
                    _observedDiscards.Add(lastDiscard.TileData);

                    // Keep memory limited
                    if (_observedDiscards.Count > MEMORY_TURNS)
                    {
                        _observedDiscards.RemoveAt(0);
                    }
                }
            }
        }

        private void UpdateOpponentAnalysis(GameState gameState)
        {
            if (gameState == null || gameState.Players == null)
            {
                return;
            }

            // Estimate opponent hand strengths based on their actions
            for (int playerIndex = 0; playerIndex < gameState.Players.Count; playerIndex++)
            {
                Player opponent = gameState.Players[playerIndex];
                if (opponent == null || opponent.Id == Id)
                {
                    continue;
                }

                float estimatedStrength = EstimateOpponentHandStrength(opponent, gameState);
                _opponentHandStrengths[opponent.Id] = estimatedStrength;
            }
        }

        private void DetermineOptimalWinStrategy(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            float handStrength = EvaluateHandStrength(gameState);
            int pairCount = CountPairs();

            // Prefer pairs win if we have many pairs
            if (pairCount >= 5)
            {
                _preferredWinType = WinType.Pairs;
            }
            // Prefer normal win for balanced hands
            else if (handStrength >= AGGRESSIVE_THRESHOLD)
            {
                _preferredWinType = WinType.Normal;
            }
            // Keep current strategy if uncertain
        }

        private void AdaptToOpponents(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            // Analyze if opponents are close to winning
            bool opponentThreat = false;
            foreach (var opponentStrength in _opponentHandStrengths.Values)
            {
                if (opponentStrength >= AGGRESSIVE_THRESHOLD)
                {
                    opponentThreat = true;
                    break;
                }
            }

            // Become more aggressive if opponents are threatening
            if (opponentThreat && _lastHandStrength >= DEFENSIVE_THRESHOLD)
            {
                Debug.Log($"[AdvancedAIPlayer] {Name} detected opponent threat - becoming more aggressive");
            }
        }

        private void OptimizeWinStrategy(GameState gameState)
        {
            if (gameState == null)
            {
                return;
            }

            // Continuously evaluate if current win strategy is optimal
            float currentProgress = EvaluateWinProgress(_preferredWinType, gameState);
            float alternativeProgress = EvaluateWinProgress(GetAlternativeWinType(), gameState);
            if (alternativeProgress > currentProgress + 1.0f)
            {
                _preferredWinType = GetAlternativeWinType();
                Debug.Log($"[AdvancedAIPlayer] {Name} switched to {_preferredWinType} win strategy");
            }
        }

        private float EstimateOpponentHandStrength(Player opponent, GameState gameState)
        {
            if (opponent == null)
            {
                return 0.0f;
            }

            // Estimate based on tile count and observed behavior
            float baseStrength = 1.0f;

            // Fewer tiles generally means stronger hand
            if (opponent.Tiles != null)
            {
                baseStrength += (15 - opponent.Tiles.Count) * 0.5f;
            }

            // Add randomness to simulate uncertainty
            baseStrength += Random.Range(-1.0f, 1.0f);
            return Mathf.Max(0.0f, baseStrength);
        }

        private TileData FindSafestDiscard(GameState gameState)
        {
            if (Tiles == null || Tiles.Count == 0)
            {
                return default;
            }

            // Find tile that's least likely to help opponents
            TileData safestTile = default;
            float lowestRisk = float.MaxValue;
            for (int tileIndex = 0; tileIndex < Tiles.Count; tileIndex++)
            {
                OkeyPiece tile = Tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                float risk = CalculateDiscardRisk(tile.TileData, gameState);
                if (risk < lowestRisk)
                {
                    lowestRisk = risk;
                    safestTile = tile.TileData;
                }
            }

            return safestTile;
        }

        private float CalculateDiscardRisk(TileData tileData, GameState gameState)
        {
            if (tileData == null)
            {
                return 0.0f;
            }

            float risk = 1.0f;

            // Higher risk for middle numbers
            if (tileData.Number >= 4 && tileData.Number <= 10)
            {
                risk += 1.0f;
            }

            // Higher risk if this tile was recently discarded (opponents might need it)
            for (int discardIndex = 0; discardIndex < _observedDiscards.Count; discardIndex++)
            {
                TileData observedDiscard = _observedDiscards[discardIndex];
                if (observedDiscard.Number == tileData.Number || observedDiscard.Color == tileData.Color)
                {
                    risk += 0.5f;
                }
            }

            return risk;
        }

        private int CountPairs()
        {
            if (Tiles == null)
            {
                return 0;
            }

            Dictionary<string, int> tileCount = new Dictionary<string, int>();
            for (int tileIndex = 0; tileIndex < Tiles.Count; tileIndex++)
            {
                OkeyPiece tile = Tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                string tileKey = $"{tile.TileData.Number}_{tile.TileData.Color}";
                if (!tileCount.ContainsKey(tileKey))
                {
                    tileCount[tileKey] = 0;
                }

                tileCount[tileKey]++;
            }

            int pairCount = 0;
            foreach (int count in tileCount.Values)
            {
                pairCount += count / 2;
            }

            return pairCount;
        }

        private float EvaluateWinProgress(WinType winType, GameState gameState)
        {
            switch (winType)
            {
                case WinType.Pairs:
                    return CountPairs() / 7.0f; // 7 pairs needed
                case WinType.Normal:
                    return EvaluateHandStrength(gameState) / 10.0f; // Normalize to 0-1
                default:
                    return 0.0f;
            }
        }

        private WinType GetAlternativeWinType()
        {
            return _preferredWinType == WinType.Pairs ? WinType.Normal : WinType.Pairs;
        }

        private string DetermineCurrentStrategy(float handStrength)
        {
            if (handStrength >= AGGRESSIVE_THRESHOLD)
            {
                return "AGGRESSIVE";
            }

            if (handStrength <= DEFENSIVE_THRESHOLD)
            {
                return "DEFENSIVE";
            }

            return "BALANCED";
        }
    }
}