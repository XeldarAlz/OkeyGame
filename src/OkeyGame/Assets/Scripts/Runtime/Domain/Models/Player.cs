using System;
using System.Collections.Generic;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using UnityEngine;

namespace Runtime.Domain.Models
{
    [Serializable]
    public class Player
    {
        [SerializeField] 
        private int _score;
        
        [SerializeField] 
        private string _name;
        
        [SerializeField] 
        private PlayerType _playerType;
        
        [SerializeField] 
        private bool _isActive;
        
        [SerializeField] 
        private List<OkeyPiece> _tiles;

        private int _id;
        
        public int Id => _id;
        public string Name => _name;
        public PlayerType PlayerType => _playerType;
        public int Score => _score;
        public bool IsActive => _isActive;
        public bool IsAI => _playerType == PlayerType.AI;
        public bool IsHuman => _playerType == PlayerType.Human;
        public List<OkeyPiece> Tiles => _tiles;
        public int TileCount => _tiles.Count;

        public Player(int id, string name, PlayerType playerType)
        {
            _id = id;
            _name = name;
            _playerType = playerType;
            _score = 20;
            _isActive = true;
            _tiles = new List<OkeyPiece>();
        }

        public void AddTile(OkeyPiece tile)
        {
            if (tile != null && !_tiles.Contains(tile))
            {
                _tiles.Add(tile);
            }
        }

        public bool RemoveTile(OkeyPiece tile)
        {
            return tile != null && _tiles.Remove(tile);
        }

        public bool RemoveTileById(int uniqueId)
        {
            for (int index = 0; index < _tiles.Count; index++)
            {
                if (_tiles[index].UniqueId == uniqueId)
                {
                    _tiles.RemoveAt(index);
                    return true;
                }
            }
            return false;
        }

        public OkeyPiece GetTileById(int uniqueId)
        {
            for (int index = 0; index < _tiles.Count; index++)
            {
                if (_tiles[index].UniqueId == uniqueId)
                {
                    return _tiles[index];
                }
            }
            return null;
        }

        public void ClearTiles()
        {
            _tiles.Clear();
        }

        public void SetScore(int score)
        {
            _score = score;
        }

        public void AddScore(int points)
        {
            _score += points;
        }

        public void SubtractScore(int points)
        {
            _score = Mathf.Max(0, _score - points);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public List<OkeyPiece> GetTiles()
        {
            return new List<OkeyPiece>(_tiles);
        }

        public bool HasTile(TileData tileData)
        {
            for (int index = 0; index < _tiles.Count; index++)
            {
                if (_tiles[index].TileData.Equals(tileData))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"Player {_id}: {_name} ({_playerType}) - Score: {_score}, Tiles: {_tiles.Count}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Player other)
            {
                return _id == other._id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
