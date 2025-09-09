using Cysharp.Threading.Tasks;
using Runtime.Core.Signals;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Runtime.Services.Navigation
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        [Inject]
        private ISignalCenter _signalCenter;
        
        private CancellationTokenSource _cancellationTokenSource;

        public UniTask LoadScene(int sceneIndex, LoadSceneMode mode = LoadSceneMode.Single)
        {
            return LoadSceneInternal(sceneIndex, mode);
        }

        public UniTask UnloadScene(int sceneIndex)
        {
            return UnloadSceneInternal(sceneIndex);
        }

        private async UniTask LoadSceneInternal(int sceneIndex, LoadSceneMode mode)
        {
            _signalCenter.Fire(new SceneLoadingStartedSignal());

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex, mode);
            if (ReferenceEquals(operation, null))
            {
                _signalCenter.Fire(new SceneLoadingCompletedSignal());
                return;
            }

            operation.allowSceneActivation = true;
            _cancellationTokenSource = new CancellationTokenSource();

            while (!operation.isDone)
            {
                float normalized = operation.progress < 0.9f ? operation.progress / 0.9f : 1f;
                _signalCenter.Fire(new SceneLoadingProgressSignal(normalized));
                await UniTask.Yield(PlayerLoopTiming.Update, _cancellationTokenSource.Token);
            }

            _signalCenter.Fire(new SceneLoadingProgressSignal(1f));
            _signalCenter.Fire(new SceneLoadingCompletedSignal());
        }

        private async UniTask UnloadSceneInternal(int sceneIndex)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneIndex);
            if (ReferenceEquals(operation, null))
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            while (!operation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _cancellationTokenSource.Token);
            }
        }
    }
}
