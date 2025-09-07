using UnityEngine;
using Zenject;

namespace Runtime.Installers
{
    /// <summary>
    /// GameInstaller - Game-specific services for gameplay scenes
    /// This installer is attached to SceneContext in game scenes
    /// </summary>
    public sealed class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] Installing game services...");
            
            // NOTE: Concrete service implementations will be added in Phase 2
            // This installer is prepared for future service bindings
            
            // TODO: Bind IGameStateService implementation
            // TODO: Bind ITileService implementation
            // TODO: Bind IGameRulesService implementation
            // TODO: Bind ITurnManager implementation
            // TODO: Bind IScoreService implementation
            // TODO: Bind IAIPlayerService implementation
            // TODO: Bind IValidationService implementation
            
            Debug.Log("[GameInstaller] Game services ready for Phase 2 implementation");
        }
    }
}
