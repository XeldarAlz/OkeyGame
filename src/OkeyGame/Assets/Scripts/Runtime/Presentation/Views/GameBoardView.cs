using UnityEngine;
using UnityEngine.UI;
using System;

namespace Runtime.Presentation.Views
{
    public sealed class GameBoardView : BaseView
    {
        [Header("Game UI Elements")]
        [SerializeField] private GameObject _playerRackArea;
        [SerializeField] private GameObject _centerGameArea;
        [SerializeField] private GameObject _opponentRacksArea;
        
        [Header("Game Controls")]
        [SerializeField] private Button _drawButton;
        [SerializeField] private Button _discardButton;
        [SerializeField] private Button _winButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _backToMenuButton;
        
        [Header("Game Information")]
        [SerializeField] private GameObject _scorePanel;
        [SerializeField] private GameObject _turnIndicator;
        [SerializeField] private GameObject _okeyIndicator;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;

        public event Action OnDrawClicked;
        public event Action OnDiscardClicked;
        public event Action OnWinClicked;
        public event Action OnPauseClicked;
        public event Action OnBackToMenuClicked;
        public event Action OnResumeClicked;

        protected override void Initialize()
        {
            base.Initialize();
            SubscribeToButtonEvents();
            ShowGamePanel();
        }

        private void SubscribeToButtonEvents()
        {
            if (_drawButton != null)
            {
                _drawButton.onClick.AddListener(() => OnDrawClicked?.Invoke());
            }

            if (_discardButton != null)
            {
                _discardButton.onClick.AddListener(() => OnDiscardClicked?.Invoke());
            }

            if (_winButton != null)
            {
                _winButton.onClick.AddListener(() => OnWinClicked?.Invoke());
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(() => OnPauseClicked?.Invoke());
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.onClick.AddListener(() => OnBackToMenuClicked?.Invoke());
            }
        }

        private void UnsubscribeFromButtonEvents()
        {
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
            }

            if (_discardButton != null)
            {
                _discardButton.onClick.RemoveAllListeners();
            }

            if (_winButton != null)
            {
                _winButton.onClick.RemoveAllListeners();
            }

            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.onClick.RemoveAllListeners();
            }
        }

        public void ShowGamePanel()
        {
            SetPanelVisibility(_gamePanel, true);
            SetPanelVisibility(_pausePanel, false);
            SetPanelVisibility(_gameOverPanel, false);
        }

        public void ShowPausePanel()
        {
            SetPanelVisibility(_gamePanel, false);
            SetPanelVisibility(_pausePanel, true);
            SetPanelVisibility(_gameOverPanel, false);
        }

        public void ShowGameOverPanel()
        {
            SetPanelVisibility(_gamePanel, false);
            SetPanelVisibility(_pausePanel, false);
            SetPanelVisibility(_gameOverPanel, true);
        }

        private void SetPanelVisibility(GameObject panel, bool isVisible)
        {
            if (panel != null)
            {
                panel.SetActive(isVisible);
            }
        }

        public void SetDrawButtonEnabled(bool enabled)
        {
            if (_drawButton != null)
            {
                _drawButton.interactable = enabled;
            }
        }

        public void SetDiscardButtonEnabled(bool enabled)
        {
            if (_discardButton != null)
            {
                _discardButton.interactable = enabled;
            }
        }

        public void SetWinButtonEnabled(bool enabled)
        {
            if (_winButton != null)
            {
                _winButton.interactable = enabled;
            }
        }

        public GameObject GetPlayerRackArea()
        {
            return _playerRackArea;
        }

        public GameObject GetCenterGameArea()
        {
            return _centerGameArea;
        }

        public GameObject GetOpponentRacksArea()
        {
            return _opponentRacksArea;
        }

        protected override void Cleanup()
        {
            UnsubscribeFromButtonEvents();
            base.Cleanup();
        }
    }
}
