using Cysharp.Threading.Tasks;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using UnityEngine;
using Zenject;

namespace Runtime.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Inject] private IAssetService _assetService;
       
        [Inject] private ILocalizationService _localizationService;

        private async void Start()
        {
            await InitializeServicesAsync();
        }

        private async UniTask InitializeServicesAsync()
        {
            try
            {
                await _assetService.InitializeAsync();
                await _localizationService.InitializeAsync();
                
                Debug.Log("[GameBootstrap] Infrastructure services initialized successfully");
                Debug.Log("[GameBootstrap] Ready for Phase 2 service implementations");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[GameBootstrap] Failed to initialize services: {exception.Message}");
            }
        }

        private void OnDestroy()
        {
            _assetService?.Dispose();
        }
    }
}
