using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

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

        [Header("Texts")] 
        [SerializeField] private TMP_Text _versionText;

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
            _singlePlayerButton?.onClick.AddListener(() => OnSinglePlayerClicked?.Invoke());
            _multiplayerButton?.onClick.AddListener(() => OnMultiplayerClicked?.Invoke());
            _settingsButton?.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            _exitButton?.onClick.AddListener(() => OnExitClicked?.Invoke());
        }

        public void ShowMainPanel()
        {
            _mainPanel?.SetActive(true);
            _settingsPanel?.SetActive(false);
        }

        public void ShowSettingsPanel()
        {
            _mainPanel?.SetActive(false);
            _settingsPanel?.SetActive(true);
        }

        private void UnsubscribeFromButtonEvents()
        {
            _singlePlayerButton?.onClick.RemoveAllListeners();
            _multiplayerButton?.onClick.RemoveAllListeners();
            _settingsButton?.onClick.RemoveAllListeners();
            _exitButton?.onClick.RemoveAllListeners();
        }

        protected override void Cleanup()
        {
            UnsubscribeFromButtonEvents();
            base.Cleanup();
        }
    }
}