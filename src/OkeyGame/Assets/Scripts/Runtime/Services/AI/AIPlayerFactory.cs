using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Zenject;

namespace Runtime.Services.AI
{
    public sealed class AIPlayerFactory : IAIPlayerFactory, IFactory<AIDifficulty, int, string, IAIPlayer>
    {
        private readonly IInstantiator _instantiator;
        
        public AIPlayerFactory(IInstantiator instantiator)
        {
            _instantiator = instantiator;
        }

        public Player CreateAIPlayer(int playerId, string playerName, AIDifficulty difficulty)
        {
            IAIPlayer aiPlayerInterface = CreateAIPlayerInterface(playerId, playerName, difficulty);
            return aiPlayerInterface as Player ?? new Player(playerId, playerName, PlayerType.AI);
        }

        public IAIPlayer CreateAIPlayerInterface(int playerId, string playerName, AIDifficulty difficulty)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"AI Player {playerId}";
            }

            IAIPlayer aiPlayer = difficulty switch
            {
                AIDifficulty.Beginner => _instantiator.Instantiate<BeginnerAIPlayer>(new object[] { playerId, playerName }),
                AIDifficulty.Intermediate => _instantiator.Instantiate<IntermediateAIPlayer>(new object[] { playerId, playerName }),
                AIDifficulty.Advanced => _instantiator.Instantiate<AdvancedAIPlayer>(new object[] { playerId, playerName }),
                _ => _instantiator.Instantiate<BeginnerAIPlayer>(new object[] { playerId, playerName })
            };

            return aiPlayer;
        }

        public IAIPlayer Create(AIDifficulty difficulty, int playerId, string name)
        {
            return CreateAIPlayerInterface(playerId, name, difficulty);
        }
    }
}