using Runtime.Domain.Enums;
using UnityEngine;
using Zenject;

namespace Runtime.Services.AI
{
    public class AIPlayerFactory : IFactory<AIDifficulty, int, string, IAIPlayer>
    {
        private readonly IInstantiator _instantiator;
        
        public AIPlayerFactory(IInstantiator instantiator)
        {
            _instantiator = instantiator;
        }

        public IAIPlayer Create(AIDifficulty difficulty, int playerId, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"AI Player {playerId}";
            }

            IAIPlayer aiPlayer = difficulty switch
            {
                AIDifficulty.Beginner => _instantiator.Instantiate<BeginnerAIPlayer>(new object[] { playerId, name }),
                AIDifficulty.Intermediate => _instantiator.Instantiate<IntermediateAIPlayer>(new object[] { playerId, name }),
                AIDifficulty.Advanced => _instantiator.Instantiate<AdvancedAIPlayer>(new object[] { playerId, name }),
                _ => _instantiator.Instantiate<BeginnerAIPlayer>(new object[] { playerId, name })
            };

            Debug.Log($"[AIPlayerFactory] Created {difficulty} AI Player: {name} (ID: {playerId})");
            return aiPlayer;
        }
    }
}