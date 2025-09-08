using System;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;

namespace Runtime.Services
{
    public interface IGameStateService : IInitializableService, IDisposableService
    {
        GameStateType CurrentGameStateType { get; }
        GameStateType CurrentStateType { get; }
        Player CurrentPlayer { get; }
        bool IsGameActive { get; }

        event Action<GameStateType> OnStateChanged;
        event Action<Player> OnPlayerTurnChanged;
        event Action<GameStateType> OnGameStateUpdated;

        UniTask<bool> StartNewGameAsync(GameConfiguration configuration);
        UniTask<bool> EndGameAsync();
        UniTask<bool> TransitionToStateAsync(GameStateType newStateType);
        UniTask<bool> NextPlayerTurnAsync();

        void UpdateGameState(GameStateType newStateType);
        Player GetPlayerById(int playerId);
        bool IsPlayerTurn(int playerId);
    }
}