using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;

namespace Runtime.Services.Navigation
{
    public interface ISceneNavigator : IService
    {
        UniTask LoadSceneAsync(string sceneName);
        UniTask LoadSceneAdditiveAsync(string sceneName);
        UniTask UnloadSceneAsync(string sceneName);
        string GetCurrentSceneName();
        bool IsSceneLoaded(string sceneName);
    }
}
