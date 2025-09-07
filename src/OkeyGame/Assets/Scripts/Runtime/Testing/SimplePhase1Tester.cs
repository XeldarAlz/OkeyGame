using Cysharp.Threading.Tasks;
using Runtime.Core.Utilities;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Localization;
using UnityEngine;
using Zenject;

namespace Runtime.Testing
{
    /// <summary>
    /// Simplified Phase 1 tester that works without ProjectContext dependencies
    /// Use this for quick testing in any scene with SceneContext
    /// </summary>
    public sealed class SimplePhase1Tester : MonoBehaviour
    {
        [Inject(Optional = true)] private IAssetService _assetService;
        [Inject(Optional = true)] private ILocalizationService _localizationService;
        [Inject(Optional = true)] private ITimeProvider _timeProvider;
        [Inject(Optional = true)] private IRandomProvider _randomProvider;

        [SerializeField] private bool _runTestsOnStart = true;

        private async void Start()
        {
            if (_runTestsOnStart)
            {
                await RunSimpleTestsAsync();
            }
        }

        [ContextMenu("Run Simple Tests")]
        public async void RunSimpleTestsManually()
        {
            await RunSimpleTestsAsync();
        }

        private async UniTask RunSimpleTestsAsync()
        {
            Debug.Log("=== SIMPLE PHASE 1 TESTS ===");

            TestBasicInjection();
            await TestAvailableServicesAsync();

            Debug.Log("=== SIMPLE TESTS COMPLETED ===");
        }

        private void TestBasicInjection()
        {
            Debug.Log("[TEST] Basic Dependency Injection...");

            int injectedCount = 0;

            if (_assetService != null)
            {
                Debug.Log("✅ Asset Service injected");
                injectedCount++;
            }
            else
            {
                Debug.Log("⚠️ Asset Service not available (requires ProjectContext)");
            }

            if (_localizationService != null)
            {
                Debug.Log("✅ Localization Service injected");
                injectedCount++;
            }
            else
            {
                Debug.Log("⚠️ Localization Service not available (requires ProjectContext)");
            }

            if (_timeProvider != null)
            {
                Debug.Log("✅ Time Provider injected");
                injectedCount++;
            }
            else
            {
                Debug.Log("⚠️ Time Provider not available (requires ProjectContext)");
            }

            if (_randomProvider != null)
            {
                Debug.Log("✅ Random Provider injected");
                injectedCount++;
            }
            else
            {
                Debug.Log("⚠️ Random Provider not available (requires ProjectContext)");
            }

            Debug.Log($"📊 Injection Summary: {injectedCount}/4 services injected");
        }

        private async UniTask TestAvailableServicesAsync()
        {
            Debug.Log("[TEST] Available Services...");

            if (_timeProvider != null)
            {
                Debug.Log($"⏰ Current Time: {_timeProvider.Time:F2}");
                Debug.Log($"⏰ Delta Time: {_timeProvider.DeltaTime:F4}");
            }

            if (_randomProvider != null)
            {
                float randomValue = _randomProvider.Value;
                int randomRange = _randomProvider.Range(1, 100);
                Debug.Log($"🎲 Random Value: {randomValue:F3}");
                Debug.Log($"🎲 Random Range (1-100): {randomRange}");
            }

            if (_assetService != null)
            {
                try
                {
                    await _assetService.InitializeAsync();
                    Debug.Log("📦 Asset Service initialized successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"📦 Asset Service initialization failed: {ex.Message}");
                }
            }

            if (_localizationService != null)
            {
                try
                {
                    await _localizationService.InitializeAsync();
                    Debug.Log("🌐 Localization Service initialized successfully");
                    
                    // Test basic localization
                    string testText = await _localizationService.GetLocalizedTextAsync("test_key");
                    Debug.Log($"🌐 Localization test: {testText}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"🌐 Localization Service test failed: {ex.Message}");
                }
            }
        }
    }
}
