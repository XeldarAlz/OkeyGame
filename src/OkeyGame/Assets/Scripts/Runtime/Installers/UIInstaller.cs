using Runtime.Presentation.Presenters;
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
            Container.BindInterfacesAndSelfTo<MainMenuPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameBoardPresenter>().AsSingle();
            
            Debug.Log("[UIInstaller] Presenters bound");
        }
    }
}
