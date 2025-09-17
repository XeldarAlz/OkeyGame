using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.Infrastructure.AssetManagement
{
    public sealed class AddressableAssetService : IAssetService
    {
        private readonly Dictionary<string, object> _loadedAssets;
        private readonly List<AsyncOperationHandle> _handles;
        private bool _isInitialized;

        public AddressableAssetService()
        {
            _loadedAssets = new Dictionary<string, object>();
            _handles = new List<AsyncOperationHandle>();
        }

        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                await Addressables.InitializeAsync();
                _isInitialized = true;
                Debug.Log("[AddressableAssetService] Initialized successfully");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AddressableAssetService] Initialization failed: {exception.Message}");
                throw;
            }
        }

        public async UniTask<T> LoadAssetAsync<T>(string key) where T : class
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_loadedAssets.TryGetValue(key, out object cached))
            {
                return cached as T;
            }

            try
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
                _handles.Add(handle);

                // Directly await the AsyncOperationHandle
                await handle;
                T asset = handle.Result;
                _loadedAssets[key] = asset;
                return asset;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[AddressableAssetService] Failed to load asset '{key}': {exception.Message}");
                return null;
            }
        }

        public async UniTask PreloadAssetsAsync(string[] keys)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            UniTask<Object>[] loadTasks = keys.Select(key => LoadAssetAsync<Object>(key)).ToArray();
            await UniTask.WhenAll(loadTasks);
            Debug.Log($"[AddressableAssetService] Preloaded {keys.Length} assets");
        }

        public void ReleaseAsset(string key)
        {
            if (!_loadedAssets.TryGetValue(key, out object asset))
            {
                return;
            }

            for (int index = _handles.Count - 1; index >= 0; index--)
            {
                if (!ReferenceEquals(_handles[index].Result, asset))
                {
                    continue;
                }
                
                Addressables.Release(_handles[index]);
                _handles.RemoveAt(index);
                break;
            }

            _loadedAssets.Remove(key);
        }

        public void ReleaseAllAssets()
        {
            for (int index = 0; index < _handles.Count; index++)
            {
                if (_handles[index].IsValid())
                {
                    Addressables.Release(_handles[index]);
                }
            }

            _handles.Clear();
            _loadedAssets.Clear();
            Debug.Log("[AddressableAssetService] Released all assets");
        }

        public bool IsAssetLoaded(string key)
        {
            return _loadedAssets.ContainsKey(key);
        }

        public void Dispose()
        {
            ReleaseAllAssets();
            Debug.Log("[AddressableAssetService] Disposed");
        }
    }
}