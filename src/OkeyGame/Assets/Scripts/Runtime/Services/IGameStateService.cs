using System;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using GameState = Runtime.Domain.Enums.GameState;

namespace Runtime.Services
{
    public interface IGameStateService : IInitializableService, IDisposableService
    {
        GameState CurrentGameState { get; }
        GameState CurrentState { get; }
        Player CurrentPlayer { get; }
        bool IsGameActive { get; }

        event Action<GameState> OnStateChanged;
        event Action<Player> OnPlayerTurnChanged;
        event Action<GameState> OnGameStateUpdated;

        UniTask<bool> StartNewGameAsync(GameConfiguration configuration);
        UniTask<bool> EndGameAsync();
        UniTask<bool> TransitionToStateAsync(GameState newState);
        UniTask<bool> NextPlayerTurnAsync();

        void UpdateGameState(GameState newState);
        Player GetPlayerById(int playerId);
        bool IsPlayerTurn(int playerId);
    }
}