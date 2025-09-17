using Runtime.Infrastructure.Persistence;
using Runtime.Services.AI;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.Score;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Tiles;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.Validation;
using Runtime.Presentation.Controllers;
using UnityEngine;
using Zenject;
using IPersistenceService = Runtime.Infrastructure.Persistence.IPersistenceService;

namespace Runtime.Installers
{
    public sealed class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] Installing all game services...");
            
            InstallCoreGameServices();
            InstallGameFlowServices();
            InstallAIServices();
            InstallControllers();
            // InstallFactories();
            
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

        private void InstallGameFlowServices()
        {
            // Phase 7 Game Flow Services
            Container.Bind<IWinConditionService>()
                .To<WinConditionService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IScoreCalculationService>()
                .To<ScoreCalculationService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IGameStatePersistenceService>()
                .To<GameStatePersistenceService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<GameInitializationService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<TurnBasedGameLoop>()
                .AsSingle()
                .NonLazy();

            Debug.Log("[GameInstaller] Game flow services bound");
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

        private void InstallControllers()
        {
            // Game Controllers
            Container.Bind<GameController>()
                .AsSingle()
                .NonLazy();

            Debug.Log("[GameInstaller] Controllers bound");
        }

        // private void InstallFactories()
        // {
        //     // AI Player Factory
        //     Container.Bind<IAIPlayerFactory>()
        //         .To<AIPlayerFactory>()
        //         .AsSingle()
        //         .NonLazy();
        //
        //     Container.BindFactory<AIDifficulty, int, string, IAIPlayer, AIPlayerFactory>();
        //
        //     Debug.Log("[GameInstaller] Factories bound");
        // }
    }
}
