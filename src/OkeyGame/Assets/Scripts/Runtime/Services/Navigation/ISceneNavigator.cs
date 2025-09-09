using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Runtime.Services.Navigation
{
    public interface ISceneNavigator
    {
        UniTask LoadScene(int sceneIndex, LoadSceneMode mode = LoadSceneMode.Single);
        UniTask UnloadScene(int sceneIndex);
    }
}
