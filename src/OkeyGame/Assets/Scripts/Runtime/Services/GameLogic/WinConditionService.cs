using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic.Tiles;
using Zenject;

namespace Runtime.Services.GameLogic
{
    public sealed class WinConditionService : IWinConditionService, IInitializableService, IDisposableService
    {
        private readonly ITileService _tileService;
        private readonly IGameRulesService _gameRulesService;

        public event Action<Player, WinType> OnWinConditionDetected;
        public event Action<Player, List<List<OkeyPiece>>> OnValidSetsFound;
        public event Action<Player> OnPairsWinDetected;

        [Inject]
        public WinConditionService(
            ITileService tileService,
            IGameRulesService gameRulesService)
        {
            _tileService = tileService;
            _gameRulesService = gameRulesService;
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.Yield();
            return true;
        }

        public async UniTask<WinType?> CheckPlayerWinConditionAsync(Player player)
        {
            if (player == null || player.TileCount == 0)
            {
                return null;
            }

            OkeyPiece indicatorTile = await _tileService.GetIndicatorTileAsync();
            TileData jokerTileData = default;
            
            if (indicatorTile != null)
            {
                jokerTileData = _tileService.CalculateJokerTile(indicatorTile.TileData);
            }

            WinType? winType = await CheckAllWinConditionsAsync(player, jokerTileData);
            
            if (winType.HasValue)
            {
                OnWinConditionDetected?.Invoke(player, winType.Value);
            }

            return winType;
        }

        public async UniTask<bool> ValidateWinDeclarationAsync(Player player, WinType declaredWinType)
        {
            if (player == null)
            {
                return false;
            }

            WinType? actualWinType = await CheckPlayerWinConditionAsync(player);
            return actualWinType.HasValue && actualWinType.Value == declaredWinType;
        }

        public async UniTask<List<List<OkeyPiece>>> FindValidSetsAsync(Player player)
        {
            List<List<OkeyPiece>> validSets = new List<List<OkeyPiece>>();

            if (player == null || player.TileCount == 0)
            {
                return validSets;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            validSets = await AnalyzeTileArrangementsAsync(playerTiles);

            if (validSets.Count > 0)
            {
                OnValidSetsFound?.Invoke(player, validSets);
            }

            return validSets;
        }

        public async UniTask<bool> CheckPairsWinAsync(Player player)
        {
            if (player == null || player.TileCount != 14)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            bool hasPairsWin = await ValidatePairsArrangementAsync(playerTiles);

            if (hasPairsWin)
            {
                OnPairsWinDetected?.Invoke(player);
            }

            return hasPairsWin;
        }

        public async UniTask<bool> CheckNormalWinAsync(Player player)
        {
            if (player == null || player.TileCount != 14)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            return await ValidateNormalWinArrangementAsync(playerTiles);
        }

        public async UniTask<bool> CheckOkeyWinAsync(Player player, OkeyPiece lastDiscardedTile)
        {
            if (lastDiscardedTile == null)
            {
                return false;
            }

            bool hasNormalWin = await CheckNormalWinAsync(player);
            if (!hasNormalWin)
            {
                return false;
            }

            OkeyPiece indicatorTile = await _tileService.GetIndicatorTileAsync();
            if (indicatorTile == null)
            {
                return false;
            }

            TileData jokerTileData = _tileService.CalculateJokerTile(indicatorTile.TileData);
            return _tileService.IsTileJoker(lastDiscardedTile.TileData, jokerTileData);
        }

        private async UniTask<WinType?> CheckAllWinConditionsAsync(Player player, TileData jokerTileData)
        {
            if (await CheckPairsWinAsync(player))
            {
                return WinType.Pairs;
            }

            if (await CheckNormalWinAsync(player))
            {
                return WinType.Normal;
            }

            await UniTask.Yield();
            return null;
        }

        private async UniTask<List<List<OkeyPiece>>> AnalyzeTileArrangementsAsync(List<OkeyPiece> tiles)
        {
            List<List<OkeyPiece>> validArrangements = new List<List<OkeyPiece>>();

            if (tiles == null || tiles.Count == 0)
            {
                return validArrangements;
            }

            List<List<OkeyPiece>> groups = FindValidGroups(tiles);
            List<List<OkeyPiece>> sequences = FindValidSequences(tiles);

            validArrangements.AddRange(groups);
            validArrangements.AddRange(sequences);

            await UniTask.Yield();
            return validArrangements;
        }

        private List<List<OkeyPiece>> FindValidGroups(List<OkeyPiece> tiles)
        {
            List<List<OkeyPiece>> groups = new List<List<OkeyPiece>>();
            Dictionary<int, List<OkeyPiece>> numberGroups = new Dictionary<int, List<OkeyPiece>>();

            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                int number = tile.TileData.Number;

                if (!numberGroups.ContainsKey(number))
                {
                    numberGroups[number] = new List<OkeyPiece>();
                }
                numberGroups[number].Add(tile);
            }

            foreach (KeyValuePair<int, List<OkeyPiece>> numberGroup in numberGroups)
            {
                List<OkeyPiece> tilesOfSameNumber = numberGroup.Value;
                if (tilesOfSameNumber.Count >= 3)
                {
                    List<OkeyPiece> validGroup = GetValidColorGroup(tilesOfSameNumber);
                    if (validGroup.Count >= 3)
                    {
                        groups.Add(validGroup);
                    }
                }
            }

            return groups;
        }

        private List<OkeyPiece> GetValidColorGroup(List<OkeyPiece> tilesOfSameNumber)
        {
            List<OkeyPiece> validGroup = new List<OkeyPiece>();
            HashSet<OkeyColor> usedColors = new HashSet<OkeyColor>();

            for (int tileIndex = 0; tileIndex < tilesOfSameNumber.Count; tileIndex++)
            {
                OkeyPiece tile = tilesOfSameNumber[tileIndex];
                OkeyColor color = tile.TileData.Color;

                if (!usedColors.Contains(color))
                {
                    usedColors.Add(color);
                    validGroup.Add(tile);
                }
            }

            return validGroup;
        }

        private List<List<OkeyPiece>> FindValidSequences(List<OkeyPiece> tiles)
        {
            List<List<OkeyPiece>> sequences = new List<List<OkeyPiece>>();
            Dictionary<OkeyColor, List<OkeyPiece>> colorGroups = new Dictionary<OkeyColor, List<OkeyPiece>>();

            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                OkeyColor color = tile.TileData.Color;

                if (!colorGroups.ContainsKey(color))
                {
                    colorGroups[color] = new List<OkeyPiece>();
                }
                
                colorGroups[color].Add(tile);
            }

            foreach (KeyValuePair<OkeyColor, List<OkeyPiece>> colorGroup in colorGroups)
            {
                List<OkeyPiece> tilesOfSameColor = colorGroup.Value;
                tilesOfSameColor.Sort((a, b) => a.TileData.Number.CompareTo(b.TileData.Number));
                
                // Find sequences within this color group
                List<List<OkeyPiece>> colorSequences = FindSequencesInColorGroup(tilesOfSameColor);
                sequences.AddRange(colorSequences);
            }

            return sequences;
        }

        private List<List<OkeyPiece>> FindSequencesInColorGroup(List<OkeyPiece> sortedTiles)
        {
            List<List<OkeyPiece>> sequences = new List<List<OkeyPiece>>();
            List<OkeyPiece> currentSequence = new List<OkeyPiece>();

            for (int tileIndex = 0; tileIndex < sortedTiles.Count; tileIndex++)
            {
                OkeyPiece tile = sortedTiles[tileIndex];

                if (currentSequence.Count == 0)
                {
                    currentSequence.Add(tile);
                }
                else
                {
                    OkeyPiece lastTile = currentSequence[currentSequence.Count - 1];
                    int expectedNumber = lastTile.TileData.Number + 1;

                    if (expectedNumber > 13)
                    {
                        expectedNumber = 1;
                    }

                    if (tile.TileData.Number == expectedNumber)
                    {
                        currentSequence.Add(tile);
                    }
                    else
                    {
                        if (currentSequence.Count >= 3)
                        {
                            sequences.Add(new List<OkeyPiece>(currentSequence));
                        }
                        currentSequence.Clear();
                        currentSequence.Add(tile);
                    }
                }
            }

            if (currentSequence.Count >= 3)
            {
                sequences.Add(currentSequence);
            }

            return sequences;
        }

        private async UniTask<bool> ValidatePairsArrangementAsync(List<OkeyPiece> tiles)
        {
            if (tiles.Count != 14)
            {
                return false;
            }

            Dictionary<string, int> tileCount = new Dictionary<string, int>();

            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                string tileKey = $"{tile.TileData.Number}-{tile.TileData.Color}";

                if (tileCount.ContainsKey(tileKey))
                {
                    tileCount[tileKey]++;
                }
                else
                {
                    tileCount[tileKey] = 1;
                }
            }

            int pairCount = 0;
            foreach (KeyValuePair<string, int> count in tileCount)
            {
                if (count.Value == 2)
                {
                    pairCount++;
                }
                else if (count.Value != 0)
                {
                    return false;
                }
            }

            await UniTask.Yield();
            return pairCount == 7;
        }

        private async UniTask<bool> ValidateNormalWinArrangementAsync(List<OkeyPiece> tiles)
        {
            if (tiles.Count != 14)
            {
                return false;
            }

            List<List<OkeyPiece>> validSets = await AnalyzeTileArrangementsAsync(tiles);
            
            int totalTilesInSets = 0;
            for (int setIndex = 0; setIndex < validSets.Count; setIndex++)
            {
                List<OkeyPiece> set = validSets[setIndex];
                if (set.Count >= 3)
                {
                    totalTilesInSets += set.Count;
                }
            }

            return totalTilesInSets == 14;
        }

        public async UniTask InitializeAsync()
        {
            // Initialize any required resources or state
            await UniTask.Yield();
        }

        public void Dispose()
        {
        }
    }
}
