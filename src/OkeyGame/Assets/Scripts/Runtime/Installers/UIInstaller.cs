using UnityEngine;
using Zenject;

namespace Runtime.Installers
{
    public sealed class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[UIInstaller] Installing UI services...");
            
            // NOTE: UI Presenters and Controllers will be added in Phase 3
            // This installer is prepared for future UI bindings
            
            // TODO: Bind IMainMenuPresenter implementation
            // TODO: Bind IGameBoardPresenter implementation
            // TODO: Bind IPlayerRackPresenter implementation
            // TODO: Bind IInputController implementation
            // TODO: Bind IUINavigationController implementation
            
            Debug.Log("[UIInstaller] UI services ready for Phase 3 implementation");
        }
    }
}
