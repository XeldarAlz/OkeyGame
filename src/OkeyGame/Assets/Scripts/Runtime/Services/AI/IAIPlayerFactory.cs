using Runtime.Domain.Models;
using Runtime.Domain.Enums;

namespace Runtime.Services.AI
{
    public interface IAIPlayerFactory
    {
        Player CreateAIPlayer(int playerId, string playerName, AIDifficulty difficulty);
        IAIPlayer CreateAIPlayerInterface(int playerId, string playerName, AIDifficulty difficulty);
    }
}
