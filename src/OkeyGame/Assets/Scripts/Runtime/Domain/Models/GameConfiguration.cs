using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Domain.Models
{
    [Serializable]
    public sealed class GameConfiguration
    {
        [SerializeField] private int _playerCount;
        [SerializeField] private int _startingScore;

        [SerializeField] private List<PlayerConfiguration> _playerConfigurations;

        [SerializeField] private bool _enableTimer;
        [SerializeField] private bool _enableSound;

        [SerializeField] private float _turnTimeLimit;

        [SerializeField] private SystemLanguage _language;

        public int PlayerCount => _playerCount;
        public IReadOnlyList<PlayerConfiguration> PlayerConfigurations => _playerConfigurations.AsReadOnly();
        public int StartingScore => _startingScore;
        public bool EnableTimer => _enableTimer;
        public float TurnTimeLimit => _turnTimeLimit;
        public bool EnableSound => _enableSound;
        public SystemLanguage Language => _language;

        public GameConfiguration(int playerCount = 4, int startingScore = 20)
        {
            _playerCount = Mathf.Clamp(playerCount, 2, 4);
            _startingScore = startingScore;
            _enableTimer = false;
            _turnTimeLimit = 60f;
            _enableSound = true;
            _language = SystemLanguage.English;
            _playerConfigurations = new List<PlayerConfiguration>();
        }

        public void AddPlayerConfiguration(PlayerConfiguration playerConfig)
        {
            if (playerConfig != null && _playerConfigurations.Count < _playerCount)
            {
                _playerConfigurations.Add(playerConfig);
            }
        }

        public void SetStartingScore(int score)
        {
            _startingScore = Mathf.Max(1, score);
        }

        public void SetTurnTimer(bool enabled, float timeLimit = 60f)
        {
            _enableTimer = enabled;
            _turnTimeLimit = Mathf.Max(10f, timeLimit);
        }

        public void SetSoundEnabled(bool enabled)
        {
            _enableSound = enabled;
        }

        public void SetLanguage(SystemLanguage language)
        {
            _language = language;
        }
    }
}