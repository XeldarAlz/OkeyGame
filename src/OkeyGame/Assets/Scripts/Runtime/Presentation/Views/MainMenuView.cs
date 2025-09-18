using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Runtime.Core.SignalCenter;
using Runtime.Core.Signals;
using Zenject;

namespace Runtime.Presentation.Views
{
    public sealed class MainMenuView : BaseView
    {
        private ISignalCenter _signalCenter;

        [Header("Main Menu Buttons")] 
        [SerializeField] private Button _singlePlayerButton;

        [SerializeField] private Button _multiplayerButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("UI Panels")] 
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Settings")] 
        [SerializeField] private Button _settingsBackButton;

        [Header("Texts")] 
        [SerializeField] private TMP_Text _versionText;

        [Inject]
        private void Construct(ISignalCenter signalCenter)
        {
            _signalCenter = signalCenter;
        }

        protected override void Initialize()
        {
            base.Initialize();
            SubscribeToButtonEvents();
            ShowMainPanel();
        }

        private void SubscribeToButtonEvents()
        {
            _singlePlayerButton?.onClick.AddListener(() => _signalCenter.Fire(new MainMenuSinglePlayerClickedSignal()));
            _multiplayerButton?.onClick.AddListener(() => _signalCenter.Fire(new MainMenuMultiplayerClickedSignal()));
            _settingsButton?.onClick.AddListener(() => _signalCenter.Fire(new MainMenuSettingsClickedSignal()));
            _exitButton?.onClick.AddListener(() => _signalCenter.Fire(new MainMenuExitClickedSignal()));
            _settingsBackButton?.onClick.AddListener(() => _signalCenter.Fire(new MainMenuBackFromSettingsClickedSignal()));
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
            _settingsBackButton?.onClick.RemoveAllListeners();
        }

        protected override void Cleanup()
        {
            UnsubscribeFromButtonEvents();
            base.Cleanup();
        }
    }
}