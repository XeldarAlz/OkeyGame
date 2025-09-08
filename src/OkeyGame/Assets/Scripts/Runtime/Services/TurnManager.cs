using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Utilities;
using Runtime.Domain.Models;
using UnityEngine;
using Zenject;

namespace Runtime.Services
{
    public sealed class TurnManager : ITurnManager
    {
        private readonly ITimeProvider _timeProvider;
       
        private List<Player> _turnOrder;
        
        private int _currentPlayerIndex;
        
        private Player _currentPlayer;
        
        private bool _isTurnActive;
        
        private float _turnStartTime;
        private float _turnTimeLimit;

        public Player CurrentPlayer => _currentPlayer;
        public int CurrentPlayerIndex => _currentPlayerIndex;

        public event Action<Player> OnTurnChanged;
        public event Action<Player> OnTurnStarted;
        public event Action<Player> OnTurnEnded;

        [Inject]
        public TurnManager(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _turnOrder = new List<Player>();
            _currentPlayerIndex = -1;
            _isTurnActive = false;
            _turnTimeLimit = 60.0f; // Default 60 seconds per turn
        }

        public async UniTask InitializeAsync()
        {
            _turnOrder.Clear();
            _currentPlayerIndex = -1;
            _currentPlayer = null;
            _isTurnActive = false;
            
            Debug.Log("[TurnManager] Initialized");
            await UniTask.Yield();
        }

        public bool IsPlayerTurn(int playerId)
        {
            return _currentPlayer != null && _currentPlayer.Id == playerId && _isTurnActive;
        }
        
        public async UniTask<bool> StartTurnAsync(Player player)
        {
            if (player == null)
            {
                Debug.LogWarning("[TurnManager] Cannot start turn for null player");
                return false;
            }

            if (_isTurnActive)
            {
                Debug.LogWarning("[TurnManager] Turn is already active");
                return false;
            }

            try
            {
                _currentPlayer = player;
                _isTurnActive = true;
                _turnStartTime = _timeProvider.Time;
                
                OnTurnStarted?.Invoke(_currentPlayer);
                
                Debug.Log($"[TurnManager] Turn started for player: {player.Name}");
                await UniTask.Yield();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TurnManager] Failed to start turn: {exception.Message}");
                return false;
            }
        }

        public async UniTask<bool> EndTurnAsync()
        {
            if (!_isTurnActive || _currentPlayer == null)
            {
                Debug.LogWarning("[TurnManager] No active turn to end");
                return false;
            }

            try
            {
                Player endingPlayer = _currentPlayer;
                _isTurnActive = false;
                
                OnTurnEnded?.Invoke(endingPlayer);
                
                Debug.Log($"[TurnManager] Turn ended for player: {endingPlayer.Name}");
                await UniTask.Yield();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TurnManager] Failed to end turn: {exception.Message}");
                return false;
            }
        }

        public async UniTask<bool> NextTurnAsync()
        {
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No players in turn order");
                return false;
            }

            try
            {
                // End current turn if active
                if (_isTurnActive)
                {
                    await EndTurnAsync();
                }

                // Move to next player
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _turnOrder.Count;
                Player nextPlayer = _turnOrder[_currentPlayerIndex];

                // Update current player and notify
                Player previousPlayer = _currentPlayer;
                _currentPlayer = nextPlayer;
                
                OnTurnChanged?.Invoke(_currentPlayer);
                
                // Start the new turn
                await StartTurnAsync(_currentPlayer);
                
                Debug.Log($"[TurnManager] Turn advanced from {previousPlayer?.Name} to {_currentPlayer.Name}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TurnManager] Failed to advance to next turn: {exception.Message}");
                return false;
            }
        }

        public void SetTurnOrder(Player[] players)
        {
            if (players == null)
            {
                Debug.LogWarning("[TurnManager] Cannot set null turn order");
                return;
            }

            _turnOrder.Clear();
            
            for (int index = 0; index < players.Length; index++)
            {
                Player player = players[index];
                if (player != null)
                {
                    _turnOrder.Add(player);
                }
            }

            _currentPlayerIndex = _turnOrder.Count > 0 ? 0 : -1;
            _currentPlayer = _turnOrder.Count > 0 ? _turnOrder[0] : null;
            
            Debug.Log($"[TurnManager] Turn order set with {_turnOrder.Count} players");
        }

        public void SetCurrentPlayer(int playerIndex)
        {
            if (_turnOrder == null || playerIndex < 0 || playerIndex >= _turnOrder.Count)
            {
                Debug.LogWarning($"[TurnManager] Invalid player index: {playerIndex}");
                return;
            }

            _currentPlayerIndex = playerIndex;
            _currentPlayer = _turnOrder[_currentPlayerIndex];
            
            Debug.Log($"[TurnManager] Current player set to: {_currentPlayer.Name}");
        }

        public void ResetTurnOrder()
        {
            _turnOrder.Clear();
            _currentPlayerIndex = -1;
            _currentPlayer = null;
            _isTurnActive = false;
            
            Debug.Log("[TurnManager] Turn order reset");
        }

        public void SetTurnTimeLimit(float timeLimit)
        {
            _turnTimeLimit = Mathf.Max(0f, timeLimit);
            Debug.Log($"[TurnManager] Turn time limit set to: {_turnTimeLimit} seconds");
        }

        public float GetRemainingTurnTime()
        {
            if (!_isTurnActive)
            {
                return 0f;
            }

            float elapsedTime = _timeProvider.Time - _turnStartTime;
            return Mathf.Max(0f, _turnTimeLimit - elapsedTime);
        }

        public bool IsTurnTimeExpired()
        {
            if (!_isTurnActive)
            {
                return false;
            }

            return GetRemainingTurnTime() <= 0f;
        }

        public async UniTask<bool> ForceEndTurnAsync()
        {
            if (!_isTurnActive)
            {
                return false;
            }

            Debug.Log($"[TurnManager] Force ending turn for player: {_currentPlayer?.Name}");
            return await EndTurnAsync();
        }

        public Player GetPlayerByIndex(int index)
        {
            if (_turnOrder == null || index < 0 || index >= _turnOrder.Count)
            {
                return null;
            }

            return _turnOrder[index];
        }

        public int GetPlayerIndex(Player player)
        {
            if (player == null || _turnOrder == null)
            {
                return -1;
            }

            for (int index = 0; index < _turnOrder.Count; index++)
            {
                if (_turnOrder[index].Id == player.Id)
                {
                    return index;
                }
            }

            return -1;
        }

        public List<Player> GetTurnOrder()
        {
            return new List<Player>(_turnOrder);
        }

        public Player GetNextPlayer()
        {
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                return null;
            }

            int nextIndex = (_currentPlayerIndex + 1) % _turnOrder.Count;
            return _turnOrder[nextIndex];
        }

        public Player GetPreviousPlayer()
        {
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                return null;
            }

            int previousIndex = (_currentPlayerIndex - 1 + _turnOrder.Count) % _turnOrder.Count;
            return _turnOrder[previousIndex];
        }

        public bool IsTurnActive()
        {
            return _isTurnActive;
        }

        public void Dispose()
        {
            OnTurnChanged = null;
            OnTurnStarted = null;
            OnTurnEnded = null;
            
            _turnOrder?.Clear();
            _currentPlayer = null;
            _isTurnActive = false;
            
            Debug.Log("[TurnManager] Disposed");
        }
    }
}
