using Cysharp.Threading.Tasks;

namespace Runtime.Core.Architecture
{
    public interface IService
    {
    }

    public interface IInitializableService : IService
    {
        UniTask InitializeAsync();
    }

    public interface IDisposableService : IService
    {
        void Dispose();
    }

    public interface IAsyncDisposableService : IService
    {
        UniTask DisposeAsync();
    }
}