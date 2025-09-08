using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Models;
using Runtime.Domain.Enums;

namespace Runtime.Services.GameLogic
{
    public interface IWinConditionService
    {
        event Action<Player, WinType> OnWinConditionDetected;
        event Action<Player, List<List<OkeyPiece>>> OnValidSetsFound;
        event Action<Player> OnPairsWinDetected;

        UniTask<WinType?> CheckPlayerWinConditionAsync(Player player);
        UniTask<bool> ValidateWinDeclarationAsync(Player player, WinType declaredWinType);
        UniTask<List<List<OkeyPiece>>> FindValidSetsAsync(Player player);
        UniTask<bool> CheckPairsWinAsync(Player player);
        UniTask<bool> CheckNormalWinAsync(Player player);
        UniTask<bool> CheckOkeyWinAsync(Player player, OkeyPiece lastDiscardedTile);
    }
}
