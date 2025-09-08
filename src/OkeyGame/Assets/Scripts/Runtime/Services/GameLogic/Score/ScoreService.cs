using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using UnityEngine;

namespace Runtime.Services.GameLogic.Score
{
    public sealed class ScoreService : IScoreService
    {
        private readonly Dictionary<int, int> _playerScores;
        private readonly Dictionary<int, ScoreBreakdown> _scoreBreakdowns;
        private readonly Dictionary<int, List<ScoreEntry>> _scoreHistory;
        
        private const int ELIMINATION_THRESHOLD = -100;
        private const int WIN_SCORE_NORMAL = 10;
        private const int WIN_SCORE_PAIRS = 20;
        private const int WIN_SCORE_OKEY = 30;

        public event Action<Player, int> OnScoreChanged;
        public event Action<Player> OnPlayerEliminated;
        public event Action<Player> OnGameWinner;

        public ScoreService()
        {
            _playerScores = new Dictionary<int, int>();
            _scoreBreakdowns = new Dictionary<int, ScoreBreakdown>();
            _scoreHistory = new Dictionary<int, List<ScoreEntry>>();
        }

        public async UniTask InitializeAsync()
        {
            _playerScores.Clear();
            _scoreBreakdowns.Clear();
            _scoreHistory.Clear();
            
            Debug.Log("[ScoreService] Initialized");
            await UniTask.Yield();
        }

        public async UniTask<int> CalculateWinScoreAsync(WinType winType)
        {
            int baseScore = GetBaseWinScore(winType);
            
            // Additional calculations can be added here for bonuses
            await UniTask.Yield();
            return baseScore;
        }

        public async UniTask<int> CalculatePlayerPenaltyAsync(Player player, WinType winnerWinType)
        {
            if (player == null)
            {
                return 0;
            }

            List<OkeyPiece> remainingTiles = player.GetTiles();
            if (remainingTiles == null || remainingTiles.Count == 0)
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
            int finalPenalty = Mathf.RoundToInt(penalty * multiplier);
            
            await UniTask.Yield();
            return finalPenalty;
        }

        public async UniTask ApplyWinScoreAsync(Player winner, WinType winType, List<Player> allPlayers)
        {
            if (winner == null || allPlayers == null)
            {
                return;
            }

            try
            {
                // Calculate and apply win score
                int winScore = await CalculateWinScoreAsync(winType);
                AddPlayerScore(winner.Id, winScore);
                
                // Update score breakdown
                UpdateScoreBreakdown(winner.Id, winner.Name, winScore, winType, true);
                
                OnScoreChanged?.Invoke(winner, winScore);
                
                // Calculate and apply penalties for other players
                for (int index = 0; index < allPlayers.Count; index++)
                {
                    Player player = allPlayers[index];
                    
                    if (player.Id == winner.Id)
                    {
                        continue; // Skip the winner
                    }
                    
                    int penalty = await CalculatePlayerPenaltyAsync(player, winType);
                    SubtractPlayerScore(player.Id, penalty);
                    
                    // Update score breakdown
                    UpdateScoreBreakdown(player.Id, player.Name, -penalty, winType, false);
                    
                    OnScoreChanged?.Invoke(player, -penalty);
                    
                    // Check for elimination
                    if (IsPlayerEliminated(player))
                    {
                        OnPlayerEliminated?.Invoke(player);
                    }
                }
                
                // Check if winner should be declared game winner
                if (ShouldDeclareGameWinner(winner, allPlayers))
                {
                    OnGameWinner?.Invoke(winner);
                }
                
                Debug.Log($"[ScoreService] Applied win score for {winner.Name} with {winType} win");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ScoreService] Failed to apply win score: {exception.Message}");
            }
        }

        public async UniTask<bool> CheckGameEndConditionAsync(List<Player> players)
        {
            if (players == null || players.Count == 0)
            {
                return false;
            }

            int activePlayers = 0;
            Player potentialWinner = null;
            
            for (int index = 0; index < players.Count; index++)
            {
                Player player = players[index];
                
                if (!IsPlayerEliminated(player))
                {
                    activePlayers++;
                    potentialWinner = player;
                }
            }
            
            // Game ends if only one player remains or if a player reaches a high score
            bool gameEnded = activePlayers <= 1 || HasPlayerReachedWinThreshold(players);
            
            if (gameEnded && potentialWinner != null)
            {
                OnGameWinner?.Invoke(potentialWinner);
            }
            
            await UniTask.Yield();
            return gameEnded;
        }

        public int GetPlayerScore(int playerId)
        {
            if (_playerScores.TryGetValue(playerId, out int score))
            {
                return score;
            }
            return 0;
        }

        public void SetPlayerScore(int playerId, int score)
        {
            _playerScores[playerId] = score;
            AddScoreHistoryEntry(playerId, score - GetPlayerScore(playerId), score);
        }

        public void AddPlayerScore(int playerId, int points)
        {
            if (!_playerScores.ContainsKey(playerId))
            {
                _playerScores[playerId] = 0;
            }
            
            int oldScore = _playerScores[playerId];
            _playerScores[playerId] += points;
            
            AddScoreHistoryEntry(playerId, points, _playerScores[playerId]);
        }

        public void SubtractPlayerScore(int playerId, int points)
        {
            if (!_playerScores.ContainsKey(playerId))
            {
                _playerScores[playerId] = 0;
            }
            
            int oldScore = _playerScores[playerId];
            _playerScores[playerId] -= points;
            
            AddScoreHistoryEntry(playerId, -points, _playerScores[playerId]);
        }

        public Player GetWinner(List<Player> players)
        {
            if (players == null || players.Count == 0)
            {
                return null;
            }

            Player winner = null;
            int highestScore = int.MinValue;
            
            for (int index = 0; index < players.Count; index++)
            {
                Player player = players[index];
                int playerScore = GetPlayerScore(player.Id);
                
                if (playerScore > highestScore && !IsPlayerEliminated(player))
                {
                    highestScore = playerScore;
                    winner = player;
                }
            }
            
            return winner;
        }

        public bool IsPlayerEliminated(Player player)
        {
            if (player == null)
            {
                return true;
            }
            
            int score = GetPlayerScore(player.Id);
            return score <= ELIMINATION_THRESHOLD;
        }

        public ScoreBreakdown GetScoreBreakdown(int playerId)
        {
            if (_scoreBreakdowns.TryGetValue(playerId, out ScoreBreakdown breakdown))
            {
                return breakdown;
            }
            return null;
        }

        public List<ScoreEntry> GetScoreHistory(int playerId)
        {
            if (_scoreHistory.TryGetValue(playerId, out List<ScoreEntry> history))
            {
                return new List<ScoreEntry>(history);
            }
            return new List<ScoreEntry>();
        }

        public void ResetPlayerScore(int playerId)
        {
            _playerScores[playerId] = 0;
            
            if (_scoreBreakdowns.ContainsKey(playerId))
            {
                _scoreBreakdowns[playerId].Reset();
            }
            
            if (_scoreHistory.ContainsKey(playerId))
            {
                _scoreHistory[playerId].Clear();
            }
        }

        public void ResetAllScores()
        {
            _playerScores.Clear();
            _scoreBreakdowns.Clear();
            _scoreHistory.Clear();
            
            Debug.Log("[ScoreService] All scores reset");
        }

        public List<Player> GetRankedPlayers(List<Player> players)
        {
            if (players == null || players.Count == 0)
            {
                return new List<Player>();
            }
            
            List<Player> rankedPlayers = new List<Player>(players);
            
            rankedPlayers.Sort((a, b) => 
            {
                int scoreA = GetPlayerScore(a.Id);
                int scoreB = GetPlayerScore(b.Id);
                return scoreB.CompareTo(scoreA); // Descending order
            });
            
            return rankedPlayers;
        }

        private int GetBaseWinScore(WinType winType)
        {
            switch (winType)
            {
                case WinType.Normal:
                    return WIN_SCORE_NORMAL;
                case WinType.Pairs:
                    return WIN_SCORE_PAIRS;
                case WinType.Okey:
                    return WIN_SCORE_OKEY;
                default:
                    return 0;
            }
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

        private void UpdateScoreBreakdown(int playerId, string playerName, int scoreChange, WinType winType, bool isWin)
        {
            if (!_scoreBreakdowns.ContainsKey(playerId))
            {
                _scoreBreakdowns[playerId] = new ScoreBreakdown(playerId, playerName);
            }

            ScoreBreakdown breakdown = _scoreBreakdowns[playerId];
            breakdown.AddScoreChange(scoreChange);
            
            if (isWin)
            {
                breakdown.AddWin(winType);
            }
            else
            {
                breakdown.AddLoss();
            }
        }

        private void AddScoreHistoryEntry(int playerId, int scoreChange, int totalScore)
        {
            if (!_scoreHistory.ContainsKey(playerId))
            {
                _scoreHistory[playerId] = new List<ScoreEntry>();
            }

            ScoreEntry entry = new ScoreEntry(scoreChange, totalScore, DateTime.Now);
            _scoreHistory[playerId].Add(entry);
            
            // Keep only last 50 entries to prevent memory bloat
            if (_scoreHistory[playerId].Count > 50)
            {
                _scoreHistory[playerId].RemoveAt(0);
            }
        }

        private bool ShouldDeclareGameWinner(Player winner, List<Player> allPlayers)
        {
            // Game winner conditions can be customized
            int winnerScore = GetPlayerScore(winner.Id);
            
            // Win if score is very high or if all other players are eliminated
            if (winnerScore >= 100)
            {
                return true;
            }
            
            int activePlayers = 0;
            for (int index = 0; index < allPlayers.Count; index++)
            {
                if (!IsPlayerEliminated(allPlayers[index]))
                {
                    activePlayers++;
                }
            }
            
            return activePlayers <= 1;
        }

        private bool HasPlayerReachedWinThreshold(List<Player> players)
        {
            for (int index = 0; index < players.Count; index++)
            {
                Player player = players[index];
                int score = GetPlayerScore(player.Id);
                
                if (score >= 100) // Win threshold
                {
                    return true;
                }
            }
            
            return false;
        }
    }

    [Serializable]
    public sealed class ScoreBreakdown
    {
        public readonly int PlayerId;
        public readonly string PlayerName;
        
        private int _totalScore;
        private int _winCount;
        private int _lossCount;
        private int _normalWins;
        private int _pairsWins;
        private int _okeyWins;

        public int TotalScore => _totalScore;
        public int WinCount => _winCount;
        public int LossCount => _lossCount;
        public int NormalWins => _normalWins;
        public int PairsWins => _pairsWins;
        public int OkeyWins => _okeyWins;
        public float WinRate => (_winCount + _lossCount) > 0 ? (float)_winCount / (_winCount + _lossCount) : 0f;

        public ScoreBreakdown(int playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Reset();
        }

        public void AddScoreChange(int scoreChange)
        {
            _totalScore += scoreChange;
        }

        public void AddWin(WinType winType)
        {
            _winCount++;
            
            switch (winType)
            {
                case WinType.Normal:
                    _normalWins++;
                    break;
                case WinType.Pairs:
                    _pairsWins++;
                    break;
                case WinType.Okey:
                    _okeyWins++;
                    break;
            }
        }

        public void AddLoss()
        {
            _lossCount++;
        }

        public void Reset()
        {
            _totalScore = 0;
            _winCount = 0;
            _lossCount = 0;
            _normalWins = 0;
            _pairsWins = 0;
            _okeyWins = 0;
        }
    }

    [Serializable]
    public sealed class ScoreEntry
    {
        public readonly int ScoreChange;
        public readonly int TotalScore;
        public readonly DateTime Timestamp;

        public ScoreEntry(int scoreChange, int totalScore, DateTime timestamp)
        {
            ScoreChange = scoreChange;
            TotalScore = totalScore;
            Timestamp = timestamp;
        }
    }
}
