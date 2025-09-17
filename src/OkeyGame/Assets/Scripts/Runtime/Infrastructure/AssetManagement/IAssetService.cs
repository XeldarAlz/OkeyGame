using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;

namespace Runtime.Infrastructure.AssetManagement
{
    public interface IAssetService : IInitializableService, IDisposableService
    {
        UniTask<T> LoadAssetAsync<T>(string key) where T : class;
        UniTask PreloadAssetsAsync(string[] keys);
        void ReleaseAsset(string key);
        void ReleaseAllAssets();
        bool IsAssetLoaded(string key);
    }
}