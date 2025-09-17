using Cysharp.Threading.Tasks;
using Runtime.Core.Configs;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Presentation.Views;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.Navigation;
using UnityEngine;
using Zenject;

namespace Runtime.Presentation.Presenters
{
    public sealed class GameBoardPresenter : BasePresenter<GameBoardView>
    {
        private readonly ISceneNavigator _sceneNavigator;
        private readonly IGameStateService _gameStateService;
        private readonly ITurnManager _turnManager;
        private readonly IGameRulesService _gameRulesService;

        [Inject]
        public GameBoardPresenter(ISceneNavigator sceneNavigator, IGameStateService gameStateService,
            ITurnManager turnManager, IGameRulesService gameRulesService)
        {
            _sceneNavigator = sceneNavigator;
            _gameStateService = gameStateService;
            _turnManager = turnManager;
            _gameRulesService = gameRulesService;
        }

        protected override void InitializeView()
        {
            base.InitializeView();
            InitializeGameAsync().Forget();
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            
            if (!ReferenceEquals(_view, null))
            {
                _view.OnDrawClicked += HandleDrawClicked;
                _view.OnDiscardClicked += HandleDiscardClicked;
                _view.OnWinClicked += HandleWinClicked;
                _view.OnPauseClicked += HandlePauseClicked;
                _view.OnBackToMenuClicked += HandleBackToMenuClicked;
                _view.OnResumeClicked += HandleResumeClicked;
            }

            if (!ReferenceEquals(_gameStateService, null))
            {
                _gameStateService.OnStateChanged += HandleGameStateChanged;
            }

            if (!ReferenceEquals(_turnManager, null))
            {
                _turnManager.OnTurnChanged += HandleTurnChanged;
            }
        }

        protected override void UnsubscribeFromEvents()
        {
            if (!ReferenceEquals(_view, null))
            {
                _view.OnDrawClicked -= HandleDrawClicked;
                _view.OnDiscardClicked -= HandleDiscardClicked;
                _view.OnWinClicked -= HandleWinClicked;
                _view.OnPauseClicked -= HandlePauseClicked;
                _view.OnBackToMenuClicked -= HandleBackToMenuClicked;
                _view.OnResumeClicked -= HandleResumeClicked;
            }

            if (!ReferenceEquals(_gameStateService, null))
            {
                _gameStateService.OnStateChanged -= HandleGameStateChanged;
            }

            if (!ReferenceEquals(_turnManager, null))
            {
                _turnManager.OnTurnChanged -= HandleTurnChanged;
            }

            base.UnsubscribeFromEvents();
        }

        private async UniTask InitializeGameAsync()
        {
            try
            {
                Debug.Log("[GameBoardPresenter] Initializing game...");

                // TODO: Initialize game with proper configuration
                // This will be expanded when we have full game flow in Phase 7
                UpdateUIForGameState();
                Debug.Log("[GameBoardPresenter] Game initialized successfully");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[GameBoardPresenter] Failed to initialize game: {exception.Message}");
            }
        }

        private void HandleDrawClicked()
        {
            Debug.Log("[GameBoardPresenter] Draw button clicked");
            // TODO: Implement draw logic when game flow is complete
        }

        private void HandleDiscardClicked()
        {
            Debug.Log("[GameBoardPresenter] Discard button clicked");
            // TODO: Implement discard logic when game flow is complete
        }

        private void HandleWinClicked()
        {
            Debug.Log("[GameBoardPresenter] Win button clicked");
            // TODO: Implement win declaration logic when game flow is complete
        }

        private void HandlePauseClicked()
        {
            Debug.Log("[GameBoardPresenter] Pause button clicked");
            _view?.ShowPausePanel();
        }

        private void HandleResumeClicked()
        {
            Debug.Log("[GameBoardPresenter] Resume button clicked");
            _view?.ShowGamePanel();
        }

        private async void HandleBackToMenuClicked()
        {
            Debug.Log("[GameBoardPresenter] Back to menu clicked");
            
            try
            {
                await _sceneNavigator.LoadScene((int)SceneConfigs.MainMenuScene);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[GameBoardPresenter] Failed to load MainMenu: {exception.Message}");
            }
        }

        private void HandleGameStateChanged(GameStateType newState)
        {
            Debug.Log($"[GameBoardPresenter] Game state changed to: {newState}");
            UpdateUIForGameState();
        }

        private void HandleTurnChanged(Player currentPlayer)
        {
            Debug.Log($"[GameBoardPresenter] Turn changed to player: {currentPlayer?.Name}");
            UpdateUIForCurrentTurn(currentPlayer);
        }

        private void UpdateUIForGameState()
        {
            if (_view == null) return;

            // TODO: Update UI based on current game state
            // This will be expanded when we have full game state management

            // For now, just ensure the game panel is shown
            _view.ShowGamePanel();
        }

        private void UpdateUIForCurrentTurn(Runtime.Domain.Models.Player currentPlayer)
        {
            if (_view == null || currentPlayer == null) return;

            // TODO: Update UI based on current player turn
            // Enable/disable buttons based on whether it's the human player's turn
            bool isHumanPlayerTurn = !currentPlayer.IsAI;
            _view.SetDrawButtonEnabled(isHumanPlayerTurn);
            _view.SetDiscardButtonEnabled(isHumanPlayerTurn);
            _view.SetWinButtonEnabled(isHumanPlayerTurn);
        }
    }
}