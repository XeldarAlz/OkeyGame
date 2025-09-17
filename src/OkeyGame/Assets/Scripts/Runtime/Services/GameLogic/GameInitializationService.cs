using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Tiles;
using Runtime.Services.AI;
using Zenject;

namespace Runtime.Services.GameLogic
{
    public sealed class GameInitializationService : IInitializableService, IDisposableService
    {
        private readonly IGameStateService _gameStateService;
        private readonly ITileService _tileService;
        private readonly IAIPlayerFactory _aiPlayerFactory;

        private GameConfiguration _currentConfiguration;
        private List<Player> _players;
        private List<OkeyPiece> _gameTiles;
        private OkeyPiece _indicatorTile;
        private bool _isInitialized;

        public GameConfiguration CurrentConfiguration => _currentConfiguration;
        public IReadOnlyList<Player> Players => _players?.AsReadOnly();
        public OkeyPiece IndicatorTile => _indicatorTile;
        public bool IsInitialized => _isInitialized;

        public event Action<GameConfiguration> OnGameConfigured;
        public event Action<List<Player>> OnPlayersCreated;
        public event Action<OkeyPiece> OnIndicatorTileSet;
        public event Action OnGameInitialized;

        [Inject]
        public GameInitializationService(
            IGameStateService gameStateService,
            ITileService tileService,
            IAIPlayerFactory aiPlayerFactory)
        {
            _gameStateService = gameStateService;
            _tileService = tileService;
            _aiPlayerFactory = aiPlayerFactory;
            _players = new List<Player>();
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.Yield();
            return true;
        }

        public async UniTask<bool> InitializeGameAsync(GameConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
            {
                return false;
            }

            try
            {
                _currentConfiguration = configuration;
                OnGameConfigured?.Invoke(configuration);

                await CreatePlayersAsync(cancellationToken);
                await CreateAndShuffleTilesAsync(cancellationToken);
                await SelectIndicatorTileAsync(cancellationToken);
                await DistributeTilesToPlayersAsync(cancellationToken);

                _isInitialized = true;
                OnGameInitialized?.Invoke();

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception)
            {
                ResetGameState();
                return false;
            }
        }

        public async UniTask<bool> RestartGameAsync(CancellationToken cancellationToken = default)
        {
            if (_currentConfiguration == null)
            {
                return false;
            }

            ResetGameState();
            return await InitializeGameAsync(_currentConfiguration, cancellationToken);
        }

        public Player GetHumanPlayer()
        {
            if (_players == null)
            {
                return null;
            }

            for (int playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                Player player = _players[playerIndex];
                if (player.IsHuman)
                {
                    return player;
                }
            }

            return null;
        }

        public List<Player> GetAIPlayers()
        {
            List<Player> aiPlayers = new List<Player>();

            if (_players == null)
            {
                return aiPlayers;
            }

            for (int playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                Player player = _players[playerIndex];
                if (player.IsAI)
                {
                    aiPlayers.Add(player);
                }
            }

            return aiPlayers;
        }

        public Player GetPlayerById(int playerId)
        {
            if (_players == null)
            {
                return null;
            }

            for (int playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                Player player = _players[playerIndex];
                if (player.Id == playerId)
                {
                    return player;
                }
            }

            return null;
        }

        private async UniTask CreatePlayersAsync(CancellationToken cancellationToken)
        {
            _players.Clear();

            IReadOnlyList<PlayerConfiguration> playerConfigs = _currentConfiguration.PlayerConfigurations;
            
            for (int configIndex = 0; configIndex < playerConfigs.Count; configIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PlayerConfiguration config = playerConfigs[configIndex];
                Player player = CreatePlayerFromConfiguration(configIndex, config);
                _players.Add(player);
            }

            OnPlayersCreated?.Invoke(_players);
            await UniTask.Yield();
        }

        private Player CreatePlayerFromConfiguration(int playerId, PlayerConfiguration config)
        {
            if (config.PlayerType == PlayerType.Human)
            {
                return new Player(playerId, config.Name, PlayerType.Human);
            }
            else
            {
                return _aiPlayerFactory.CreateAIPlayer(playerId, config.Name, config.AIDifficulty);
            }
        }

        private async UniTask CreateAndShuffleTilesAsync(CancellationToken cancellationToken)
        {
            _gameTiles = await _tileService.CreateFullTileSetAsync();
            cancellationToken.ThrowIfCancellationRequested();

            _gameTiles = _tileService.ShuffleTiles(_gameTiles);
            await UniTask.Yield();
        }

        private async UniTask SelectIndicatorTileAsync(CancellationToken cancellationToken)
        {
            if (_gameTiles == null || _gameTiles.Count == 0)
            {
                return;
            }

            int indicatorIndex = _gameTiles.Count - 1;
            _indicatorTile = _gameTiles[indicatorIndex];
            _gameTiles.RemoveAt(indicatorIndex);

            await _tileService.SetIndicatorTileAsync(_indicatorTile);
            OnIndicatorTileSet?.Invoke(_indicatorTile);

            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }

        private async UniTask DistributeTilesToPlayersAsync(CancellationToken cancellationToken)
        {
            if (_players == null || _gameTiles == null)
            {
                return;
            }

            int tileIndex = 0;

            for (int playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Player player = _players[playerIndex];
                int tilesToDistribute = playerIndex == 0 ? 15 : 14;

                for (int tileCount = 0; tileCount < tilesToDistribute && tileIndex < _gameTiles.Count; tileCount++)
                {
                    OkeyPiece tile = _gameTiles[tileIndex++];
                    player.AddTile(tile);
                }
            }

            await UniTask.Yield();
        }

        private void ResetGameState()
        {
            _players?.Clear();
            _gameTiles?.Clear();
            _indicatorTile = null;
            _isInitialized = false;
        }

        public void Dispose()
        {
            ResetGameState();
            _currentConfiguration = null;
        }

        public async UniTask InitializeAsync()
        {
            // Reset internal state
            _currentConfiguration = null;
            _players.Clear();
            _gameTiles = null;
            _indicatorTile = null;
            _isInitialized = false;
            
            // Initialize dependent services
            await _gameStateService.InitializeAsync();
            await _tileService.InitializeAsync();
            
            _isInitialized = true;
            await UniTask.Yield();
        }
    }
}
