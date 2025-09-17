using System;
using UnityEngine;
using UnityEngine.UI;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Cysharp.Threading.Tasks;
using TMPro;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class TileView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private Image _colorIndicator;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Visual States")]
        [SerializeField] private TileVisualConfig _normalConfig;
        [SerializeField] private TileVisualConfig _selectedConfig;
        [SerializeField] private TileVisualConfig _draggingConfig;
        [SerializeField] private TileVisualConfig _hoveringConfig;
        [SerializeField] private TileVisualConfig _snappingConfig;
        [SerializeField] private TileVisualConfig _invalidConfig;

        [Header("Animation Settings")]
        [SerializeField] private float _stateTransitionDuration = 0.15f;
        [SerializeField] private AnimationCurve _stateTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Color Mapping")]
        [SerializeField] private Color _redColor = Color.red;
        [SerializeField] private Color _yellowColor = Color.yellow;
        [SerializeField] private Color _blueColor = Color.blue;
        [SerializeField] private Color _blackColor = Color.black;
        [SerializeField] private Color _jokerColor = Color.magenta;
        
        private OkeyPiece _associatedPiece;
        private DragState _currentVisualState;
        private DraggableTile _draggableTile;
        private bool _isAnimating;
        public event Action<TileView, DragState> OnVisualStateChanged;
        public OkeyPiece AssociatedPiece => _associatedPiece;
        public DragState CurrentVisualState => _currentVisualState;
        public bool IsAnimating => _isAnimating;

        [Serializable]
        public sealed class TileVisualConfig
        {
            [Header("Colors")] 
            public Color backgroundColor = Color.white;
            public Color borderColor = Color.black;
            public Color textColor = Color.black;
            
            [Header("Scale and Alpha")] 
            public Vector3 scale = Vector3.one;
            public float alpha = 1f;
            public float borderWidth = 2f;
            
            [Header("Effects")] 
            public bool glowEffect = false;
            public Color glowColor = Color.white;
            public float glowIntensity = 1f;
        }

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _draggableTile = GetComponent<DraggableTile>();
            if (_draggableTile != null)
            {
                _draggableTile.OnDragStateChanged += HandleDragStateChanged;
            }
        }

        private void Start()
        {
            SetVisualState(DragState.None);
        }

        public void Initialize(OkeyPiece piece)
        {
            _associatedPiece = piece;
            UpdateTileDisplay();
            SetVisualState(DragState.None);
        }

        private void UpdateTileDisplay()
        {
            if (_associatedPiece == null)
            {
                return;
            }

            // Update number text
            if (_numberText != null)
            {
                if (_associatedPiece.PieceType == OkeyPieceType.FalseJoker)
                {
                    _numberText.text = "★";
                }
                else if (_associatedPiece.IsJoker)
                {
                    _numberText.text = "J";
                }
                else
                {
                    _numberText.text = _associatedPiece.Number.ToString();
                }
            }

            // Update color indicator
            if (_colorIndicator != null)
            {
                _colorIndicator.color = GetColorForOkeyColor(_associatedPiece.Color);
            }
        }

        private Color GetColorForOkeyColor(OkeyColor okeyColor)
        {
            return okeyColor switch
            {
                OkeyColor.Red => _redColor,
                OkeyColor.Yellow => _yellowColor,
                OkeyColor.Blue => _blackColor,
                OkeyColor.Black => _blueColor,
                _ => _jokerColor
            };
        }

        private void HandleDragStateChanged(DraggableTile tile, DragState newState)
        {
            SetVisualState(newState);
        }

        public async void SetVisualState(DragState newState)
        {
            if (_currentVisualState == newState || _isAnimating)
            {
                return;
            }

            DragState previousState = _currentVisualState;
            _currentVisualState = newState;
            TileVisualConfig targetConfig = GetConfigForState(newState);
            
            if (targetConfig != null)
            {
                await AnimateToConfigAsync(targetConfig);
            }

            OnVisualStateChanged?.Invoke(this, newState);
        }

        private TileVisualConfig GetConfigForState(DragState state)
        {
            return state switch
            {
                DragState.None => _normalConfig,
                DragState.Selected => _selectedConfig,
                DragState.Dragging => _draggingConfig,
                DragState.Hovering => _hoveringConfig,
                DragState.Snapping => _snappingConfig,
                _ => _normalConfig
            };
        }

        private async UniTask AnimateToConfigAsync(TileVisualConfig targetConfig)
        {
            if (targetConfig == null)
            {
                return;
            }

            _isAnimating = true;

            // Store initial values
            Color initialBackgroundColor = _backgroundImage != null ? _backgroundImage.color : Color.white;
            Color initialBorderColor = _borderImage != null ? _borderImage.color : Color.black;
            Color initialTextColor = _numberText != null ? _numberText.color : Color.black;
            Vector3 initialScale = transform.localScale;
            float initialAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            float elapsedTime = 0f;
            
            while (elapsedTime < _stateTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _stateTransitionDuration;
                float easedTime = _stateTransitionCurve.Evaluate(normalizedTime);

                // Animate colors
                if (_backgroundImage != null)
                {
                    _backgroundImage.color =
                        Color.Lerp(initialBackgroundColor, targetConfig.backgroundColor, easedTime);
                }

                if (_borderImage != null)
                {
                    _borderImage.color = Color.Lerp(initialBorderColor, targetConfig.borderColor, easedTime);
                }

                if (_numberText != null)
                {
                    _numberText.color = Color.Lerp(initialTextColor, targetConfig.textColor, easedTime);
                }

                // Animate scale
                transform.localScale = Vector3.Lerp(initialScale, targetConfig.scale, easedTime);

                // Animate alpha
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Lerp(initialAlpha, targetConfig.alpha, easedTime);
                }

                await UniTask.Yield();
            }

            // Ensure final values
            if (_backgroundImage != null)
            {
                _backgroundImage.color = targetConfig.backgroundColor;
            }

            if (_borderImage != null)
            {
                _borderImage.color = targetConfig.borderColor;
            }

            if (_numberText != null)
            {
                _numberText.color = targetConfig.textColor;
            }

            transform.localScale = targetConfig.scale;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = targetConfig.alpha;
            }

            _isAnimating = false;
        }

        public void SetInvalidState(bool isInvalid)
        {
            if (isInvalid)
            {
                SetVisualState(DragState.None); // Use a custom invalid state if needed
                ApplyConfigImmediate(_invalidConfig);
            }
            else
            {
                SetVisualState(_currentVisualState);
            }
        }

        private void ApplyConfigImmediate(TileVisualConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = config.backgroundColor;
            }

            if (_borderImage != null)
            {
                _borderImage.color = config.borderColor;
            }

            if (_numberText != null)
            {
                _numberText.color = config.textColor;
            }

            transform.localScale = config.scale;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = config.alpha;
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
            {
                SetVisualState(DragState.Selected);
            }
            else if (_currentVisualState == DragState.Selected)
            {
                SetVisualState(DragState.None);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }
            
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        public async UniTask PlayPulseAnimationAsync(float duration = 0.5f, float intensity = 0.1f)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * (1f + intensity);
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;
                float pulseValue = Mathf.Sin(normalizedTime * Mathf.PI * 2f);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, pulseValue * intensity);
                await UniTask.Yield();
            }

            transform.localScale = originalScale;
        }

        public async UniTask PlayShakeAnimationAsync(float duration = 0.3f, float intensity = 5f)
        {
            Vector3 originalPosition = transform.localPosition;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;
                float shakeIntensity = intensity * (1f - normalizedTime);
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                    UnityEngine.Random.Range(-shakeIntensity, shakeIntensity), 0f);
                transform.localPosition = originalPosition + randomOffset;
                await UniTask.Yield();
            }

            transform.localPosition = originalPosition;
        }

        public void UpdatePieceData(OkeyPiece newPiece)
        {
            _associatedPiece = newPiece;
            UpdateTileDisplay();
        }

        public void ResetVisualState()
        {
            SetVisualState(DragState.None);
        }

        private void OnDestroy()
        {
            if (_draggableTile != null)
            {
                _draggableTile.OnDragStateChanged -= HandleDragStateChanged;
            }
        }

        private void OnValidate()
        {
            if (_stateTransitionDuration < 0f)
            {
                _stateTransitionDuration = 0f;
            }

            _normalConfig ??= new TileVisualConfig();

            _selectedConfig ??= new TileVisualConfig { backgroundColor = Color.cyan, scale = Vector3.one * 1.05f };

            _draggingConfig ??= new TileVisualConfig { alpha = 0.8f, scale = Vector3.one * 1.1f };
        }
    }
}