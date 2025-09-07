using Runtime.Core.Architecture;
using Runtime.Domain.Enums;

namespace Runtime.Services.AI
{
    public interface IAIPlayerFactory : IService
    {
        IAIPlayer CreateAIPlayer(int playerId, string name, AIDifficulty difficulty);
        IAIPlayer CreateBeginnerAI(int playerId, string name);
        IAIPlayer CreateIntermediateAI(int playerId, string name);
        IAIPlayer CreateAdvancedAI(int playerId, string name);
    }
}