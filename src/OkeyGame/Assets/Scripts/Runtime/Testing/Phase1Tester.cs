using Cysharp.Threading.Tasks;
using Runtime.Core.Utilities;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using UnityEngine;
using Zenject;

namespace Runtime.Testing
{
    public sealed class Phase1Tester : MonoBehaviour
    {
        [Inject] private IAssetService _assetService;
        [Inject] private ILocalizationService _localizationService;
        [Inject] private ITimeProvider _timeProvider;
        [Inject] private IRandomProvider _randomProvider;

        [SerializeField] private bool _runTestsOnStart = true;

        private async void Start()
        {
            if (_runTestsOnStart)
            {
                await RunPhase1TestsAsync();
            }
        }

        [ContextMenu("Run Phase 1 Tests")]
        public async void RunPhase1TestsManually()
        {
            await RunPhase1TestsAsync();
        }

        private async UniTask RunPhase1TestsAsync()
        {
            Debug.Log("=== PHASE 1 FOUNDATION TESTS ===");

            await TestAssetService();
            await TestLocalizationService();
            TestUtilityProviders();

            Debug.Log("=== PHASE 1 TESTS COMPLETED ===");
        }

        private async UniTask TestAssetService()
        {
            Debug.Log("[TEST] Asset Service...");
            
            if (_assetService != null)
            {
                Debug.Log("✅ Asset Service injected successfully");
                
                // Test initialization
                await _assetService.InitializeAsync();
                Debug.Log("✅ Asset Service initialized");
            }
            else
            {
                Debug.LogError("❌ Asset Service not injected");
            }
        }

        private async UniTask TestLocalizationService()
        {
            Debug.Log("[TEST] Localization Service...");
            
            if (_localizationService != null)
            {
                Debug.Log("✅ Localization Service injected successfully");
                
                // Test initialization
                await _localizationService.InitializeAsync();
                Debug.Log("✅ Localization Service initialized");
                
                // Test localization
                string gameTitle = await _localizationService.GetLocalizedTextAsync("game_title");
                Debug.Log($"✅ Localized text retrieved: {gameTitle}");
            }
            else
            {
                Debug.LogError("❌ Localization Service not injected");
            }
        }

        private void TestUtilityProviders()
        {
            Debug.Log("[TEST] Utility Providers...");
            
            if (_timeProvider != null)
            {
                Debug.Log($"✅ Time Provider: Current time = {_timeProvider.Time}");
            }
            else
            {
                Debug.LogError("❌ Time Provider not injected");
            }
            
            if (_randomProvider != null)
            {
                float randomValue = _randomProvider.Value;
                Debug.Log($"✅ Random Provider: Random value = {randomValue}");
            }
            else
            {
                Debug.LogError("❌ Random Provider not injected");
            }
        }
    }
}
