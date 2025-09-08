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
    public sealed class ScoreCalculationService : IScoreCalculationService, IInitializableService, IDisposableService
    {
        private const int BASE_WIN_SCORE = 2;
        private const int PAIRS_WIN_MULTIPLIER = 4;
        private const int OKEY_WIN_MULTIPLIER = 8;
        private const int INDICATOR_BONUS = 1;
        private const int FALSE_JOKER_PENALTY = 2;
        private const int REMAINING_TILE_PENALTY = 1;

        private readonly ITileService _tileService;
        private readonly IGameRulesService _gameRulesService;
        private readonly IWinConditionService _winConditionService;

        public event Action<Player, int, ScoreBreakdown> OnScoreCalculated;
        public event Action<List<Player>, Dictionary<Player, int>> OnRoundScoresCalculated;

        [Inject]
        public ScoreCalculationService(
            ITileService tileService,
            IGameRulesService gameRulesService,
            IWinConditionService winConditionService)
        {
            _tileService = tileService;
            _gameRulesService = gameRulesService;
            _winConditionService = winConditionService;
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.Yield();
            return true;
        }

        public async UniTask<ScoreBreakdown> CalculateWinnerScoreAsync(Player winner, WinType winType, List<Player> allPlayers)
        {
            if (winner == null || allPlayers == null || allPlayers.Count == 0)
            {
                return new ScoreBreakdown();
            }

            ScoreBreakdown breakdown = new ScoreBreakdown
            {
                Player = winner,
                WinType = winType,
                BaseScore = BASE_WIN_SCORE
            };

            breakdown.WinTypeMultiplier = GetWinTypeMultiplier(winType);
            breakdown.IndicatorBonus = await CalculateIndicatorBonusAsync(winner);
            breakdown.OpponentPenalties = await CalculateOpponentPenaltiesAsync(winner, allPlayers);

            int totalScore = (breakdown.BaseScore * breakdown.WinTypeMultiplier) + 
                           breakdown.IndicatorBonus + 
                           breakdown.OpponentPenalties;

            breakdown.TotalScore = totalScore;

            OnScoreCalculated?.Invoke(winner, totalScore, breakdown);
            return breakdown;
        }

        public async UniTask<Dictionary<Player, int>> CalculateRoundScoresAsync(List<Player> players, Player winner, WinType winType)
        {
            Dictionary<Player, int> roundScores = new Dictionary<Player, int>();

            if (players == null || winner == null)
            {
                return roundScores;
            }

            ScoreBreakdown winnerBreakdown = await CalculateWinnerScoreAsync(winner, winType, players);
            roundScores[winner] = winnerBreakdown.TotalScore;

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                Player player = players[playerIndex];
                if (player != winner)
                {
                    int penalty = await CalculatePlayerPenaltyAsync(player, winner, winType);
                    roundScores[player] = -penalty;
                }
            }

            OnRoundScoresCalculated?.Invoke(players, roundScores);
            return roundScores;
        }

        public async UniTask<int> CalculatePlayerPenaltyAsync(Player player, Player winner, WinType winType)
        {
            if (player == null || winner == null)
            {
                return 0;
            }

            int penalty = 0;
            List<OkeyPiece> remainingTiles = player.GetTiles();

            penalty += CalculateRemainingTilesPenalty(remainingTiles);
            penalty += await CalculateFalseJokerPenaltyAsync(remainingTiles);
            penalty = ApplyWinTypeMultiplier(penalty, winType);

            return penalty;
        }

        public async UniTask<int> CalculateIndicatorBonusAsync(Player player)
        {
            if (player == null)
            {
                return 0;
            }

            OkeyPiece indicatorTile = await _tileService.GetIndicatorTileAsync();
            if (indicatorTile == null)
            {
                return 0;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            int indicatorCount = 0;

            for (int tileIndex = 0; tileIndex < playerTiles.Count; tileIndex++)
            {
                OkeyPiece tile = playerTiles[tileIndex];
                if (IsSameTile(tile.TileData, indicatorTile.TileData))
                {
                    indicatorCount++;
                }
            }

            return indicatorCount * INDICATOR_BONUS;
        }

        public int CalculateRemainingTilesPenalty(List<OkeyPiece> remainingTiles)
        {
            if (remainingTiles == null)
            {
                return 0;
            }

            return remainingTiles.Count * REMAINING_TILE_PENALTY;
        }

        public async UniTask<int> CalculateFalseJokerPenaltyAsync(List<OkeyPiece> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return 0;
            }

            int falseJokerCount = 0;

            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                OkeyPiece tile = tiles[tileIndex];
                if (await IsFalseJokerAsync(tile))
                {
                    falseJokerCount++;
                }
            }

            return falseJokerCount * FALSE_JOKER_PENALTY;
        }

        public async UniTask<bool> ShouldPlayerPayDoubleAsync(Player player, WinType winType)
        {
            if (player == null)
            {
                return false;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            
            bool hasFalseJoker = false;
            for (int tileIndex = 0; tileIndex < playerTiles.Count; tileIndex++)
            {
                if (await IsFalseJokerAsync(playerTiles[tileIndex]))
                {
                    hasFalseJoker = true;
                    break;
                }
            }

            return hasFalseJoker && (winType == WinType.Pairs || winType == WinType.Okey);
        }

        public int GetWinTypeMultiplier(WinType winType)
        {
            return winType switch
            {
                WinType.Normal => 1,
                WinType.Pairs => PAIRS_WIN_MULTIPLIER,
                WinType.Okey => OKEY_WIN_MULTIPLIER,
                _ => 1
            };
        }

        public async UniTask<ScoreBreakdown> GetDetailedScoreBreakdownAsync(Player player, WinType winType, List<Player> allPlayers)
        {
            ScoreBreakdown breakdown = new ScoreBreakdown
            {
                Player = player,
                WinType = winType,
                BaseScore = BASE_WIN_SCORE,
                WinTypeMultiplier = GetWinTypeMultiplier(winType)
            };

            breakdown.IndicatorBonus = await CalculateIndicatorBonusAsync(player);
            breakdown.OpponentPenalties = await CalculateOpponentPenaltiesAsync(player, allPlayers);

            List<OkeyPiece> playerTiles = player.GetTiles();
            breakdown.RemainingTilesPenalty = CalculateRemainingTilesPenalty(playerTiles);
            breakdown.FalseJokerPenalty = await CalculateFalseJokerPenaltyAsync(playerTiles);

            int baseWinScore = breakdown.BaseScore * breakdown.WinTypeMultiplier;
            breakdown.TotalScore = baseWinScore + breakdown.IndicatorBonus + breakdown.OpponentPenalties - 
                                 breakdown.RemainingTilesPenalty - breakdown.FalseJokerPenalty;

            return breakdown;
        }

        private async UniTask<int> CalculateOpponentPenaltiesAsync(Player winner, List<Player> allPlayers)
        {
            int totalPenalties = 0;

            for (int playerIndex = 0; playerIndex < allPlayers.Count; playerIndex++)
            {
                Player player = allPlayers[playerIndex];
                if (player != winner)
                {
                    List<OkeyPiece> opponentTiles = player.GetTiles();
                    int opponentPenalty = CalculateRemainingTilesPenalty(opponentTiles);
                    opponentPenalty += await CalculateFalseJokerPenaltyAsync(opponentTiles);
                    totalPenalties += opponentPenalty;
                }
            }

            return totalPenalties;
        }

        private int ApplyWinTypeMultiplier(int basePenalty, WinType winType)
        {
            int multiplier = GetWinTypeMultiplier(winType);
            return basePenalty * multiplier;
        }

        private async UniTask<bool> IsFalseJokerAsync(OkeyPiece tile)
        {
            if (tile == null)
            {
                return false;
            }

            await UniTask.Yield();
            return tile.TileData.PieceType == OkeyPieceType.FalseJoker;
        }

        private bool IsSameTile(TileData tile1, TileData tile2)
        {
            if (tile1 == null || tile2 == null)
            {
                return false;
            }

            return tile1.Number == tile2.Number && tile1.Color == tile2.Color;
        }

        public async UniTask InitializeAsync()
        {
            // Initialize any required resources or state
            // Note: There are no required resources or state to initialize in this class
            await UniTask.Yield();
        }

        public void Dispose()
        {
        }
    }

    public sealed class ScoreBreakdown
    {
        public Player Player { get; set; }
        public WinType WinType { get; set; }
        public int BaseScore { get; set; }
        public int WinTypeMultiplier { get; set; }
        public int IndicatorBonus { get; set; }
        public int OpponentPenalties { get; set; }
        public int RemainingTilesPenalty { get; set; }
        public int FalseJokerPenalty { get; set; }
        public int TotalScore { get; set; }

        public ScoreBreakdown()
        {
            BaseScore = 0;
            WinTypeMultiplier = 1;
            IndicatorBonus = 0;
            OpponentPenalties = 0;
            RemainingTilesPenalty = 0;
            FalseJokerPenalty = 0;
            TotalScore = 0;
        }
    }
}
