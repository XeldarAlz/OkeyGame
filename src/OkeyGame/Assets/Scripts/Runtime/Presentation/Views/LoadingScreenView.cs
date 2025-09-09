using Cysharp.Threading.Tasks;
using Runtime.Core.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Runtime.Presentation.Views
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _loadingCanvasGroup;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private GameObject _loadingSpinner;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.1f;
        [SerializeField] private float _fadeOutDuration = 0.1f;
        
        [Inject] private ISignalCenter _signalCenter;
        
        private bool _isVisible;
        
        private void Awake()
        {
            if (_loadingCanvasGroup == null)
            {
                _loadingCanvasGroup = GetComponent<CanvasGroup>();
            }
            
            HideImmediate();
        }
        
        private void OnEnable()
        {
            SubscribeToSignals();
        }
        
        private void SubscribeToSignals()
        {
            _signalCenter.Subscribe<SceneLoadingStartedSignal>(OnSceneLoadingStarted);
            _signalCenter.Subscribe<SceneLoadingProgressSignal>(OnSceneLoadingProgress);
            _signalCenter.Subscribe<SceneLoadingCompletedSignal>(OnSceneLoadingCompleted);
        }

        private void OnSceneLoadingStarted(SceneLoadingStartedSignal signal)
        {
            ShowLoadingScreenAsync().Forget();
        }

        private void OnSceneLoadingProgress(SceneLoadingProgressSignal signal)
        {
            SetProgress(signal.Value);
        }

        private void OnSceneLoadingCompleted(SceneLoadingCompletedSignal signal)
        {
            HideLoadingScreenAsync().Forget();
        }

        private async UniTask ShowLoadingScreenAsync()
        {
            if (_isVisible)
            {
                return;
            }
            
            _isVisible = true;
            
            SetProgress(0f);
            _loadingCanvasGroup.blocksRaycasts = true;
            
            await FadeCanvasGroupAsync(_loadingCanvasGroup, 0f, 1f, _fadeInDuration);
        }
        
        private async UniTask HideLoadingScreenAsync()
        {
            if (!_isVisible)
            {
                return;
            }
            
            await FadeCanvasGroupAsync(_loadingCanvasGroup, 1f, 0f, _fadeOutDuration);
            
            _isVisible = false;
            _loadingCanvasGroup.blocksRaycasts = false;
            _loadingSpinner.SetActive(false);
        }
        
        private void HideImmediate()
        {
            _loadingCanvasGroup.alpha = 0f;
            _loadingCanvasGroup.blocksRaycasts = false;
            _loadingSpinner.SetActive(false);
            _isVisible = false;
        }
        
        private void SetProgress(float progress)
        {
            if (!ReferenceEquals(_progressBar, null))
            {
                _progressBar.value = Mathf.Clamp01(progress);
            }
        }
        
        private void SetLoadingText(string text)
        {
            if (!ReferenceEquals(_loadingText, null))
            {
                _loadingText.text = text;
            }
        }
        
        private async UniTask FadeCanvasGroupAsync(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
        {
            float elapsedTime = 0f;
            canvasGroup.alpha = startAlpha;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                await UniTask.Yield();
            }
            
            canvasGroup.alpha = endAlpha;
        }
        
        private void UnsubscribeFromSignals()
        {
            _signalCenter.Unsubscribe<SceneLoadingStartedSignal>(OnSceneLoadingStarted);
            _signalCenter.Unsubscribe<SceneLoadingProgressSignal>(OnSceneLoadingProgress);
            _signalCenter.Unsubscribe<SceneLoadingCompletedSignal>(OnSceneLoadingCompleted);
        }
        
        private void OnDisable()
        {
            UnsubscribeFromSignals();
        }
    }
}
