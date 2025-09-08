using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Utilities;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using Zenject;

namespace Runtime.Services.GameLogic.Tiles
{
    public sealed class TileService : ITileService
    {
        private readonly IRandomProvider _randomProvider;
        private readonly List<OkeyPiece> _allTiles;
        
        private const int TILES_PER_COLOR = 13;
        private const int COPIES_PER_TILE = 2;
        private const int FALSE_JOKERS_COUNT = 2;
        private const int TOTAL_TILES = 106; // 104 numbered tiles + 2 false jokers

        private OkeyPiece _indicatorTile;
        private List<OkeyPiece> _tilePile;
        private List<OkeyPiece> _discardPile;

        [Inject]
        public TileService(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
            _allTiles = new List<OkeyPiece>();
            _tilePile = new List<OkeyPiece>();
            _discardPile = new List<OkeyPiece>();
        }

        public async UniTask InitializeAsync()
        {
            _allTiles.Clear();
            _tilePile.Clear();
            _discardPile.Clear();
            Debug.Log("[TileService] Initialized");
            await UniTask.Yield();
        }

        public async UniTask<List<OkeyPiece>> CreateTileSetAsync()
        {
            List<OkeyPiece> tileSet = new List<OkeyPiece>();
            
            // Create numbered tiles (1-13) for each color, with 2 copies each
            OkeyColor[] colors = { OkeyColor.Red, OkeyColor.Yellow, OkeyColor.Black, OkeyColor.Green };
            
            for (int colorIndex = 0; colorIndex < colors.Length; colorIndex++)
            {
                OkeyColor color = colors[colorIndex];
                
                for (int number = 1; number <= TILES_PER_COLOR; number++)
                {
                    for (int copy = 0; copy < COPIES_PER_TILE; copy++)
                    {
                        OkeyPiece tile = new OkeyPiece(number, color, OkeyPieceType.Normal, colorIndex + number + copy);
                        tileSet.Add(tile);
                    }
                }
            }
            
            // Add false jokers
            for (int index = 0; index < FALSE_JOKERS_COUNT; index++)
            {
                OkeyPiece falseJoker = new OkeyPiece(0, OkeyColor.Red, OkeyPieceType.FalseJoker, index);
                tileSet.Add(falseJoker);
            }
            
            await UniTask.Yield();
            return tileSet;
        }

        public async UniTask<List<OkeyPiece>> CreateFullTileSetAsync()
        {
            // Create a complete set of tiles
            List<OkeyPiece> tileSet = await CreateTileSetAsync();
            
            // Initialize the tile pile and discard pile
            _tilePile = new List<OkeyPiece>(tileSet);
            _discardPile = new List<OkeyPiece>();
            
            // Shuffle the tiles
            _tilePile = await ShuffleTilesAsync(_tilePile);
            
            await UniTask.Yield();
            return tileSet;
        }

        public async UniTask<List<OkeyPiece>> ShuffleTilesAsync(List<OkeyPiece> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return new List<OkeyPiece>();
            }
            
            List<OkeyPiece> shuffledTiles = new List<OkeyPiece>(tiles);
            
            // Fisher-Yates shuffle algorithm
            for (int index = shuffledTiles.Count - 1; index > 0; index--)
            {
                int randomIndex = _randomProvider.Range(0, index + 1);
                OkeyPiece temp = shuffledTiles[index];
                shuffledTiles[index] = shuffledTiles[randomIndex];
                shuffledTiles[randomIndex] = temp;
            }
            
            await UniTask.Yield();
            return shuffledTiles;
        }

        public List<OkeyPiece> ShuffleTiles(List<OkeyPiece> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return new List<OkeyPiece>();
            }
            
            List<OkeyPiece> shuffledTiles = new List<OkeyPiece>(tiles);
            
            // Fisher-Yates shuffle algorithm
            for (int index = shuffledTiles.Count - 1; index > 0; index--)
            {
                int randomIndex = _randomProvider.Range(0, index + 1);
                OkeyPiece temp = shuffledTiles[index];
                shuffledTiles[index] = shuffledTiles[randomIndex];
                shuffledTiles[randomIndex] = temp;
            }
            
            return shuffledTiles;
        }

        public async UniTask<TileData> DetermineIndicatorTileAsync(List<OkeyPiece> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                // Default indicator tile if no tiles available
                return new TileData(1, OkeyColor.Red, OkeyPieceType.Normal);
            }

            // In traditional Okey, the indicator tile is typically the 5th tile from the end
            int indicatorIndex = tiles.Count - 5;
            if (indicatorIndex < 0) 
            {
                indicatorIndex = 0;
            }

            OkeyPiece indicatorTile = tiles[indicatorIndex];
            TileData indicatorTileData = new TileData(indicatorTile.Number, indicatorTile.Color, indicatorTile.PieceType);
            
            await UniTask.Yield();
            return indicatorTileData;
        }

        public async UniTask<bool> SetIndicatorTileAsync(OkeyPiece indicatorTile)
        {
            if (indicatorTile == null)
            {
                return false;
            }
            
            _indicatorTile = indicatorTile;
            await UniTask.Yield();
            return true;
        }

        public async UniTask<OkeyPiece> GetIndicatorTileAsync()
        {
            await UniTask.Yield();
            return _indicatorTile;
        }

        public async UniTask<OkeyPiece> DrawTileFromPileAsync()
        {
            if (_tilePile == null || _tilePile.Count == 0)
            {
                Debug.LogWarning("[TileService] Cannot draw tile from empty pile");
                return null;
            }
            
            int lastIndex = _tilePile.Count - 1;
            OkeyPiece drawnTile = _tilePile[lastIndex];
            _tilePile.RemoveAt(lastIndex);
            
            await UniTask.Yield();
            return drawnTile;
        }

        public async UniTask<OkeyPiece> DrawTileFromDiscardAsync()
        {
            if (_discardPile == null || _discardPile.Count == 0)
            {
                Debug.LogWarning("[TileService] Cannot draw tile from empty discard pile");
                return null;
            }
            
            int lastIndex = _discardPile.Count - 1;
            OkeyPiece drawnTile = _discardPile[lastIndex];
            _discardPile.RemoveAt(lastIndex);
            
            await UniTask.Yield();
            return drawnTile;
        }

        public async UniTask<bool> AddTileToDiscardAsync(OkeyPiece tile)
        {
            if (tile == null)
            {
                return false;
            }
            
            if (_discardPile == null)
            {
                _discardPile = new List<OkeyPiece>();
            }
            
            _discardPile.Add(tile);
            await UniTask.Yield();
            return true;
        }

        public TileData CalculateJokerTile(TileData indicatorTile)
        {
            if (indicatorTile == null)
            {
                return new TileData(1, OkeyColor.Red, OkeyPieceType.Normal);
            }
            
            // In Okey, the joker is the next number in the same color
            int jokerNumber = indicatorTile.Number + 1;
            OkeyColor jokerColor = indicatorTile.Color;
            
            // Handle wrap-around: if indicator is 13, joker becomes 1
            if (jokerNumber > TILES_PER_COLOR)
            {
                jokerNumber = 1;
            }
            
            return new TileData(jokerNumber, jokerColor, OkeyPieceType.Normal);
        }

        public bool IsTileJoker(TileData tileData, TileData jokerTile)
        {
            if (tileData == null || jokerTile == null)
            {
                return false;
            }
            
            // False jokers are always jokers
            if (tileData.PieceType == OkeyPieceType.FalseJoker)
            {
                return true;
            }
            
            // Regular tiles that match the joker tile specification
            return tileData.Number == jokerTile.Number && 
                   tileData.Color == jokerTile.Color && 
                   tileData.PieceType == OkeyPieceType.Normal;
        }

        public bool ValidateTileSet(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count < 3 || tiles.Count > 4)
            {
                return false;
            }
            
            // A valid set consists of 3 or 4 tiles with the same number but different colors
            int firstNumber = GetEffectiveNumber(tiles[0]);
            List<OkeyColor> usedColors = new List<OkeyColor>();
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                int effectiveNumber = GetEffectiveNumber(tile);
                
                // All tiles must have the same effective number
                if (effectiveNumber != firstNumber)
                {
                    return false;
                }
                
                // No duplicate colors allowed (except for jokers)
                if (tile.PieceType != OkeyPieceType.FalseJoker)
                {
                    if (usedColors.Contains(tile.Color))
                    {
                        return false;
                    }
                    usedColors.Add(tile.Color);
                }
            }
            
            return true;
        }

        public bool ValidateTileSequence(List<TileData> tiles)
        {
            if (tiles == null || tiles.Count < 3)
            {
                return false;
            }
            
            // A valid sequence consists of consecutive numbers in the same color
            OkeyColor sequenceColor = GetEffectiveColor(tiles[0]);
            List<int> numbers = new List<int>();
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                
                // All tiles must be the same color (except jokers)
                if (tile.PieceType != OkeyPieceType.FalseJoker)
                {
                    if (tile.Color != sequenceColor)
                    {
                        return false;
                    }
                }
                
                numbers.Add(GetEffectiveNumber(tile));
            }
            
            // Sort numbers and check for consecutive sequence
            numbers.Sort();
            
            for (int index = 1; index < numbers.Count; index++)
            {
                if (numbers[index] != numbers[index - 1] + 1)
                {
                    // Check for wrap-around case (13, 1, 2...)
                    if (!(numbers[index - 1] == 13 && numbers[index] == 1))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        public List<List<TileData>> FindAllValidSets(List<TileData> tiles)
        {
            List<List<TileData>> validSets = new List<List<TileData>>();
            
            if (tiles == null || tiles.Count < 3)
            {
                return validSets;
            }
            
            // Group tiles by number
            Dictionary<int, List<TileData>> tilesByNumber = new Dictionary<int, List<TileData>>();
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                int effectiveNumber = GetEffectiveNumber(tile);
                
                if (!tilesByNumber.ContainsKey(effectiveNumber))
                {
                    tilesByNumber[effectiveNumber] = new List<TileData>();
                }
                tilesByNumber[effectiveNumber].Add(tile);
            }
            
            // Find valid sets for each number group
            foreach (KeyValuePair<int, List<TileData>> numberGroup in tilesByNumber)
            {
                List<TileData> tilesForNumber = numberGroup.Value;
                
                if (tilesForNumber.Count >= 3)
                {
                    // Try different combinations of 3 and 4 tiles
                    for (int setSize = 3; setSize <= 4 && setSize <= tilesForNumber.Count; setSize++)
                    {
                        List<List<TileData>> combinations = GetCombinations(tilesForNumber, setSize);
                        
                        for (int combIndex = 0; combIndex < combinations.Count; combIndex++)
                        {
                            List<TileData> combination = combinations[combIndex];
                            if (ValidateTileSet(combination))
                            {
                                validSets.Add(combination);
                            }
                        }
                    }
                }
            }
            
            return validSets;
        }

        public List<List<TileData>> FindAllValidSequences(List<TileData> tiles)
        {
            List<List<TileData>> validSequences = new List<List<TileData>>();
            
            if (tiles == null || tiles.Count < 3)
            {
                return validSequences;
            }
            
            // Group tiles by color
            Dictionary<OkeyColor, List<TileData>> tilesByColor = new Dictionary<OkeyColor, List<TileData>>();
            
            for (int index = 0; index < tiles.Count; index++)
            {
                TileData tile = tiles[index];
                OkeyColor effectiveColor = GetEffectiveColor(tile);
                
                if (!tilesByColor.ContainsKey(effectiveColor))
                {
                    tilesByColor[effectiveColor] = new List<TileData>();
                }
                tilesByColor[effectiveColor].Add(tile);
            }
            
            // Find valid sequences for each color group
            foreach (KeyValuePair<OkeyColor, List<TileData>> colorGroup in tilesByColor)
            {
                List<TileData> tilesForColor = colorGroup.Value;
                
                if (tilesForColor.Count >= 3)
                {
                    // Sort tiles by number
                    tilesForColor.Sort((a, b) => GetEffectiveNumber(a).CompareTo(GetEffectiveNumber(b)));
                    
                    // Find consecutive sequences
                    for (int startIndex = 0; startIndex <= tilesForColor.Count - 3; startIndex++)
                    {
                        for (int length = 3; length <= tilesForColor.Count - startIndex; length++)
                        {
                            List<TileData> sequence = new List<TileData>();
                            
                            for (int seqIndex = startIndex; seqIndex < startIndex + length; seqIndex++)
                            {
                                sequence.Add(tilesForColor[seqIndex]);
                            }
                            
                            if (ValidateTileSequence(sequence))
                            {
                                validSequences.Add(sequence);
                            }
                        }
                    }
                }
            }
            
            return validSequences;
        }

        private int GetEffectiveNumber(TileData tile)
        {
            if (tile == null)
            {
                return 0;
            }
            
            // False jokers can represent any number (context-dependent)
            if (tile.PieceType == OkeyPieceType.FalseJoker)
            {
                return 1; // Default value, should be determined by context
            }
            
            return tile.Number;
        }

        private OkeyColor GetEffectiveColor(TileData tile)
        {
            if (tile == null)
            {
                return OkeyColor.Red;
            }
            
            // False jokers can represent any color (context-dependent)
            if (tile.PieceType == OkeyPieceType.FalseJoker)
            {
                return OkeyColor.Red; // Default value, should be determined by context
            }
            
            return tile.Color;
        }

        private List<List<TileData>> GetCombinations(List<TileData> tiles, int size)
        {
            List<List<TileData>> combinations = new List<List<TileData>>();
            
            if (size == 0 || tiles.Count < size)
            {
                return combinations;
            }
            
            if (size == 1)
            {
                for (int index = 0; index < tiles.Count; index++)
                {
                    List<TileData> singleTile = new List<TileData> { tiles[index] };
                    combinations.Add(singleTile);
                }
                return combinations;
            }
            
            // Recursive combination generation
            for (int index = 0; index <= tiles.Count - size; index++)
            {
                TileData currentTile = tiles[index];
                List<TileData> remainingTiles = new List<TileData>();
                
                for (int remainingIndex = index + 1; remainingIndex < tiles.Count; remainingIndex++)
                {
                    remainingTiles.Add(tiles[remainingIndex]);
                }
                
                List<List<TileData>> subCombinations = GetCombinations(remainingTiles, size - 1);
                
                for (int subIndex = 0; subIndex < subCombinations.Count; subIndex++)
                {
                    List<TileData> combination = new List<TileData> { currentTile };
                    combination.AddRange(subCombinations[subIndex]);
                    combinations.Add(combination);
                }
            }
            
            return combinations;
        }
    }
}
