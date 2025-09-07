using System;
using System.Collections.Generic;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using UnityEngine;
using GameState = Runtime.Domain.Enums.GameState;

namespace Runtime.Domain.Models
{
    [Serializable]
    public sealed class GameState
    {
        [SerializeField] 
        private Enums.GameState _currentState;
        
        [SerializeField] 
        private List<Player> _players;
        
        [SerializeField] 
        private int _currentPlayerIndex;
        [SerializeField] 
        private int _roundNumber;
        
        [SerializeField] 
        private TileData _indicatorTile;
        [SerializeField] 
        private TileData _jokerTile;
        
        [SerializeField] 
        private List<OkeyPiece> _drawPile;
        [SerializeField] 
        private List<OkeyPiece> _discardPile;

        [SerializeField] 
        private bool _gameEnded;

        public Enums.GameState CurrentState => _currentState;
        public IReadOnlyList<Player> Players => _players.AsReadOnly();
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public Player CurrentPlayer => _currentPlayerIndex >= 0 && _currentPlayerIndex < _players.Count ? _players[_currentPlayerIndex] : null;
        public TileData IndicatorTile => _indicatorTile;
        public TileData JokerTile => _jokerTile;
        public IReadOnlyList<OkeyPiece> DrawPile => _drawPile.AsReadOnly();
        public IReadOnlyList<OkeyPiece> DiscardPile => _discardPile.AsReadOnly();
        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int RoundNumber => _roundNumber;
        public bool GameEnded => _gameEnded;
        public int PlayerCount => _players.Count;

        public GameState()
        {
            _currentState = Enums.GameState.None;
            _players = new List<Player>();
            _currentPlayerIndex = -1;
            _drawPile = new List<OkeyPiece>();
            _discardPile = new List<OkeyPiece>();
            _roundNumber = 0;
            _gameEnded = false;
        }

        public void SetState(Runtime.Domain.Enums.GameState newState)
        {
            _currentState = newState;
        }

        public void AddPlayer(Player player)
        {
            if (player != null && !_players.Contains(player))
            {
                _players.Add(player);
            }
        }

        public void RemovePlayer(Player player)
        {
            if (player != null)
            {
                _players.Remove(player);
            }
        }

        public Player GetPlayerById(int playerId)
        {
            for (int index = 0; index < _players.Count; index++)
            {
                if (_players[index].Id == playerId)
                {
                    return _players[index];
                }
            }
            return null;
        }

        public void SetCurrentPlayer(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < _players.Count)
            {
                _currentPlayerIndex = playerIndex;
            }
        }

        public void NextPlayer()
        {
            if (_players.Count > 0)
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            }
        }

        public void SetIndicatorTile(TileData indicatorTile)
        {
            _indicatorTile = indicatorTile;
            _jokerTile = CalculateJokerTile(indicatorTile);
        }

        public void SetDrawPile(List<OkeyPiece> drawPile)
        {
            _drawPile = drawPile ?? new List<OkeyPiece>();
        }

        public OkeyPiece DrawFromPile()
        {
            if (_drawPile.Count > 0)
            {
                OkeyPiece drawnTile = _drawPile[0];
                _drawPile.RemoveAt(0);
                return drawnTile;
            }
            return null;
        }

        public void AddToDiscardPile(OkeyPiece tile)
        {
            if (tile != null)
            {
                _discardPile.Add(tile);
            }
        }

        public OkeyPiece GetTopDiscardTile()
        {
            return _discardPile.Count > 0 ? _discardPile[_discardPile.Count - 1] : null;
        }

        public OkeyPiece TakeFromDiscardPile()
        {
            if (_discardPile.Count > 0)
            {
                OkeyPiece topTile = _discardPile[_discardPile.Count - 1];
                _discardPile.RemoveAt(_discardPile.Count - 1);
                return topTile;
            }
            return null;
        }

        public void IncrementRound()
        {
            _roundNumber++;
        }

        public void EndGame()
        {
            _gameEnded = true;
            _currentState = Runtime.Domain.Enums.GameState.GameEnded;
        }

        public void ResetForNewRound()
        {
            _currentPlayerIndex = -1;
            _drawPile.Clear();
            _discardPile.Clear();
            
            for (int index = 0; index < _players.Count; index++)
            {
                _players[index].ClearTiles();
            }
        }

        private static TileData CalculateJokerTile(TileData indicatorTile)
        {
            if (indicatorTile.IsJoker)
            {
                return indicatorTile;
            }

            int jokerNumber = indicatorTile.Number == 13 ? 1 : indicatorTile.Number + 1;
            return new TileData(jokerNumber, indicatorTile.Color, OkeyPieceType.Joker);
        }
    }
}
