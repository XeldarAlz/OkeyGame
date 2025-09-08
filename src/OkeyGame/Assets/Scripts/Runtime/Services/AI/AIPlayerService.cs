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
    public sealed class AIPlayerService : IAIPlayerService
    {
        private readonly IGameRulesService _gameRulesService;
        
        private readonly IAIDecisionService _aiDecisionService;
        
        private readonly IRandomProvider _randomProvider;
        
        [Inject]
        public AIPlayerService(IGameRulesService gameRulesService, IAIDecisionService aiDecisionService, IRandomProvider randomProvider)
        {
            _gameRulesService = gameRulesService;
            _aiDecisionService = aiDecisionService;
            _randomProvider = randomProvider;
        }

        public async UniTask<PlayerAction> GetBestActionAsync(Player aiPlayer, GameState gameState)
        {
            if (aiPlayer == null || gameState == null)
            {
                Debug.LogError("[AIPlayerService] Invalid parameters for GetBestActionAsync");
                return default;
            }

            try
            {
                // Cast to IAIPlayer to access Difficulty property
                if (aiPlayer is IAIPlayer aiPlayerInterface)
                {
                    AIDifficulty difficulty = aiPlayerInterface.Difficulty;
                    return await _aiDecisionService.DecideActionAsync(aiPlayer, gameState, difficulty);
                }
                else
                {
                    Debug.LogError($"[AIPlayerService] Player {aiPlayer.Name} is not an AI player");
                    return await GetRandomValidActionAsync(aiPlayer, gameState);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AIPlayerService] Error in GetBestActionAsync: {exception.Message}");
                return await GetRandomValidActionAsync(aiPlayer, gameState);
            }
        }

        public async UniTask<PlayerAction> GetRandomValidActionAsync(Player aiPlayer, GameState gameState)
        {
            if (aiPlayer == null || gameState == null)
            {
                Debug.LogError("[AIPlayerService] Invalid parameters for GetRandomValidActionAsync");
                return default;
            }

            await UniTask.Delay(50); // Small delay to simulate thinking

            List<PlayerAction> validActions = _gameRulesService.GetValidActions(aiPlayer, gameState);
            if (validActions == null || validActions.Count == 0)
            {
                Debug.LogWarning("[AIPlayerService] No valid actions available for AI player");
                return default;
            }

            return validActions[_randomProvider.Range(0, validActions.Count)];
        }

        public async UniTask<PlayerAction> GetStrategicActionAsync(Player aiPlayer, GameState gameState, AIDifficulty difficulty)
        {
            if (aiPlayer == null || gameState == null)
            {
                Debug.LogError("[AIPlayerService] Invalid parameters for GetStrategicActionAsync");
                return default;
            }

            try
            {
                return await _aiDecisionService.DecideActionAsync(aiPlayer, gameState, difficulty);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AIPlayerService] Error in GetStrategicActionAsync: {exception.Message}");
                return await GetRandomValidActionAsync(aiPlayer, gameState);
            }
        }

        public float EvaluateHandStrength(Player player, TileData jokerTile)
        {
            if (player == null || player.Tiles == null)
            {
                return 0.0f;
            }

            float handStrength = 0.0f;
            List<OkeyPiece> tiles = player.GetTiles();

            // Evaluate based on potential sets and sequences
            handStrength += EvaluatePotentialSets(tiles, jokerTile);
            handStrength += EvaluatePotentialSequences(tiles, jokerTile);
            handStrength += EvaluateJokerCount(tiles, jokerTile);
            handStrength += EvaluatePairsForPairsWin(tiles);

            return handStrength;
        }

        public bool ShouldDrawFromDiscard(Player player, GameState gameState)
        {
            if (player == null || gameState == null || gameState.DiscardPile == null || gameState.DiscardPile.Count == 0)
            {
                return false;
            }

            OkeyPiece topDiscardTile = gameState.DiscardPile[gameState.DiscardPile.Count - 1];
            if (topDiscardTile == null)
            {
                return false;
            }

            // Check if the top discard tile would help complete a set or sequence
            return WouldTileHelpComplete(player, topDiscardTile.TileData, gameState.JokerTile);
        }

        public TileData SelectBestDiscard(Player player, GameState gameState)
        {
            if (player == null || player.Tiles == null || player.Tiles.Count == 0)
            {
                return default;
            }

            TileData bestDiscard = default;
            float lowestValue = float.MaxValue;

            for (int tileIndex = 0; tileIndex < player.Tiles.Count; tileIndex++)
            {
                OkeyPiece tile = player.Tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                float tileValue = EvaluateTileValue(tile.TileData, player, gameState);
                if (tileValue < lowestValue)
                {
                    lowestValue = tileValue;
                    bestDiscard = tile.TileData;
                }
            }

            return bestDiscard;
        }

        public bool ShouldDeclareWin(Player player, GameState gameState)
        {
            if (player == null || gameState == null)
            {
                return false;
            }

            // Check if player can win with current hand
            WinType? winType = _gameRulesService.CheckWinCondition(player, gameState.JokerTile);
            return winType.HasValue;
        }

        private float EvaluatePotentialSets(List<OkeyPiece> tiles, TileData jokerTile)
        {
            float setStrength = 0.0f;
            Dictionary<int, List<OkeyColor>> numberGroups = new Dictionary<int, List<OkeyColor>>();

            // Group tiles by number
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                int number = tile.TileData.Number;
                if (!numberGroups.ContainsKey(number))
                {
                    numberGroups[number] = new List<OkeyColor>();
                }
                numberGroups[number].Add(tile.TileData.Color);
            }

            // Evaluate potential sets
            foreach (var group in numberGroups)
            {
                int uniqueColors = group.Value.Count;
                if (uniqueColors >= 2)
                {
                    setStrength += uniqueColors * 0.5f; // Bonus for potential sets
                }
            }

            return setStrength;
        }

        private float EvaluatePotentialSequences(List<OkeyPiece> tiles, TileData jokerTile)
        {
            float sequenceStrength = 0.0f;
            Dictionary<OkeyColor, List<int>> colorGroups = new Dictionary<OkeyColor, List<int>>();

            // Group tiles by color
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                OkeyColor color = tile.TileData.Color;
                if (!colorGroups.ContainsKey(color))
                {
                    colorGroups[color] = new List<int>();
                }
                colorGroups[color].Add(tile.TileData.Number);
            }

            // Evaluate potential sequences
            foreach (var group in colorGroups)
            {
                List<int> numbers = group.Value;
                numbers.Sort();
                
                int consecutiveCount = 1;
                for (int numberIndex = 1; numberIndex < numbers.Count; numberIndex++)
                {
                    if (numbers[numberIndex] == numbers[numberIndex - 1] + 1)
                    {
                        consecutiveCount++;
                    }
                    else
                    {
                        if (consecutiveCount >= 2)
                        {
                            sequenceStrength += consecutiveCount * 0.3f;
                        }
                        consecutiveCount = 1;
                    }
                }
                
                if (consecutiveCount >= 2)
                {
                    sequenceStrength += consecutiveCount * 0.3f;
                }
            }

            return sequenceStrength;
        }

        private float EvaluateJokerCount(List<OkeyPiece> tiles, TileData jokerTile)
        {
            if (jokerTile == null)
            {
                return 0.0f;
            }

            float jokerBonus = 0.0f;
            
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                if (tile == null)
                {
                    continue;
                }

                // Check if this tile is a joker
                if (IsJokerTile(tile.TileData, jokerTile))
                {
                    jokerBonus += 1.0f; // High value for jokers
                }
            }

            return jokerBonus;
        }

        private float EvaluatePairsForPairsWin(List<OkeyPiece> tiles)
        {
            float pairsStrength = 0.0f;
            Dictionary<string, int> tileCount = new Dictionary<string, int>();

            // Count identical tiles
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
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

            // Count pairs
            int pairCount = 0;
            foreach (var count in tileCount.Values)
            {
                pairCount += count / 2;
            }

            // Bonus for pairs (7 pairs needed for pairs win)
            pairsStrength = pairCount * 0.4f;
            if (pairCount >= 6)
            {
                pairsStrength += 2.0f; // Big bonus when close to pairs win
            }

            return pairsStrength;
        }

        private bool WouldTileHelpComplete(Player player, TileData tileData, TileData jokerTile)
        {
            if (player == null || player.Tiles == null || tileData == null)
            {
                return false;
            }

            // Simple heuristic: check if this tile would form a pair or help with sequences
            for (int tileIndex = 0; tileIndex < player.Tiles.Count; tileIndex++)
            {
                OkeyPiece existingTile = player.Tiles[tileIndex];
                if (existingTile == null)
                {
                    continue;
                }

                // Check for pair potential
                if (existingTile.TileData.Number == tileData.Number && existingTile.TileData.Color == tileData.Color)
                {
                    return true;
                }

                // Check for sequence potential
                if (existingTile.TileData.Color == tileData.Color)
                {
                    int numberDiff = Mathf.Abs(existingTile.TileData.Number - tileData.Number);
                    if (numberDiff == 1 || numberDiff == 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float EvaluateTileValue(TileData tileData, Player player, GameState gameState)
        {
            if (tileData == null)
            {
                return 0.0f;
            }

            float value = 1.0f;

            // Jokers are very valuable
            if (IsJokerTile(tileData, gameState.JokerTile))
            {
                value += 3.0f;
            }

            // Middle numbers are generally more useful
            int number = tileData.Number;
            if (number >= 4 && number <= 10)
            {
                value += 0.5f;
            }

            // Check if this tile helps complete sets or sequences
            if (WouldTileHelpComplete(player, tileData, gameState.JokerTile))
            {
                value += 1.0f;
            }

            return value;
        }

        private bool IsJokerTile(TileData tileData, TileData jokerTile)
        {
            if (tileData == null || jokerTile == null)
            {
                return false;
            }

            return tileData.Number == jokerTile.Number && tileData.Color == jokerTile.Color;
        }
    }
}
