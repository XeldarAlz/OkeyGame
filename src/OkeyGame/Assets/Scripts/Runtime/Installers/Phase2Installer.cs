using Runtime.Services;
using Runtime.Services.GameLogic;
using Runtime.Services.Validation;
using Zenject;

namespace Runtime.Installers
{
    public sealed class Phase2Installer : MonoInstaller
    {
        public override void InstallBindings()
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

            UnityEngine.Debug.Log("[Phase2Installer] All Phase 2 services registered with dependency injection");
        }
    }
}
