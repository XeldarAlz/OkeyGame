using System;
using Runtime.Domain.Enums;
using UnityEngine;

namespace Runtime.Domain.Models
{
    [Serializable]
    public sealed class PlayerConfiguration
    {
        [SerializeField] private string _name;

        [SerializeField] private PlayerType _playerType;

        [SerializeField] private AIDifficulty _aiDifficulty;

        public string Name => _name;
        public PlayerType PlayerType => _playerType;
        public AIDifficulty AIDifficulty => _aiDifficulty;

        public PlayerConfiguration(string name, PlayerType playerType,
            AIDifficulty aiDifficulty = AIDifficulty.Beginner)
        {
            _name = name;
            _playerType = playerType;
            _aiDifficulty = aiDifficulty;
        }
    }
}