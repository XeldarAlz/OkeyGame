using Cysharp.Threading.Tasks;
using Runtime.Core.Signals;
using System.Threading;
using Runtime.Core.SignalCenter;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Runtime.Services.Navigation
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        private const float MIN_LOADING_DURATION_SEC = 2f;

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

            operation.allowSceneActivation = false;

            CancellationTokenSource existing = _cancellationTokenSource;
            if (!ReferenceEquals(existing, null))
            {
                existing.Cancel();
                existing.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();

            float elapsed = 0f;

            while (operation.progress < 0.9f || elapsed < MIN_LOADING_DURATION_SEC)
            {
                elapsed += Time.unscaledDeltaTime;

                float opNormalized = operation.progress < 0.9f ? operation.progress / 0.9f : 1f;
                float timeNormalized = elapsed / MIN_LOADING_DURATION_SEC;
                float visibleNormalized = timeNormalized < opNormalized ? timeNormalized : opNormalized;
                if (visibleNormalized > 0.99f)
                {
                    visibleNormalized = 0.99f;
                }

                _signalCenter.Fire(new SceneLoadingProgressSignal(visibleNormalized));
                await UniTask.Yield(PlayerLoopTiming.Update, _cancellationTokenSource.Token);
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
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

            CancellationTokenSource existing = _cancellationTokenSource;
            if (ReferenceEquals(existing, null))
            {
                return;
            } 
            
            existing.Cancel(); 
            existing.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();

            while (!operation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _cancellationTokenSource.Token);
            }
        }
    }
}
