using System;
using UnityEngine;
using UnityEngine.UI;
using Runtime.Domain.ValueObjects;
using Cysharp.Threading.Tasks;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class DropZoneFeedback : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private ParticleSystem _particleEffect;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Feedback Configuration")]
        [SerializeField] private DropZoneVisualConfig _validConfig;
        [SerializeField] private DropZoneVisualConfig _invalidConfig;
        [SerializeField] private DropZoneVisualConfig _hoverConfig;
        [SerializeField] private DropZoneVisualConfig _snapPreviewConfig;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.15f;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseIntensity = 0.3f;

        private DropZoneState _currentState;
        private GridPosition _gridPosition;
        private bool _isVisible;
        private bool _isAnimating;

        public event Action<DropZoneFeedback, DropZoneState> OnStateChanged;

        public DropZoneState CurrentState => _currentState;
        public GridPosition GridPosition => _gridPosition;
        public bool IsVisible => _isVisible;
        public bool IsAnimating => _isAnimating;

        [Serializable]
        public sealed class DropZoneVisualConfig
        {
            [Header("Colors")]
            public Color backgroundColor = Color.white;
            public Color borderColor = Color.black;
            
            [Header("Scale and Alpha")]
            public Vector3 scale = Vector3.one;
            public float alpha = 0.7f;
            public float borderWidth = 2f;

            [Header("Effects")]
            public bool enablePulse = false;
            public bool enableParticles = false;
            public Color particleColor = Color.white;
            public float particleEmissionRate = 10f;
        }

        public enum DropZoneState
        {
            Hidden,
            Valid,
            Invalid,
            Hover,
            SnapPreview
        }

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            InitializeVisualComponents();
            SetState(DropZoneState.Hidden);
        }

        private void InitializeVisualComponents()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_particleEffect != null)
            {
                _particleEffect.Stop();
            }
        }

        public void Initialize(GridPosition gridPosition)
        {
            _gridPosition = gridPosition;
        }

        public async void SetState(DropZoneState newState)
        {
            if (_currentState == newState || _isAnimating)
            {
                return;
            }

            DropZoneState previousState = _currentState;
            _currentState = newState;

            DropZoneVisualConfig targetConfig = GetConfigForState(newState);
            
            if (newState == DropZoneState.Hidden)
            {
                await HideAsync();
            }
            else
            {
                await ShowWithConfigAsync(targetConfig);
            }

            OnStateChanged?.Invoke(this, newState);
        }

        private DropZoneVisualConfig GetConfigForState(DropZoneState state)
        {
            return state switch
            {
                DropZoneState.Valid => _validConfig,
                DropZoneState.Invalid => _invalidConfig,
                DropZoneState.Hover => _hoverConfig,
                DropZoneState.SnapPreview => _snapPreviewConfig,
                _ => null
            };
        }

        private async UniTask ShowWithConfigAsync(DropZoneVisualConfig config)
        {
            if (config == null)
            {
                return;
            }

            _isAnimating = true;

            // Apply visual configuration
            ApplyVisualConfig(config);

            // Fade in animation
            if (_canvasGroup != null)
            {
                float elapsedTime = 0f;
                float startAlpha = _canvasGroup.alpha;

                while (elapsedTime < _fadeInDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / _fadeInDuration;
                    float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                    
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, config.alpha, easedProgress);
                    await UniTask.Yield();
                }

                _canvasGroup.alpha = config.alpha;
            }

            _isVisible = true;
            _isAnimating = false;

            // Start continuous effects
            if (config.enablePulse)
            {
                StartPulseEffect().Forget();
            }

            if (config.enableParticles && _particleEffect != null)
            {
                StartParticleEffect(config);
            }
        }

        private async UniTask HideAsync()
        {
            if (!_isVisible)
            {
                return;
            }

            _isAnimating = true;

            // Stop effects
            StopAllEffects();

            // Fade out animation
            if (_canvasGroup != null)
            {
                float elapsedTime = 0f;
                float startAlpha = _canvasGroup.alpha;

                while (elapsedTime < _fadeOutDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / _fadeOutDuration;
                    float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                    
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easedProgress);
                    await UniTask.Yield();
                }

                _canvasGroup.alpha = 0f;
            }

            _isVisible = false;
            _isAnimating = false;
        }

        private void ApplyVisualConfig(DropZoneVisualConfig config)
        {
            if (config == null)
            {
                return;
            }

            // Apply colors
            if (_backgroundImage != null)
            {
                _backgroundImage.color = config.backgroundColor;
            }

            if (_borderImage != null)
            {
                _borderImage.color = config.borderColor;
            }

            // Apply scale
            transform.localScale = config.scale;
        }

        private async UniTaskVoid StartPulseEffect()
        {
            Vector3 originalScale = transform.localScale;
            
            while (_isVisible && _currentState != DropZoneState.Hidden)
            {
                float time = Time.time * _pulseSpeed;
                float pulseValue = Mathf.Sin(time) * _pulseIntensity;
                float scaleMultiplier = 1f + pulseValue;
                
                transform.localScale = originalScale * scaleMultiplier;
                
                await UniTask.Yield();
            }

            transform.localScale = originalScale;
        }

        private void StartParticleEffect(DropZoneVisualConfig config)
        {
            if (_particleEffect == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _particleEffect.main;
            main.startColor = config.particleColor;

            ParticleSystem.EmissionModule emission = _particleEffect.emission;
            emission.rateOverTime = config.particleEmissionRate;

            _particleEffect.Play();
        }

        private void StopAllEffects()
        {
            if (_particleEffect != null)
            {
                _particleEffect.Stop();
            }
        }

        public void SetValidDropZone()
        {
            SetState(DropZoneState.Valid);
        }

        public void SetInvalidDropZone()
        {
            SetState(DropZoneState.Invalid);
        }

        public void SetHoverState()
        {
            SetState(DropZoneState.Hover);
        }

        public void SetSnapPreview()
        {
            SetState(DropZoneState.SnapPreview);
        }

        public void Hide()
        {
            SetState(DropZoneState.Hidden);
        }

        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public async UniTask PlayHighlightAnimationAsync(float duration = 0.5f)
        {
            if (!_isVisible)
            {
                return;
            }

            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            Color originalColor = _backgroundImage != null ? _backgroundImage.color : Color.white;
            Color targetColor = Color.Lerp(originalColor, Color.white, 0.5f);

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;
                float animationValue = Mathf.Sin(normalizedTime * Mathf.PI);

                // Animate scale
                transform.localScale = Vector3.Lerp(originalScale, targetScale, animationValue);

                // Animate color
                if (_backgroundImage != null)
                {
                    _backgroundImage.color = Color.Lerp(originalColor, targetColor, animationValue);
                }

                await UniTask.Yield();
            }

            // Reset to original values
            transform.localScale = originalScale;
            if (_backgroundImage != null)
            {
                _backgroundImage.color = originalColor;
            }
        }

        private void OnDestroy()
        {
            StopAllEffects();
        }

        private void OnValidate()
        {
            if (_fadeInDuration < 0f)
            {
                _fadeInDuration = 0f;
            }

            if (_fadeOutDuration < 0f)
            {
                _fadeOutDuration = 0f;
            }

            if (_pulseSpeed < 0f)
            {
                _pulseSpeed = 0f;
            }

            if (_pulseIntensity < 0f)
            {
                _pulseIntensity = 0f;
            }

            // Ensure we have default configs
            if (_validConfig == null)
            {
                _validConfig = new DropZoneVisualConfig
                {
                    backgroundColor = Color.green,
                    borderColor = Color.white,
                    alpha = 0.7f,
                    enablePulse = true
                };
            }

            if (_invalidConfig == null)
            {
                _invalidConfig = new DropZoneVisualConfig
                {
                    backgroundColor = Color.red,
                    borderColor = Color.white,
                    alpha = 0.7f,
                    enablePulse = false
                };
            }

            if (_hoverConfig == null)
            {
                _hoverConfig = new DropZoneVisualConfig
                {
                    backgroundColor = Color.yellow,
                    borderColor = Color.white,
                    alpha = 0.8f,
                    scale = Vector3.one * 1.1f,
                    enablePulse = true
                };
            }

            if (_snapPreviewConfig == null)
            {
                _snapPreviewConfig = new DropZoneVisualConfig
                {
                    backgroundColor = Color.cyan,
                    borderColor = Color.white,
                    alpha = 0.9f,
                    scale = Vector3.one * 1.05f,
                    enableParticles = true
                };
            }
        }
    }
}
