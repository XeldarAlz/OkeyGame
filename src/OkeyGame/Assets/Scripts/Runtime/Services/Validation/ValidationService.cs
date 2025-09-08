using System.Collections.Generic;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Services.Validation
{
    public sealed class ValidationService : IValidationService
    {
        private readonly IGameRulesService _gameRulesService;
        
        private const int MIN_TILES_FOR_SET = 3;
        private const int MAX_TILES_FOR_SET = 4;
        private const int MIN_TILES_FOR_SEQUENCE = 3;
        private const int TILES_FOR_WIN = 14;
        private const int PAIRS_WIN_COUNT = 7;
        private const int MAX_GRID_POSITION = 14;

        [Inject]
        public ValidationService(IGameRulesService gameRulesService)
        {
            _gameRulesService = gameRulesService;
        }

        public ValidationResult ValidateMove(PlayerAction action, GameState gameState)
        {
            if (action == null)
            {
                return ValidationResult.Invalid;
            }

            if (gameState == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if it's the player's turn
            if (!IsPlayersTurn(action.PlayerId, gameState))
            {
                return ValidationResult.InvalidMove;
            }

            // Validate specific action types
            switch (action.ActionType)
            {
                case TurnAction.Draw:
                    return ValidateDrawAction(action, gameState);
                
                case TurnAction.Discard:
                    return ValidateDiscardAction(action, gameState);
                
                case TurnAction.ShowIndicator:
                    return ValidateShowIndicatorAction(action, gameState);
                
                case TurnAction.DeclareWin:
                    return ValidateDeclareWinAction(action, gameState);
                
                default:
                    return ValidationResult.Invalid;
            }
        }

        public ValidationResult ValidateTileSet(List<TileData> tiles, TileData jokerTile)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return ValidationResult.Invalid;
            }

            if (tiles.Count < MIN_TILES_FOR_SET || tiles.Count > MAX_TILES_FOR_SET)
            {
                return ValidationResult.InvalidSet;
            }

            // Check if all tiles have the same number but different colors
            int setNumber = GetEffectiveNumber(tiles[0], jokerTile);
            List<OkeyColor> usedColors = new List<OkeyColor>();
            int jokerCount = 0;

            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                
                if (IsJoker(tile, jokerTile))
                {
                    jokerCount++;
                    continue;
                }

                if (tile.Number != setNumber)
                {
                    return ValidationResult.InvalidSet;
                }

                if (usedColors.Contains(tile.Color))
                {
                    return ValidationResult.InvalidSet;
                }

                usedColors.Add(tile.Color);
            }

            // Check if we have enough non-joker tiles
            if (usedColors.Count < MIN_TILES_FOR_SET - jokerCount)
            {
                return ValidationResult.InsufficientTiles;
            }

            return ValidationResult.Valid;
        }

        public ValidationResult ValidateTileSequence(List<TileData> tiles, TileData jokerTile)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return ValidationResult.Invalid;
            }

            if (tiles.Count < MIN_TILES_FOR_SEQUENCE)
            {
                return ValidationResult.InsufficientTiles;
            }

            // Check if all tiles have the same color but sequential numbers
            OkeyColor? sequenceColor = null;
            List<int> numbers = new List<int>();
            int jokerCount = 0;

            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                
                if (IsJoker(tile, jokerTile))
                {
                    jokerCount++;
                    continue;
                }

                if (sequenceColor == null)
                {
                    sequenceColor = tile.Color;
                }
                else if (tile.Color != sequenceColor)
                {
                    return ValidationResult.InvalidSequence;
                }

                numbers.Add(tile.Number);
            }

            if (numbers.Count == 0 && jokerCount < MIN_TILES_FOR_SEQUENCE)
            {
                return ValidationResult.InsufficientTiles;
            }

            // Sort numbers and validate sequence
            numbers.Sort();
            
            if (numbers.Count > 0)
            {
                int expectedLength = tiles.Count;
                int minNumber = numbers[0];
                int maxNumber = numbers[numbers.Count - 1];
                int actualSpan = maxNumber - minNumber + 1;
                int gaps = actualSpan - numbers.Count;

                if (gaps != jokerCount)
                {
                    return ValidationResult.InvalidSequence;
                }

                if (actualSpan != expectedLength)
                {
                    return ValidationResult.InvalidSequence;
                }

                // Check for duplicates
                for (int index = 1; index < numbers.Count; index++)
                {
                    if (numbers[index] == numbers[index - 1])
                    {
                        return ValidationResult.InvalidSequence;
                    }
                }
            }

            return ValidationResult.Valid;
        }

        public ValidationResult ValidatePlayerHand(Player player, TileData jokerTile)
        {
            if (player == null)
            {
                return ValidationResult.Invalid;
            }

            if (player.Tiles.Count == 0)
            {
                return ValidationResult.InsufficientTiles;
            }

            // Check if player has a valid winning hand
            if (HasValidWinningHand(player, jokerTile))
            {
                return ValidationResult.Valid;
            }

            return ValidationResult.Invalid;
        }

        public ValidationResult ValidateGridPosition(GridPosition position)
        {
            if (position.Column < 0 || position.Column > MAX_GRID_POSITION || position.Row < 0 || position.Row > 1)
            {
                return ValidationResult.InvalidMove;
            }

            return ValidationResult.Valid;
        }

        public ValidationResult ValidateTilePlacement(TileData tileData, GridPosition position, GameState gameState)
        {
            if (gameState == null)
            {
                return ValidationResult.Invalid;
            }

            // Validate grid position
            ValidationResult positionResult = ValidateGridPosition(position);
            if (positionResult != ValidationResult.Valid)
            {
                return positionResult;
            }

            // Check if position is already occupied
            Player currentPlayer = gameState.CurrentPlayer;
            if (currentPlayer == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if the position is already occupied by iterating through player's tiles
            foreach (OkeyPiece tile in currentPlayer.Tiles)
            {
                if (tile.GridPosition.Equals(position))
                {
                    return ValidationResult.InvalidMove;
                }
            }

            return ValidationResult.Valid;
        }

        public bool IsValidSet(List<TileData> tiles, TileData jokerTile)
        {
            ValidationResult result = ValidateTileSet(tiles, jokerTile);
            return result == ValidationResult.Valid;
        }

        public bool IsValidSequence(List<TileData> tiles, TileData jokerTile)
        {
            ValidationResult result = ValidateTileSequence(tiles, jokerTile);
            return result == ValidationResult.Valid;
        }

        public bool IsValidPairsHand(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count != TILES_FOR_WIN)
            {
                return false;
            }

            // Group tiles by number and color
            Dictionary<string, List<TileData>> pairs = new Dictionary<string, List<TileData>>();
            
            foreach (TileData tile in tiles)
            {
                string key = $"{tile.Number}_{tile.Color}";
                
                if (!pairs.ContainsKey(key))
                {
                    pairs[key] = new List<TileData>();
                }
                
                pairs[key].Add(tile);
            }

            // Check if we have exactly 7 pairs
            int pairCount = 0;
            
            foreach (var pair in pairs.Values)
            {
                if (pair.Count == 2)
                {
                    pairCount++;
                }
                else if (pair.Count > 2)
                {
                    // More than 2 of the same tile is not a valid pairs hand
                    return false;
                }
            }

            return pairCount == PAIRS_WIN_COUNT;
        }

        public bool CanPlayerWin(Player player, TileData jokerTile)
        {
            if (player == null || player.Tiles.Count != TILES_FOR_WIN)
            {
                return false;
            }

            // Convert player tiles to TileData list
            List<TileData> tiles = new List<TileData>();
            foreach (OkeyPiece piece in player.Tiles)
            {
                tiles.Add(new TileData(piece.Number, piece.Color, piece.PieceType));
            }

            // Check if player has a valid winning hand
            return HasValidWinningHand(player, jokerTile);
        }

        public bool HasValidWinningHand(Player player, TileData jokerTile)
        {
            if (player == null || player.Tiles.Count != TILES_FOR_WIN)
            {
                return false;
            }

            // Convert player tiles to TileData list
            List<TileData> tiles = new List<TileData>();
            foreach (OkeyPiece piece in player.Tiles)
            {
                tiles.Add(new TileData(piece.Number, piece.Color, piece.PieceType));
            }

            // Check for pairs win condition
            if (IsValidPairsHand(tiles))
            {
                return true;
            }

            // Try to find valid sets and sequences
            return _gameRulesService.ValidateWinCondition(player, WinType.Normal);
        }

        private ValidationResult ValidateDrawAction(PlayerAction action, GameState gameState)
        {
            if (action == null || gameState == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if there are tiles left to draw
            if (gameState.DrawPile.Count == 0)
            {
                return ValidationResult.InvalidMove;
            }

            // Check if player already has the maximum number of tiles
            Player player = gameState.GetPlayerById(action.PlayerId);
            if (player == null)
            {
                return ValidationResult.Invalid;
            }

            if (player.Tiles.Count >= TILES_FOR_WIN)
            {
                return ValidationResult.InvalidMove;
            }

            return ValidationResult.Valid;
        }

        private ValidationResult ValidateDiscardAction(PlayerAction action, GameState gameState)
        {
            if (gameState == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if player has the tile
            Player player = gameState.GetPlayerById(action.PlayerId);
            if (player == null)
            {
                return ValidationResult.Invalid;
            }

            // Validate the from position
            ValidationResult positionResult = ValidateGridPosition(action.FromPosition);
            if (positionResult != ValidationResult.Valid)
            {
                return positionResult;
            }

            // Check if the tile exists at the specified position
            OkeyPiece tileAtPosition = null;
            foreach (OkeyPiece tile in player.Tiles)
            {
                if (tile.GridPosition.Equals(action.FromPosition))
                {
                    tileAtPosition = tile;
                    break;
                }
            }
            
            if (tileAtPosition == null)
            {
                return ValidationResult.InvalidMove;
            }

            // Check if the tile data matches
            if (tileAtPosition.Number != action.TileData.Number || 
                tileAtPosition.Color != action.TileData.Color)
            {
                return ValidationResult.InvalidMove;
            }

            return ValidationResult.Valid;
        }

        private ValidationResult ValidateShowIndicatorAction(PlayerAction action, GameState gameState)
        {
            if (action == null || gameState == null || action.TileData == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if player has the indicator tile
            Player player = gameState.GetPlayerById(action.PlayerId);
            if (player == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if the tile is the indicator tile
            if (action.TileData.Number != gameState.IndicatorTile.Number || 
                action.TileData.Color != gameState.IndicatorTile.Color)
            {
                return ValidationResult.InvalidMove;
            }

            // Check if player has the indicator tile
            bool hasIndicator = false;
            foreach (OkeyPiece tile in player.Tiles)
            {
                if (tile.Number == action.TileData.Number && tile.Color == action.TileData.Color)
                {
                    hasIndicator = true;
                    break;
                }
            }

            if (!hasIndicator)
            {
                return ValidationResult.InvalidMove;
            }

            return ValidationResult.Valid;
        }

        private ValidationResult ValidateDeclareWinAction(PlayerAction action, GameState gameState)
        {
            if (action == null || gameState == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if player exists
            Player player = gameState.GetPlayerById(action.PlayerId);
            if (player == null)
            {
                return ValidationResult.Invalid;
            }

            // Check if player has a valid winning hand
            if (!HasValidWinningHand(player, gameState.IndicatorTile))
            {
                return ValidationResult.InvalidMove;
            }

            return ValidationResult.Valid;
        }

        private bool IsPlayersTurn(int playerId, GameState gameState)
        {
            if (gameState == null)
            {
                return false;
            }

            Player currentPlayer = gameState.CurrentPlayer;
            return currentPlayer != null && currentPlayer.Id == playerId;
        }

        private Player GetPlayerById(int playerId, GameState gameState)
        {
            if (gameState.Players != null)
            {
                for (int index = 0; index < gameState.Players.Count; index++)
                {
                    Player player = gameState.Players[index];
                    if (player.Id == playerId)
                    {
                        return player;
                    }
                }
            }
            return null;
        }

        private bool IsPositionOccupied(GridPosition position, GameState gameState)
        {
            // This would need to check the actual game board state
            // For now, returning false as a placeholder
            return false;
        }

        private bool HasValidSetsAndSequences(List<TileData> tiles, TileData jokerTile)
        {
            // This is a simplified implementation
            // A full implementation would need to try all possible combinations
            // of sets and sequences to see if they use all tiles exactly once
            
            // For now, we'll use the game rules service to check basic validity
            return _gameRulesService.ValidateWinCondition(
                CreateTempPlayerWithTiles(tiles), 
                WinType.Normal
            );
        }

        private Player CreateTempPlayerWithTiles(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return null;
            }

            Player tempPlayer = new Player(0, "TempPlayer", Runtime.Domain.Enums.PlayerType.Human);
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tileData = tiles[index];
                OkeyPiece piece = new OkeyPiece(
                    tileData.Number, 
                    tileData.Color, 
                    tileData.PieceType, 
                    index
                );
                tempPlayer.AddTile(piece);
            }

            return tempPlayer;
        }

        private bool IsJoker(TileData tile, TileData jokerTile)
        {
            if (tile == null || jokerTile == null)
            {
                return false;
            }

            // Check if tile is a joker (same number and color as joker tile)
            if (tile.PieceType == OkeyPieceType.Joker)
            {
                return true;
            }

            // Check if tile is a false joker
            if (tile.PieceType == OkeyPieceType.FalseJoker)
            {
                return true;
            }

            // Calculate the joker value (indicator + 1, same color)
            int jokerNumber = (jokerTile.Number % 13) + 1;
            if (jokerNumber == 14)
            {
                jokerNumber = 1;
            }

            return tile.Number == jokerNumber && tile.Color == jokerTile.Color;
        }

        private int GetEffectiveNumber(TileData tile, TileData jokerTile)
        {
            if (tile == null)
            {
                return 0;
            }

            if (IsJoker(tile, jokerTile))
            {
                // For jokers, we need to determine their effective number based on context
                // This is a placeholder - in a real implementation, this would be more complex
                return 0;
            }

            return tile.Number;
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
    }
}
