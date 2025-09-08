using System.Collections.Generic;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic.Tiles;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.GameLogic
{
    public sealed class GameRulesService : IGameRulesService
    {
        private readonly ITileService _tileService;
        
        private const int MIN_TILES_FOR_WIN = 14;
        private const int MAX_TILES_IN_HAND = 15;
        private const int MIN_SET_SIZE = 3;
        private const int MAX_SET_SIZE = 4;
        private const int PAIRS_WIN_COUNT = 7;

        [Inject]
        public GameRulesService(ITileService tileService)
        {
            _tileService = tileService;
        }

        public bool ValidatePlayerMove(PlayerAction action, GameState gameState)
        {
            if (action == null || gameState == null)
            {
                return false;
            }

            Player currentPlayer = gameState.CurrentPlayer;
            if (currentPlayer == null)
            {
                return false;
            }

            switch (action.ActionType)
            {
                case TurnAction.Draw:
                    return ValidateDrawAction(action, gameState);
                
                case TurnAction.Discard:
                    return ValidateDiscardAction(action, currentPlayer, gameState);
                
                case TurnAction.ShowIndicator:
                    return ValidateShowIndicatorAction(action, currentPlayer, gameState);
                
                case TurnAction.DeclareWin:
                    return ValidateDeclareWinAction(currentPlayer, gameState);
                
                default:
                    return false;
            }
        }

        public bool ValidateWinCondition(Player player, WinType winType)
        {
            if (player == null)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            if (playerTiles == null || playerTiles.Count != MIN_TILES_FOR_WIN)
            {
                return false;
            }

            List<TileData> tileDataList = ConvertToTileDataList(playerTiles);

            switch (winType)
            {
                case WinType.Normal:
                    return ValidateNormalWin(tileDataList);
                
                case WinType.Pairs:
                    return ValidatePairsWin(tileDataList);
                
                case WinType.Okey:
                    return ValidateOkeyWin(tileDataList);
                
                default:
                    return false;
            }
        }

        public bool ValidateSet(List<TileData> tiles, TileData jokerTile)
        {
            if (tiles == null || tiles.Count < MIN_SET_SIZE || tiles.Count > MAX_SET_SIZE)
            {
                return false;
            }

            // Group tiles by their effective number (considering jokers)
            Dictionary<int, int> numberCounts = new Dictionary<int, int>();
            Dictionary<OkeyColor, int> colorCounts = new Dictionary<OkeyColor, int>();
            int jokerCount = 0;

            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                
                if (IsJoker(tile, jokerTile))
                {
                    jokerCount++;
                }
                else
                {
                    if (!numberCounts.ContainsKey(tile.Number))
                    {
                        numberCounts[tile.Number] = 0;
                    }
                    numberCounts[tile.Number]++;
                    
                    if (!colorCounts.ContainsKey(tile.Color))
                    {
                        colorCounts[tile.Color] = 0;
                    }
                    colorCounts[tile.Color]++;
                }
            }

            // For a valid set, all tiles must have the same number but different colors
            if (numberCounts.Count > 1)
            {
                return false; // Multiple different numbers
            }

            if (numberCounts.Count == 0 && jokerCount < tiles.Count)
            {
                return false; // No regular tiles but not all jokers
            }

            // Check that we don't have duplicate colors (except jokers can fill gaps)
            int regularTileCount = tiles.Count - jokerCount;
            return colorCounts.Count == regularTileCount;
        }

        public bool ValidateSequence(List<TileData> tiles, TileData jokerTile)
        {
            if (tiles == null || tiles.Count < MIN_SET_SIZE)
            {
                return false;
            }

            // All tiles must be the same color (except jokers)
            OkeyColor sequenceColor = OkeyColor.Red;
            bool colorSet = false;
            List<int> numbers = new List<int>();
            int jokerCount = 0;

            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                
                if (IsJoker(tile, jokerTile))
                {
                    jokerCount++;
                }
                else
                {
                    if (!colorSet)
                    {
                        sequenceColor = tile.Color;
                        colorSet = true;
                    }
                    else if (tile.Color != sequenceColor)
                    {
                        return false; // Mixed colors
                    }
                    
                    numbers.Add(tile.Number);
                }
            }

            if (numbers.Count == 0)
            {
                return jokerCount >= MIN_SET_SIZE; // All jokers
            }

            // Sort numbers and check for valid sequence with jokers filling gaps
            numbers.Sort();
            
            int expectedLength = tiles.Count;
            int actualSpan = numbers[numbers.Count - 1] - numbers[0] + 1;
            int gaps = actualSpan - numbers.Count;
            
            return gaps == jokerCount && actualSpan == expectedLength;
        }

        public bool ValidatePairsWin(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count != MIN_TILES_FOR_WIN)
            {
                return false;
            }

            // Group tiles by number and color
            Dictionary<string, int> tileCounts = new Dictionary<string, int>();
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                string tileKey = $"{tile.Number}_{tile.Color}";
                
                if (!tileCounts.ContainsKey(tileKey))
                {
                    tileCounts[tileKey] = 0;
                }
                tileCounts[tileKey]++;
            }

            // Check if we have exactly 7 pairs
            int pairCount = 0;
            foreach (KeyValuePair<string, int> tileCount in tileCounts)
            {
                if (tileCount.Value == 2)
                {
                    pairCount++;
                }
                else if (tileCount.Value != 0)
                {
                    return false; // Invalid count (not 0 or 2)
                }
            }

            return pairCount == PAIRS_WIN_COUNT;
        }

        public List<PlayerAction> GetValidActions(Player player, GameState gameState)
        {
            List<PlayerAction> validActions = new List<PlayerAction>();
            
            if (player == null || gameState == null)
            {
                return validActions;
            }

            // Draw actions
            if (CanDrawFromPile(gameState))
            {
                PlayerAction drawFromPile = PlayerAction.CreateDrawAction(player.Id);
                validActions.Add(drawFromPile);
            }

            if (CanDrawFromDiscard(gameState))
            {
                PlayerAction drawFromDiscard = PlayerAction.CreateDrawAction(player.Id);
                validActions.Add(drawFromDiscard);
            }

            // Discard actions
            List<OkeyPiece> playerTiles = player.GetTiles();
            if (playerTiles != null)
            {
                for (int index = 0; index < playerTiles.Count; index++)
                {
                    OkeyPiece tile = playerTiles[index];
                    TileData tileData = new TileData(tile.Number, tile.Color, tile.PieceType);
                    
                    if (CanDiscard(player, tileData))
                    {
                        GridPosition discardPosition = new GridPosition(0, 0); // Default position
                        PlayerAction discardAction = PlayerAction.CreateDiscardAction(player.Id, tileData, discardPosition);
                        validActions.Add(discardAction);
                    }
                }
            }

            // Show indicator action
            if (gameState.IndicatorTile != null && CanShowIndicator(player, gameState.IndicatorTile))
            {
                PlayerAction showIndicatorAction = PlayerAction.CreateShowIndicatorAction(player.Id, gameState.IndicatorTile);
                validActions.Add(showIndicatorAction);
            }

            // Declare win action
            WinType? winType = CheckWinCondition(player, gameState.JokerTile);
            if (winType.HasValue)
            {
                PlayerAction declareWinAction = PlayerAction.CreateDeclareWinAction(player.Id, winType.Value);
                validActions.Add(declareWinAction);
            }

            return validActions;
        }

        public WinType? CheckWinCondition(Player player, TileData jokerTile)
        {
            if (player == null)
            {
                return null;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            if (playerTiles == null || playerTiles.Count != MIN_TILES_FOR_WIN)
            {
                return null;
            }

            List<TileData> tileDataList = ConvertToTileDataList(playerTiles);

            // Check for pairs win first (highest priority)
            if (ValidatePairsWin(tileDataList))
            {
                return WinType.Pairs;
            }

            // Check for Okey win (special win with jokers)
            if (ValidateOkeyWin(tileDataList))
            {
                return WinType.Okey;
            }

            // Check for normal win
            if (ValidateNormalWin(tileDataList))
            {
                return WinType.Normal;
            }

            return null;
        }

        public int CalculateWinScore(WinType winType)
        {
            switch (winType)
            {
                case WinType.Normal:
                    return 10;
                case WinType.Pairs:
                    return 20;
                case WinType.Okey:
                    return 30;
                default:
                    return 0;
            }
        }

        public int CalculatePlayerPenalty(Player player, WinType winnerWinType)
        {
            if (player == null)
            {
                return 0;
            }

            List<OkeyPiece> remainingTiles = player.GetTiles();
            if (remainingTiles == null)
            {
                return 0;
            }

            int penalty = 0;
            
            for (int index = 0; index < remainingTiles.Count; index++)
            {
                OkeyPiece tile = remainingTiles[index];
                penalty += CalculateTilePenalty(tile);
            }

            // Apply multiplier based on winner's win type
            float multiplier = GetPenaltyMultiplier(winnerWinType);
            return Mathf.RoundToInt(penalty * multiplier);
        }

        public bool CanDrawFromPile(GameState gameState)
        {
            if (gameState == null)
            {
                return false;
            }

            return gameState.DrawPile != null && gameState.DrawPile.Count > 0;
        }

        public bool CanDrawFromDiscard(GameState gameState)
        {
            if (gameState == null)
            {
                return false;
            }

            return gameState.DiscardPile != null && gameState.DiscardPile.Count > 0;
        }

        public bool CanDiscard(Player player, TileData tileData)
        {
            if (player == null || tileData == null)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            if (playerTiles == null)
            {
                return false;
            }

            // Check if player actually has this tile
            for (int index = 0; index < playerTiles.Count; index++)
            {
                OkeyPiece tile = playerTiles[index];
                if (tile.Number == tileData.Number && 
                    tile.Color == tileData.Color && 
                    tile.PieceType == tileData.PieceType)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanShowIndicator(Player player, TileData indicatorTile)
        {
            if (player == null || indicatorTile == null)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            if (playerTiles == null)
            {
                return false;
            }

            // Player can show indicator if they have the indicator tile
            for (int index = 0; index < playerTiles.Count; index++)
            {
                OkeyPiece tile = playerTiles[index];
                if (tile.Number == indicatorTile.Number && 
                    tile.Color == indicatorTile.Color && 
                    tile.PieceType == indicatorTile.PieceType)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ValidateDrawAction(PlayerAction action, GameState gameState)
        {
            Player currentPlayer = gameState.CurrentPlayer;
            if (currentPlayer == null)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = currentPlayer.GetTiles();
            if (playerTiles != null && playerTiles.Count >= MAX_TILES_IN_HAND)
            {
                return false; // Player already has maximum tiles
            }

            // Check if there are tiles available to draw
            return CanDrawFromPile(gameState) || CanDrawFromDiscard(gameState);
        }

        private bool ValidateDiscardAction(PlayerAction action, Player player, GameState gameState)
        {
            if (action.TileData == null)
            {
                return false;
            }

            return CanDiscard(player, action.TileData);
        }

        private bool ValidateShowIndicatorAction(PlayerAction action, Player player, GameState gameState)
        {
            return CanShowIndicator(player, gameState.IndicatorTile);
        }

        private bool ValidateDeclareWinAction(Player player, GameState gameState)
        {
            WinType? winType = CheckWinCondition(player, gameState.JokerTile);
            return winType.HasValue;
        }

        private bool ValidateNormalWin(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count != MIN_TILES_FOR_WIN)
            {
                return false;
            }

            // Try to form valid sets and sequences
            List<List<TileData>> validSets = _tileService.FindAllValidSets(tiles);
            List<List<TileData>> validSequences = _tileService.FindAllValidSequences(tiles);

            // Try different combinations of sets and sequences
            return TryFormWinningCombination(tiles, validSets, validSequences);
        }

        private bool ValidateOkeyWin(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count != MIN_TILES_FOR_WIN)
            {
                return false;
            }

            // Okey win requires specific conditions with jokers
            int jokerCount = 0;
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                if (tile.PieceType == OkeyPieceType.FalseJoker)
                {
                    jokerCount++;
                }
            }

            // Okey win typically requires at least one joker and specific arrangements
            return jokerCount > 0 && ValidateNormalWin(tiles);
        }

        private bool TryFormWinningCombination(List<TileData> tiles, List<List<TileData>> validSets, List<List<TileData>> validSequences)
        {
            // This is a simplified implementation
            // In a full implementation, you would need to try all possible combinations
            // of sets and sequences to see if they use all tiles exactly once
            
            List<TileData> usedTiles = new List<TileData>();
            
            // Try to use valid sets
            for (int setIndex = 0; setIndex < validSets.Count; setIndex++)
            {
                List<TileData> set = validSets[setIndex];
                bool canUseSet = true;
                
                for (int tileIndex = 0; tileIndex < set.Count; tileIndex++)
                {
                    if (usedTiles.Contains(set[tileIndex]))
                    {
                        canUseSet = false;
                        break;
                    }
                }
                
                if (canUseSet)
                {
                    usedTiles.AddRange(set);
                }
            }
            
            // Try to use valid sequences for remaining tiles
            for (int seqIndex = 0; seqIndex < validSequences.Count; seqIndex++)
            {
                List<TileData> sequence = validSequences[seqIndex];
                bool canUseSequence = true;
                
                for (int tileIndex = 0; tileIndex < sequence.Count; tileIndex++)
                {
                    if (usedTiles.Contains(sequence[tileIndex]))
                    {
                        canUseSequence = false;
                        break;
                    }
                }
                
                if (canUseSequence)
                {
                    usedTiles.AddRange(sequence);
                }
            }
            
            return usedTiles.Count == tiles.Count;
        }

        private bool IsJoker(TileData tile, TileData jokerTile)
        {
            if (tile == null)
            {
                return false;
            }

            // False jokers are always jokers
            if (tile.PieceType == OkeyPieceType.FalseJoker)
            {
                return true;
            }

            // Regular tiles that match the joker specification
            if (jokerTile != null)
            {
                return tile.Number == jokerTile.Number && 
                       tile.Color == jokerTile.Color && 
                       tile.PieceType == OkeyPieceType.Normal;
            }

            return false;
        }

        private List<TileData> ConvertToTileDataList(List<OkeyPiece> pieces)
        {
            List<TileData> tileDataList = new List<TileData>();
            
            if (pieces != null)
            {
                for (int index = 0; index < pieces.Count; index++)
                {
                    OkeyPiece piece = pieces[index];
                    TileData tileData = new TileData(piece.Number, piece.Color, piece.PieceType);
                    tileDataList.Add(tileData);
                }
            }
            
            return tileDataList;
        }

        private int CalculateTilePenalty(OkeyPiece tile)
        {
            if (tile == null)
            {
                return 0;
            }

            if (tile.PieceType == OkeyPieceType.FalseJoker)
            {
                return 25; // High penalty for false joker
            }

            // Regular tiles: penalty equals face value
            return tile.Number;
        }

        private float GetPenaltyMultiplier(WinType winnerWinType)
        {
            switch (winnerWinType)
            {
                case WinType.Normal:
                    return 1.0f;
                case WinType.Pairs:
                    return 1.5f;
                case WinType.Okey:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }
    }
}
