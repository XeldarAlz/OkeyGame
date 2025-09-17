using Runtime.Core.SignalCenter;
using Runtime.Core.Utilities;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using Runtime.Infrastructure.Persistence;
using Runtime.Services.Audio;
using Runtime.Services.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Runtime.Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Installing global services...");
            
            InstallInfrastructureServices();
            InstallUtilityProviders();
            
            Debug.Log("[ProjectInstaller] Global services installed successfully");
        }

        private void InstallInfrastructureServices()
        {
            Container.Bind<IAssetService>().To<AddressableAssetService>().AsSingle().NonLazy();
            Container.Bind<ILocalizationService>().To<UnityLocalizationService>().AsSingle().NonLazy();
            Container.Bind<ISceneNavigator>().To<SceneNavigator>().AsSingle().NonLazy();
            Container.Bind<IPersistenceService>().To<PersistenceService>().AsSingle().NonLazy();
            Container.Bind<IAudioService>().To<AudioService>().AsSingle().NonLazy();
            Container.Bind<ISignalCenter>().To<SignalCenter>().AsSingle().NonLazy();
            
            Debug.Log("[ProjectInstaller] Infrastructure services bound");
        }

        private void InstallUtilityProviders()
        {
            Container.Bind<ITimeProvider>().To<UnityTimeProvider>().AsSingle();
            Container.Bind<IRandomProvider>().To<UnityRandomProvider>().AsSingle();
            
            Debug.Log("[ProjectInstaller] Utility providers bound");
        }
    }
}
