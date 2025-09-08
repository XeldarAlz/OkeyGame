using Cysharp.Threading.Tasks;
using Runtime.Services;
using Runtime.Services.AI;
using Runtime.Services.GameLogic;
using UnityEngine;
using Zenject;

namespace Runtime.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Inject] private IGameStateService _gameStateService;
        [Inject] private ITileService _tileService;
        [Inject] private IGameRulesService _gameRulesService;
        [Inject] private ITurnManager _turnManager;
        [Inject] private IScoreService _scoreService;
        
        // AI services
        [Inject] private IAIDecisionService _aiDecisionService;
        [Inject] private IAIPlayerService _aiPlayerService;
        
        private async void Start()
        {
            await InitializeGameServicesAsync();
        }

        private async UniTask InitializeGameServicesAsync()
        {
            try
            {
                // Game services are already initialized by Zenject
                // This is just a hook for any additional initialization if needed
                
                Debug.Log("[GameBootstrap] Game services initialized successfully");
                Debug.Log("[GameBootstrap] AI Player System (Phase 3) ready for testing");
                
                // Additional game-specific initialization can be added here
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[GameBootstrap] Failed to initialize game services: {exception.Message}");
            }
        }
    }
}
