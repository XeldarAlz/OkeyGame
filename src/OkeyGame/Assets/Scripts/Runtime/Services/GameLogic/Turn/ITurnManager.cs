using System;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;

namespace Runtime.Services.GameLogic.Turn
{
    public interface ITurnManager : IInitializableService, IDisposableService
    {
        Player CurrentPlayer { get; }
        int CurrentPlayerIndex { get; }
        bool IsPlayerTurn(int playerId);
        
        event Action<Player> OnTurnChanged;
        event Action<Player> OnTurnStarted;
        event Action<Player> OnTurnEnded;
        
        UniTask<bool> StartTurnAsync(Player player);
        UniTask<bool> EndTurnAsync();
        UniTask<bool> NextTurnAsync();
        void SetTurnOrder(Player[] players);
        void SetCurrentPlayer(int playerIndex);
        void ResetTurnOrder();
        int GetPlayerIndex(Player player);
    }
}
