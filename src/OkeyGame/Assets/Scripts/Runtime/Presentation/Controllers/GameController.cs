using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.GameLogic.Tiles;
using Runtime.Services.GameLogic.Score;
using Runtime.Services.AI;
using Runtime.Presentation.Presenters;
using Zenject;

namespace Runtime.Presentation.Controllers
{
    public sealed class GameController : IInitializable, IDisposable
    {
        private readonly IGameStateService _gameStateService;
        private readonly ITileService _tileService;
        private readonly IGameRulesService _gameRulesService;
        private readonly ITurnManager _turnManager;
        private readonly IAIPlayerFactory _aiPlayerFactory;
        private readonly IWinConditionService _winConditionService;
        private readonly IScoreCalculationService _scoreCalculationService;
        private readonly IScoreService _scoreService;
        private readonly GameBoardPresenter _gameBoardPresenter;
        private readonly PlayerRackPresenter _playerRackPresenter;
        
        private GameConfiguration _currentGameConfiguration;
        private List<Player> _players;
        private Player _humanPlayer;
        private List<Player> _aiPlayers;
        private CancellationTokenSource _gameLoopCancellation;
        private bool _isGameActive;
        
        public event Action<GameStateType> OnGameStateChanged;
        public event Action<Player> OnPlayerTurnChanged;
        public event Action<Player, WinType> OnGameEnded;
        public event Action<List<Player>> OnPlayersInitialized;
        public GameConfiguration CurrentGameConfiguration => _currentGameConfiguration;
        public List<Player> Players => _players;
        public Player HumanPlayer => _humanPlayer;
        public bool IsGameActive => _isGameActive;
        public GameStateType CurrentGameState => _gameStateService?.CurrentStateType ?? GameStateType.None;

        [Inject]
        public GameController(IGameStateService gameStateService, ITileService tileService,
            IGameRulesService gameRulesService, ITurnManager turnManager, IAIPlayerFactory aiPlayerFactory,
            IWinConditionService winConditionService, IScoreCalculationService scoreCalculationService,
            IScoreService scoreService, GameBoardPresenter gameBoardPresenter, PlayerRackPresenter playerRackPresenter)
        {
            _gameStateService = gameStateService;
            _tileService = tileService;
            _gameRulesService = gameRulesService;
            _turnManager = turnManager;
            _aiPlayerFactory = aiPlayerFactory;
            _winConditionService = winConditionService;
            _scoreCalculationService = scoreCalculationService;
            _scoreService = scoreService;
            _gameBoardPresenter = gameBoardPresenter;
            _playerRackPresenter = playerRackPresenter;
            _players = new List<Player>();
            _aiPlayers = new List<Player>();
            _gameLoopCancellation = new CancellationTokenSource();
        }

        public void Initialize()
        {
            SubscribeToEvents();
        }

        public async UniTask<bool> StartNewGameAsync(GameConfiguration configuration)
        {
            if (_isGameActive)
            {
                await EndCurrentGameAsync();
            }

            _currentGameConfiguration = configuration;
            _gameLoopCancellation?.Cancel();
            _gameLoopCancellation = new CancellationTokenSource();
            
            try
            {
                await InitializeGameAsync();
                await SetupPlayersAsync();
                await DistributeTilesAsync();
                await StartGameLoopAsync();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception)
            {
                await EndCurrentGameAsync();
                return false;
            }
        }

        public async UniTask<bool> EndCurrentGameAsync()
        {
            if (!_isGameActive)
            {
                return true;
            }

            _gameLoopCancellation?.Cancel();
            _isGameActive = false;
            
            await _gameStateService.TransitionToStateAsync(GameStateType.GameEnded);
            
            ClearGameData();
            
            return true;
        }

        public async UniTask<bool> PauseGameAsync()
        {
            if (!_isGameActive)
            {
                return false;
            }

            return await _gameStateService.TransitionToStateAsync(GameStateType.Paused);
        }

        public async UniTask<bool> ResumeGameAsync()
        {
            if (!_isGameActive || _gameStateService.CurrentStateType != GameStateType.Paused)
            {
                return false;
            }

            Player currentPlayer = _turnManager.GetCurrentPlayer();
            GameStateType targetState = currentPlayer != null && currentPlayer.IsAI
                ? GameStateType.AITurn
                : GameStateType.PlayerTurn;
            
            return await _gameStateService.TransitionToStateAsync(targetState);
        }

        public async UniTask<bool> ProcessPlayerActionAsync(PlayerAction action)
        {
            if (!_isGameActive || _gameStateService.CurrentStateType != GameStateType.PlayerTurn)
            {
                return false;
            }

            Player currentPlayer = _turnManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.IsAI)
            {
                return false;
            }

            bool isValidAction = await ValidatePlayerActionAsync(action, currentPlayer);
            if (!isValidAction)
            {
                return false;
            }

            await ExecutePlayerActionAsync(action, currentPlayer);
            
            WinType? winCondition = await CheckWinConditionAsync(currentPlayer);
            
            if (winCondition.HasValue)
            {
                await HandleGameEndAsync(currentPlayer, winCondition.Value);
                return true;
            }

            await _turnManager.NextPlayerTurnAsync();
            
            return true;
        }

        private async UniTask InitializeGameAsync()
        {
            _isGameActive = true;
            
            await _gameStateService.TransitionToStateAsync(GameStateType.Initializing);
        }

        private async UniTask SetupPlayersAsync()
        {
            _players.Clear();
            _aiPlayers.Clear();
            _humanPlayer = new Player(0, "Human Player", PlayerType.Human);
            _players.Add(_humanPlayer);
            
            for (int aiIndex = 1; aiIndex <= 3; aiIndex++)
            {
                AIDifficulty difficulty = _currentGameConfiguration.AIDifficulty;
                Player aiPlayer = _aiPlayerFactory.CreateAIPlayer(aiIndex, $"AI Player {aiIndex}", difficulty);
                _players.Add(aiPlayer);
                _aiPlayers.Add(aiPlayer);
            }

            await _playerRackPresenter.SetPlayerAsync(_humanPlayer);
            await _turnManager.InitializePlayersAsync(_players);
            
            OnPlayersInitialized?.Invoke(_players);
        }

        private async UniTask DistributeTilesAsync()
        {
            List<OkeyPiece> allTiles = await _tileService.CreateFullTileSetAsync();
            List<OkeyPiece> shuffledTiles = _tileService.ShuffleTiles(allTiles);
            int tileIndex = 0;
            
            for (int playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                Player player = _players[playerIndex];
                int tilesToDraw = playerIndex == 0 ? 15 : 14;
                for (int tileCount = 0; tileCount < tilesToDraw; tileCount++)
                {
                    if (tileIndex < shuffledTiles.Count)
                    {
                        OkeyPiece tile = shuffledTiles[tileIndex++];
                        player.AddTile(tile);
                        if (player == _humanPlayer)
                        {
                            await _playerRackPresenter.AddTileToRackAsync(tile);
                        }
                    }
                }
            }

            if (tileIndex < shuffledTiles.Count)
            {
                OkeyPiece indicatorTile = shuffledTiles[tileIndex];
                await _tileService.SetIndicatorTileAsync(indicatorTile);
            }
        }

        private async UniTask StartGameLoopAsync()
        {
            await _gameStateService.TransitionToStateAsync(GameStateType.GameStarted);
            await _turnManager.StartFirstTurnAsync();
            
            while (_isGameActive && !_gameLoopCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    await ProcessCurrentTurnAsync();
                    await UniTask.Yield();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async UniTask ProcessCurrentTurnAsync()
        {
            Player currentPlayer = _turnManager.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                return;
            }

            if (currentPlayer.IsAI)
            {
                await ProcessAITurnAsync(currentPlayer);
            }
            else
            {
                await ProcessHumanTurnAsync(currentPlayer);
            }
        }

        private async UniTask ProcessAITurnAsync(Player aiPlayer)
        {
            await _gameStateService.TransitionToStateAsync(GameStateType.AITurn);
            await UniTask.Delay(1000, cancellationToken: _gameLoopCancellation.Token);
            
            if (aiPlayer is IAIPlayer aiPlayerInterface)
            {
                PlayerAction aiAction =
                    await aiPlayerInterface.DecideActionAsync(_gameStateService.GetCurrentGameState());
                bool actionExecuted = await ExecutePlayerActionAsync(aiAction, aiPlayer);
                
                if (actionExecuted)
                {
                    WinType? winCondition = await CheckWinConditionAsync(aiPlayer);
                    
                    if (winCondition.HasValue)
                    {
                        await HandleGameEndAsync(aiPlayer, winCondition.Value);
                        return;
                    }
                }
            }

            await _turnManager.NextPlayerTurnAsync();
        }

        private async UniTask ProcessHumanTurnAsync(Player humanPlayer)
        {
            await _gameStateService.TransitionToStateAsync(GameStateType.PlayerTurn);
        }

        private async UniTask<bool> ValidatePlayerActionAsync(PlayerAction action, Player player)
        {
            if (player == null)
            {
                return false;
            }

            GameState currentGameState = _gameStateService.GetCurrentGameState();
            
            return _gameRulesService.ValidatePlayerMove(action, currentGameState);
        }

        private async UniTask<bool> ExecutePlayerActionAsync(PlayerAction action, Player player)
        {
            if (player == null)
            {
                return false;
            }

            switch (action.ActionType)
            {
                case TurnAction.Draw:
                    return await HandleDrawFromPileAsync(player);
                case TurnAction.Discard:
                    OkeyPiece tileToDiscard = FindPlayerTileByData(player, action.TileData);
                    return await HandleDiscardTileAsync(player, tileToDiscard);
                case TurnAction.DeclareWin:
                    return await HandleDeclareWinAsync(player);
                default:
                    return false;
            }
        }

        private async UniTask<bool> HandleDrawFromPileAsync(Player player)
        {
            OkeyPiece drawnTile = await _tileService.DrawTileFromPileAsync();
            
            if (drawnTile == null)
            {
                return false;
            }

            player.AddTile(drawnTile);
            
            if (player == _humanPlayer)
            {
                await _playerRackPresenter.AddTileToRackAsync(drawnTile);
            }

            return true;
        }

        private async UniTask<bool> HandleDrawFromDiscardAsync(Player player)
        {
            OkeyPiece discardedTile = await _tileService.DrawTileFromDiscardAsync();
            
            if (discardedTile == null)
            {
                return false;
            }

            player.AddTile(discardedTile);
            
            if (ReferenceEquals(player, _humanPlayer))
            {
                await _playerRackPresenter.AddTileToRackAsync(discardedTile);
            }

            return true;
        }

        private async UniTask<bool> HandleDiscardTileAsync(Player player, OkeyPiece tile)
        {
            if (tile == null || !player.RemoveTile(tile))
            {
                return false;
            }

            await _tileService.AddTileToDiscardAsync(tile);
            
            if (ReferenceEquals(player, _humanPlayer))
            {
                await _playerRackPresenter.RemoveTileFromRackAsync(tile);
            }

            return true;
        }

        private async UniTask<bool> HandleDeclareWinAsync(Player player)
        {
            WinType? winCondition = await _winConditionService.CheckPlayerWinConditionAsync(player);

            if (!winCondition.HasValue)
            {
                return false;
            }
            
            bool isValidWin = await _winConditionService.ValidateWinDeclarationAsync(player, winCondition.Value);
            
            if (!isValidWin)
            {
                return false;
            }
            
            await HandleGameEndAsync(player, winCondition.Value);
            
            return true;
        }

        private async UniTask<WinType?> CheckWinConditionAsync(Player player)
        {
            return await _winConditionService.CheckPlayerWinConditionAsync(player);
        }

        private async UniTask HandleGameEndAsync(Player winner, WinType winType)
        {
            _isGameActive = false;

            // Calculate and apply scores for all players
            Dictionary<Player, int> roundScores =
                await _scoreCalculationService.CalculateRoundScoresAsync(_players, winner, winType);
            await _scoreService.ApplyWinScoreAsync(winner, winType, _players);

            // Check if game should end (player elimination or target score reached)
            bool gameEnded = await _scoreService.CheckGameEndConditionAsync(_players);
            
            if (gameEnded)
            {
                await _gameStateService.TransitionToStateAsync(GameStateType.GameEnded);
            }
            else
            {
                await _gameStateService.TransitionToStateAsync(GameStateType.RoundEnded);
            }

            OnGameEnded?.Invoke(winner, winType);
        }

        private void ClearGameData()
        {
            _players.Clear();
            _aiPlayers.Clear();
            _humanPlayer = null;
            _currentGameConfiguration = null;
        }

        private void SubscribeToEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged += HandleGameStateChanged;
            }

            if (_turnManager != null)
            {
                _turnManager.OnPlayerTurnChanged += HandlePlayerTurnChanged;
            }

            if (_winConditionService != null)
            {
                _winConditionService.OnWinConditionDetected += HandleWinConditionDetected;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged -= HandleGameStateChanged;
            }

            if (_turnManager != null)
            {
                _turnManager.OnPlayerTurnChanged -= HandlePlayerTurnChanged;
            }

            if (_winConditionService != null)
            {
                _winConditionService.OnWinConditionDetected -= HandleWinConditionDetected;
            }
        }

        private void HandleGameStateChanged(GameStateType newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        private void HandlePlayerTurnChanged(Player newCurrentPlayer)
        {
            OnPlayerTurnChanged?.Invoke(newCurrentPlayer);
        }

        private async void HandleWinConditionDetected(Player winner, WinType winType)
        {
            await HandleGameEndAsync(winner, winType);
        }

        private OkeyPiece FindPlayerTileByData(Player player, TileData tileData)
        {
            if (player == null)
            {
                return null;
            }

            List<OkeyPiece> playerTiles = player.GetTiles();
            
            for (int tileIndex = 0; tileIndex < playerTiles.Count; tileIndex++)
            {
                OkeyPiece tile = playerTiles[tileIndex];
                if (tile != null && tile.TileData.Equals(tileData))
                {
                    return tile;
                }
            }

            return null;
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
            _gameLoopCancellation?.Cancel();
            _gameLoopCancellation?.Dispose();
            ClearGameData();
        }
    }
}