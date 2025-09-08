using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using UnityEngine;
using Zenject;

namespace Runtime.Installers
{
    public sealed class InitializationInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[InitializationInstaller] Installing initialization services...");
            
            // Infrastructure Services
            Container.Bind<IAssetService>()
                .To<AddressableAssetService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<ILocalizationService>()
                .To<UnityLocalizationService>()
                .AsSingle()
                .NonLazy();
            
            Debug.Log("[InitializationInstaller] Initialization services installed successfully");
        }
    }
}
