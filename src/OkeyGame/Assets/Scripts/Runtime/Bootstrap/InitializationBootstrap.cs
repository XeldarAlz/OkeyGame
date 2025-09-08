using Cysharp.Threading.Tasks;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using Runtime.Services.Navigation;
using UnityEngine;
using Zenject;

namespace Runtime.Bootstrap
{
    public sealed class InitializationBootstrap : MonoBehaviour
    {
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        
        [Inject] private IAssetService _assetService;
        [Inject] private ILocalizationService _localizationService;
        [Inject] private ISceneNavigator _sceneNavigator;

        private async void Start()
        {
            await InitializeServicesAsync();
            await LoadMainMenuAsync();
        }

        private async UniTask InitializeServicesAsync()
        {
            try
            {
                Debug.Log("[InitializationBootstrap] Starting initialization of core services...");
                
                await _assetService.InitializeAsync();
                await _localizationService.InitializeAsync();
                
                Debug.Log("[InitializationBootstrap] Infrastructure services initialized successfully");
                Debug.Log("[InitializationBootstrap] Ready for Phase 3 AI Player System and beyond");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[InitializationBootstrap] Failed to initialize services: {exception.Message}");
            }
        }

        private async UniTask LoadMainMenuAsync()
        {
            try
            {
                Debug.Log($"[InitializationBootstrap] Loading MainMenu scene: {_mainMenuSceneName}");
                await _sceneNavigator.LoadSceneAsync(_mainMenuSceneName);
                Debug.Log("[InitializationBootstrap] MainMenu scene loaded successfully");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[InitializationBootstrap] Failed to load MainMenu scene: {exception.Message}");
            }
        }

        private void OnDestroy()
        {
            _assetService?.Dispose();
        }
    }
}
