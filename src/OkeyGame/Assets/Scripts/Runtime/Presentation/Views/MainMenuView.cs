using UnityEngine;
using UnityEngine.UI;
using System;

namespace Runtime.Presentation.Views
{
    public sealed class MainMenuView : BaseView
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button _singlePlayerButton;
        [SerializeField] private Button _multiplayerButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("UI Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;

        public event Action OnSinglePlayerClicked;
        public event Action OnMultiplayerClicked;
        public event Action OnSettingsClicked;
        public event Action OnExitClicked;
        public event Action OnBackFromSettingsClicked;

        protected override void Initialize()
        {
            base.Initialize();
            SubscribeToButtonEvents();
            ShowMainPanel();
        }

        private void SubscribeToButtonEvents()
        {
            if (_singlePlayerButton != null)
            {
                _singlePlayerButton.onClick.AddListener(() => OnSinglePlayerClicked?.Invoke());
            }

            if (_multiplayerButton != null)
            {
                _multiplayerButton.onClick.AddListener(() => OnMultiplayerClicked?.Invoke());
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(() => OnExitClicked?.Invoke());
            }
        }

        private void UnsubscribeFromButtonEvents()
        {
            if (_singlePlayerButton != null)
            {
                _singlePlayerButton.onClick.RemoveAllListeners();
            }

            if (_multiplayerButton != null)
            {
                _multiplayerButton.onClick.RemoveAllListeners();
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveAllListeners();
            }
        }

        public void ShowMainPanel()
        {
            if (_mainPanel != null)
            {
                _mainPanel.SetActive(true);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public void ShowSettingsPanel()
        {
            if (_mainPanel != null)
            {
                _mainPanel.SetActive(false);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }

        protected override void Cleanup()
        {
            UnsubscribeFromButtonEvents();
            base.Cleanup();
        }
    }
}
