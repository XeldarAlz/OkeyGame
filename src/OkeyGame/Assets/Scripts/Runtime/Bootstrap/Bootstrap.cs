using Cysharp.Threading.Tasks;
using Runtime.Core.Configs;
using Runtime.Core.Navigation;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using Runtime.Infrastructure.Persistence;
using Runtime.Services.Audio;
using UnityEngine;
using Zenject;

namespace Runtime.Bootstrap
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [Inject] private IAssetService _assetService;
        [Inject] private ILocalizationService _localizationService;
        [Inject] private IAudioService _audioService;
        [Inject] private IPersistenceService _persistenceService;
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
                await _audioService.InitializeAsync();
                await _persistenceService.InitializeAsync();
                Debug.Log("[InitializationBootstrap] Infrastructure services initialized successfully");
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
                Debug.Log($"[InitializationBootstrap] Loading MainMenu scene");
                await _sceneNavigator.LoadScene((int)SceneConfigs.MainMenuScene);
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