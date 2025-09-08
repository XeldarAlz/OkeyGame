using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.Services.Navigation
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        public async UniTask LoadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneNavigator] Scene name cannot be null or empty");
                return;
            }

            try
            {
                Debug.Log($"[SceneNavigator] Loading scene: {sceneName}");
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                Debug.Log($"[SceneNavigator] Successfully loaded scene: {sceneName}");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[SceneNavigator] Failed to load scene '{sceneName}': {exception.Message}");
                throw;
            }
        }

        public async UniTask LoadSceneAdditiveAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneNavigator] Scene name cannot be null or empty");
                return;
            }

            try
            {
                Debug.Log($"[SceneNavigator] Loading scene additively: {sceneName}");
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                Debug.Log($"[SceneNavigator] Successfully loaded scene additively: {sceneName}");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[SceneNavigator] Failed to load scene additively '{sceneName}': {exception.Message}");
                throw;
            }
        }

        public async UniTask UnloadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneNavigator] Scene name cannot be null or empty");
                return;
            }

            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneNavigator] Scene '{sceneName}' is not loaded, cannot unload");
                return;
            }

            try
            {
                Debug.Log($"[SceneNavigator] Unloading scene: {sceneName}");
                await SceneManager.UnloadSceneAsync(sceneName);
                Debug.Log($"[SceneNavigator] Successfully unloaded scene: {sceneName}");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[SceneNavigator] Failed to unload scene '{sceneName}': {exception.Message}");
                throw;
            }
        }

        public string GetCurrentSceneName()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.name;
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }
    }
}
