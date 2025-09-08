using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;
using Runtime.Domain.ValueObjects;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.AI;
using Zenject;

namespace Runtime.Services.GameLogic
{
    public sealed class TurnBasedGameLoop : IInitializableService, IDisposableService
    {
        private readonly IGameStateService _gameStateService;
        private readonly ITurnManager _turnManager;
        private readonly IGameRulesService _gameRulesService;

        private bool _isLoopActive;
        private bool _isProcessingTurn;
        private CancellationTokenSource _loopCancellation;

        public bool IsLoopActive => _isLoopActive;
        public bool IsProcessingTurn => _isProcessingTurn;

        public event Action<Player> OnTurnStarted;
        public event Action<Player> OnTurnEnded;
        public event Action<Player, PlayerAction> OnPlayerActionExecuted;
        public event Action<Player, WinType> OnPlayerWon;
        public event Action OnGameLoopStopped;

        [Inject]
        public TurnBasedGameLoop(
            IGameStateService gameStateService,
            ITurnManager turnManager,
            IGameRulesService gameRulesService)
        {
            _gameStateService = gameStateService;
            _turnManager = turnManager;
            _gameRulesService = gameRulesService;
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            SubscribeToEvents();
            await UniTask.Yield();
            return true;
        }

        public async UniTask InitializeAsync()
        {
            // Reset game loop state
            _isLoopActive = false;
            _isProcessingTurn = false;
            _loopCancellation?.Cancel();
            _loopCancellation?.Dispose();
            _loopCancellation = null;
            
            await UniTask.Yield();
        }

        public async UniTask<bool> StartGameLoopAsync(CancellationToken cancellationToken = default)
        {
            if (_isLoopActive)
            {
                return false;
            }

            _loopCancellation?.Cancel();
            _loopCancellation = new CancellationTokenSource();

            try
            {
                _isLoopActive = true;
                await RunGameLoopAsync(_loopCancellation.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _isLoopActive = false;
                OnGameLoopStopped?.Invoke();
            }
        }

        public async UniTask<bool> StopGameLoopAsync()
        {
            if (!_isLoopActive)
            {
                return false;
            }

            _loopCancellation?.Cancel();
            
            while (_isLoopActive)
            {
                await UniTask.Yield();
            }

            return true;
        }

        public async UniTask<bool> ProcessPlayerActionAsync(PlayerAction action, Player player)
        {
            if (!_isLoopActive || _isProcessingTurn || action == null || player == null)
            {
                return false;
            }

            if (_turnManager.CurrentPlayer != player)
            {
                return false;
            }

            GameState currentGameState = _gameStateService.GetCurrentGameState();
            bool isValidAction = _gameRulesService.ValidatePlayerMove(action, currentGameState);
            
            if (!isValidAction)
            {
                return false;
            }

            _isProcessingTurn = true;

            try
            {
                bool actionExecuted = await ExecutePlayerActionAsync(action, player);
                if (actionExecuted)
                {
                    OnPlayerActionExecuted?.Invoke(player, action);
                    
                    WinType? winCondition = await CheckWinConditionAsync(player);
                    if (winCondition.HasValue)
                    {
                        OnPlayerWon?.Invoke(player, winCondition.Value);
                        await StopGameLoopAsync();
                        return true;
                    }

                    await EndCurrentTurnAsync(player);
                    await StartNextTurnAsync();
                }

                return actionExecuted;
            }
            finally
            {
                _isProcessingTurn = false;
            }
        }

        private async UniTask RunGameLoopAsync(CancellationToken cancellationToken)
        {
            await _turnManager.StartFirstTurnAsync();

            while (!cancellationToken.IsCancellationRequested && _isLoopActive)
            {
                try
                {
                    Player currentPlayer = _turnManager.CurrentPlayer;
                    if (currentPlayer == null)
                    {
                        break;
                    }

                    await ProcessCurrentPlayerTurnAsync(currentPlayer, cancellationToken);
                    await UniTask.Yield();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async UniTask ProcessCurrentPlayerTurnAsync(Player currentPlayer, CancellationToken cancellationToken)
        {
            if (currentPlayer.IsAI)
            {
                await ProcessAIPlayerTurnAsync(currentPlayer, cancellationToken);
            }
            else
            {
                await ProcessHumanPlayerTurnAsync(currentPlayer, cancellationToken);
            }
        }

        private async UniTask ProcessAIPlayerTurnAsync(Player aiPlayer, CancellationToken cancellationToken)
        {
            await _gameStateService.TransitionToStateAsync(GameStateType.AITurn);
            OnTurnStarted?.Invoke(aiPlayer);

            await UniTask.Delay(1000, cancellationToken: cancellationToken);

            if (aiPlayer is IAIPlayer aiPlayerInterface)
            {
                GameState currentGameState = _gameStateService.GetCurrentGameState();
                PlayerAction aiAction = await aiPlayerInterface.DecideActionAsync(currentGameState);
                
                if (aiAction != null)
                {
                    await ProcessPlayerActionAsync(aiAction, aiPlayer);
                }
            }
        }

        private async UniTask ProcessHumanPlayerTurnAsync(Player humanPlayer, CancellationToken cancellationToken)
        {
            await _gameStateService.TransitionToStateAsync(GameStateType.PlayerTurn);
            OnTurnStarted?.Invoke(humanPlayer);

            while (_turnManager.CurrentPlayer == humanPlayer && !cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask<bool> ExecutePlayerActionAsync(PlayerAction action, Player player)
        {
            switch (action.ActionType)
            {
                case TurnAction.Draw:
                    // Handle both drawing from pile and discard
                    // You might need additional logic to determine which one
                    return await HandleDrawFromPileAsync(player);
                
                case TurnAction.Discard:
                    // Find the OkeyPiece in the player's rack that matches the TileData
                    OkeyPiece tileToDiscard = FindPlayerTileByTileData(player, action.TileData);
                    if (tileToDiscard == null)
                    {
                        return false;
                    }
                    return await HandleDiscardTileAsync(player, tileToDiscard);
                
                case TurnAction.DeclareWin:
                    return await HandleDeclareWinAsync(player);
                
                case TurnAction.ShowIndicator:
                    // Find the OkeyPiece in the player's rack that matches the TileData
                    OkeyPiece indicatorTile = FindPlayerTileByTileData(player, action.TileData);
                    if (indicatorTile == null)
                    {
                        return false;
                    }
                    return await HandleShowIndicatorAsync(player, indicatorTile);
                
                default:
                    return false;
            }
        }
        
        private OkeyPiece FindPlayerTileByTileData(Player player, TileData tileData)
        {
            if (player == null || tileData.Equals(default(TileData)))
            {
                return null;
            }
            
            // Find the matching tile in the player's rack
            foreach (OkeyPiece tile in player.Tiles)
            {
                if (tile != null && 
                    tile.Number == tileData.Number && 
                    tile.Color == tileData.Color && 
                    tile.PieceType == tileData.PieceType)
                {
                    return tile;
                }
            }
            
            return null;
        }

        private async UniTask<bool> HandleDrawFromPileAsync(Player player)
        {
            await UniTask.Yield();
            return true;
        }

        private async UniTask<bool> HandleDrawFromDiscardAsync(Player player)
        {
            await UniTask.Yield();
            return true;
        }

        private async UniTask<bool> HandleDiscardTileAsync(Player player, OkeyPiece tile)
        {
            if (tile == null || !player.RemoveTile(tile))
            {
                return false;
            }

            await UniTask.Yield();
            return true;
        }

        private async UniTask<bool> HandleMoveTileInRackAsync(Player player, OkeyPiece tile, GridPosition fromPosition, GridPosition toPosition)
        {
            await UniTask.Yield();
            return true;
        }

        private async UniTask<bool> HandleDeclareWinAsync(Player player)
        {
            WinType? winCondition = await CheckWinConditionAsync(player);
            return winCondition.HasValue;
        }

        private async UniTask<bool> HandleShowIndicatorAsync(Player player, OkeyPiece indicatorTile)
        {
            await UniTask.Yield();
            return true;
        }

        private async UniTask<WinType?> CheckWinConditionAsync(Player player)
        {
            if (player == null)
            {
                return null;
            }

            await UniTask.Yield();
            return null;
        }

        private async UniTask EndCurrentTurnAsync(Player player)
        {
            OnTurnEnded?.Invoke(player);
            await UniTask.Yield();
        }

        private async UniTask StartNextTurnAsync()
        {
            await _turnManager.NextPlayerTurnAsync();
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
        }

        private void HandleGameStateChanged(GameStateType newState)
        {
            if (newState == GameStateType.GameEnded || newState == GameStateType.Paused)
            {
                _loopCancellation?.Cancel();
            }
        }

        private void HandlePlayerTurnChanged(Player newCurrentPlayer)
        {
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
            _loopCancellation?.Cancel();
            _loopCancellation?.Dispose();
            _isLoopActive = false;
            _isProcessingTurn = false;
        }
    }
}
