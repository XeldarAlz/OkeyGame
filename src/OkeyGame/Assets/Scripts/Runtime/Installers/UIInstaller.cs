using Runtime.Presentation.Presenters;
using Runtime.Presentation.Views;
using UnityEngine;
using Zenject;

namespace Runtime.Installers
{
    public sealed class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[UIInstaller] Installing UI services...");
            
            InstallPresenters();
            
            Debug.Log("[UIInstaller] UI services installed successfully");
        }

        private void InstallPresenters()
        {
            Container.Bind<MainMenuView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuPresenter>().AsSingle().NonLazy();
            
            Container.Bind<SettingsMenuView>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<SettingsMenuPresenter>().AsSingle().NonLazy();
            
            Debug.Log("[UIInstaller] Presenters bound");
        }
    }
}
