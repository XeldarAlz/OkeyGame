using Runtime.Infrastructure.Persistence;
using Runtime.Services.AI;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.Score;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Tiles;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.Validation;
using UnityEngine;
using Zenject;
using IPersistenceService = Runtime.Infrastructure.Persistence.IPersistenceService;

namespace Runtime.Installers
{
    /// <summary>
    /// GameInstaller - Unified installer for all game services
    /// This installer is attached to SceneContext in game scenes
    /// </summary>
    public sealed class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] Installing all game services...");
            
            InstallCoreGameServices();
            InstallAIServices();
            InstallFactories();
            
            Debug.Log("[GameInstaller] All game services installed successfully");
        }

        private void InstallCoreGameServices()
        {
            // Core Game Logic Services
            Container.Bind<ITileService>()
                .To<TileService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IGameRulesService>()
                .To<GameRulesService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IGameStateService>()
                .To<GameStateService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IValidationService>()
                .To<ValidationService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<ITurnManager>()
                .To<TurnManager>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IScoreService>()
                .To<ScoreService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IPersistenceService>()
                .To<PersistenceService>()
                .AsSingle()
                .NonLazy();

            Debug.Log("[GameInstaller] Core game services bound");
        }

        private void InstallAIServices()
        {
            // AI Services
            Container.Bind<IAIDecisionService>()
                .To<AIDecisionService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IAIPlayerService>()
                .To<AIPlayerService>()
                .AsSingle()
                .NonLazy();
                
            // Bind AI player classes
            Container.Bind<BeginnerAIPlayer>().AsTransient();
            Container.Bind<IntermediateAIPlayer>().AsTransient();
            Container.Bind<AdvancedAIPlayer>().AsTransient();

            Debug.Log("[GameInstaller] AI services bound");
        }

        private void InstallFactories()
        {
            // No factories needed for now
            Debug.Log("[GameInstaller] No factories needed at this stage");
        }
    }
}
