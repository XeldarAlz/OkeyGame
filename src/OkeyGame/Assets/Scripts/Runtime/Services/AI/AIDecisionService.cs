using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Utilities;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.AI
{
    public sealed class AIDecisionService : IAIDecisionService
    {
        private readonly IGameRulesService _gameRulesService;
        
        private readonly IRandomProvider _randomProvider;
        
        [Inject]
        public AIDecisionService(IGameRulesService gameRulesService, IRandomProvider randomProvider)
        {
            _gameRulesService = gameRulesService;
            _randomProvider = randomProvider;
        }

        public async UniTask<PlayerAction> DecideActionAsync(Player aiPlayer, GameState gameState, AIDifficulty difficulty)
        {
            if (aiPlayer == null || gameState == null)
            {
                Debug.LogError("[AIDecisionService] Invalid parameters for DecideActionAsync");
                return default;
            }

            try
            {
                return difficulty switch
                {
                    AIDifficulty.Beginner => await GetBeginnerActionAsync(aiPlayer, gameState),
                    AIDifficulty.Intermediate => await GetIntermediateActionAsync(aiPlayer, gameState),
                    AIDifficulty.Advanced => await GetAdvancedActionAsync(aiPlayer, gameState),
                    _ => await GetBeginnerActionAsync(aiPlayer, gameState)
                };
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AIDecisionService] Error in DecideActionAsync: {exception.Message}");
                return await GetBeginnerActionAsync(aiPlayer, gameState);
            }
        }

        public float CalculateActionValue(PlayerAction action, GameState gameState)
        {
            if (gameState == null)
            {
                return 0.0f;
            }

            float baseValue = 1.0f;

            switch (action.ActionType)
            {
                case TurnAction.Draw:
                    return CalculateDrawActionValue(action);
                
                case TurnAction.Discard:
                    return CalculateDiscardActionValue(action);
                
                case TurnAction.ShowIndicator:
                    return CalculateShowIndicatorValue();
                
                default:
                    return baseValue;
            }
        }

        public PlayerAction GetBestAction(Player aiPlayer, GameState gameState, AIDifficulty difficulty)
        {
            if (aiPlayer == null || gameState == null)
            {
                return default;
            }

            List<PlayerAction> validActions = _gameRulesService.GetValidActions(aiPlayer, gameState);
            if (validActions == null || validActions.Count == 0)
            {
                return default;
            }

            if (difficulty == AIDifficulty.Beginner)
            {
                return validActions[_randomProvider.Range(0, validActions.Count)];
            }

            PlayerAction bestAction = validActions[0];
            float bestValue = CalculateActionValue(bestAction, gameState);

            for (int actionIndex = 1; actionIndex < validActions.Count; actionIndex++)
            {
                PlayerAction currentAction = validActions[actionIndex];
                float currentValue = CalculateActionValue(currentAction, gameState);
                
                if (currentValue > bestValue)
                {
                    bestValue = currentValue;
                    bestAction = currentAction;
                }
            }

            return bestAction;
        }

        private async UniTask<PlayerAction> GetBeginnerActionAsync(Player aiPlayer, GameState gameState)
        {
            await UniTask.Delay(100); // Simulate thinking time
            
            List<PlayerAction> validActions = _gameRulesService.GetValidActions(aiPlayer, gameState);
            if (validActions == null || validActions.Count == 0)
            {
                return default;
            }

            return validActions[_randomProvider.Range(0, validActions.Count)];
        }

        private async UniTask<PlayerAction> GetIntermediateActionAsync(Player aiPlayer, GameState gameState)
        {
            await UniTask.Delay(200); // Simulate thinking time
            
            List<PlayerAction> validActions = _gameRulesService.GetValidActions(aiPlayer, gameState);
            if (validActions == null || validActions.Count == 0)
            {
                return default;
            }

            // Prioritize actions that complete sets or sequences
            PlayerAction bestAction = default;
            float bestValue = -1.0f;

            for (int actionIndex = 0; actionIndex < validActions.Count; actionIndex++)
            {
                PlayerAction action = validActions[actionIndex];
                float actionValue = CalculateActionValue(action, gameState);
                
                if (actionValue > bestValue)
                {
                    bestValue = actionValue;
                    bestAction = action;
                }
            }

            if (bestAction.Equals(default(PlayerAction)) && validActions.Count > 0)
            {
                return validActions[_randomProvider.Range(0, validActions.Count)];
            }
            
            return bestAction;
        }

        private async UniTask<PlayerAction> GetAdvancedActionAsync(Player aiPlayer, GameState gameState)
        {
            await UniTask.Delay(300); // Simulate thinking time
            
            List<PlayerAction> validActions = _gameRulesService.GetValidActions(aiPlayer, gameState);
            if (validActions == null || validActions.Count == 0)
            {
                return default;
            }

            // Advanced AI considers multiple factors
            PlayerAction bestAction = default;
            float bestValue = -1.0f;

            for (int actionIndex = 0; actionIndex < validActions.Count; actionIndex++)
            {
                PlayerAction action = validActions[actionIndex];
                float actionValue = CalculateAdvancedActionValue(action, gameState);
                
                if (actionValue > bestValue)
                {
                    bestValue = actionValue;
                    bestAction = action;
                }
            }

            if (bestAction.Equals(default(PlayerAction)) && validActions.Count > 0)
            {
                return validActions[_randomProvider.Range(0, validActions.Count)];
            }
            
            return bestAction;
        }

        private float CalculateDrawActionValue(PlayerAction action)
        {
            float baseValue = 1.0f;
            
            if (action.ActionType == TurnAction.Draw)
            {
                // Prefer drawing from discard if it helps complete sets
                if (!action.ToPosition.Equals(default))
                {
                    baseValue += 0.5f; // Drawing from discard pile
                }
                else
                {
                    baseValue += 0.3f; // Drawing from pile
                }
            }

            return baseValue;
        }

        private float CalculateDiscardActionValue(PlayerAction action)
        {
            float baseValue = 1.0f;
            
            if (action.ActionType == TurnAction.Discard)
            {
                // Prefer discarding tiles that are less useful
                TileData tileToDiscard = action.TileData;
                
                // Lower value for higher numbers (less useful)
                baseValue += (14 - tileToDiscard.Number) * 0.1f;
                
                // Consider if this tile might be useful to opponents
                baseValue -= 0.3f; // Penalty for potentially useful tiles
            }

            return baseValue;
        }

        private float CalculateShowIndicatorValue()
        {
            // Showing indicator is generally valuable for bonus points
            return 2.0f;
        }

        private float CalculateAdvancedActionValue(PlayerAction action, GameState gameState)
        {
            float baseValue = CalculateActionValue(action, gameState);
            
            // Advanced considerations
            baseValue += AnalyzeHandCompletion(action);
            baseValue += AnalyzeOpponentThreats(action);
            baseValue += AnalyzeWinProbability(action);
            
            return baseValue;
        }

        private float AnalyzeHandCompletion(PlayerAction action)
        {
            // Analyze how much this action helps complete the hand
            float completionValue = 0.0f;
            
            if (action.ActionType == TurnAction.Draw)
            {
                completionValue += 0.5f; // Drawing generally helps
            }
            else if (action.ActionType == TurnAction.Discard)
            {
                completionValue += 0.3f; // Discarding clears space
            }
            
            return completionValue;
        }

        private float AnalyzeOpponentThreats(PlayerAction action)
        {
            // Analyze if this action helps or hinders opponents
            float threatValue = 0.0f;
            
            if (action.ActionType == TurnAction.Discard && !action.TileData.Equals(default(TileData)))
            {
                // Penalty for discarding potentially useful tiles to opponents
                threatValue -= 0.2f;
            }
            
            return threatValue;
        }

        private float AnalyzeWinProbability(PlayerAction action)
        {
            // Analyze if this action increases win probability
            float winValue = 0.0f;
            
            // Simple heuristic: actions that reduce hand size are good
            if (action.ActionType == TurnAction.Discard)
            {
                winValue += 0.4f;
            }
            
            return winValue;
        }
    }
}
