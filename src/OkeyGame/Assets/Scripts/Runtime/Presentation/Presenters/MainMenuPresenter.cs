using Cysharp.Threading.Tasks;
using Runtime.Core.Configs;
using Runtime.Core.Navigation;
using Runtime.Presentation.Views;
using Runtime.Core.SignalCenter;
using Runtime.Core.Signals;
using UnityEngine;
using Zenject;

namespace Runtime.Presentation.Presenters
{
    public sealed class MainMenuPresenter : BasePresenter<MainMenuView>
    {
        private readonly ISceneNavigator _sceneNavigator;
        private readonly ISignalCenter _signalCenter;

        [Inject]
        public MainMenuPresenter(ISceneNavigator sceneNavigator, ISignalCenter signalCenter)
        {
            _sceneNavigator = sceneNavigator;
            _signalCenter = signalCenter;
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            _signalCenter.Subscribe<MainMenuSinglePlayerClickedSignal>(OnSinglePlayerClicked);
            _signalCenter.Subscribe<MainMenuMultiplayerClickedSignal>(OnMultiplayerClicked);
            _signalCenter.Subscribe<MainMenuSettingsClickedSignal>(OnSettingsClicked);
            _signalCenter.Subscribe<MainMenuExitClickedSignal>(OnExitClicked);
            _signalCenter.Subscribe<MainMenuBackFromSettingsClickedSignal>(OnBackFromSettingsClicked);
        }

        private void OnSinglePlayerClicked(MainMenuSinglePlayerClickedSignal signal)
        {
            HandleSinglePlayerClicked().Forget();
        }

        private async UniTask HandleSinglePlayerClicked()
        {
            await _sceneNavigator.LoadScene((int)SceneConfigs.PlayScene);
        }

        private void OnMultiplayerClicked(MainMenuMultiplayerClickedSignal signal)
        {
            // TODO: Implement multiplayer functionality in future phases
        }

        private void OnSettingsClicked(MainMenuSettingsClickedSignal signal)
        {
            _view?.ShowSettingsPanel();
        }

        private void OnExitClicked(MainMenuExitClickedSignal signal)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnBackFromSettingsClicked(MainMenuBackFromSettingsClickedSignal signal)
        {
            _view?.ShowMainPanel();
        }

        protected override void UnsubscribeFromEvents()
        {
            _signalCenter.Unsubscribe<MainMenuSinglePlayerClickedSignal>(OnSinglePlayerClicked);
            _signalCenter.Unsubscribe<MainMenuMultiplayerClickedSignal>(OnMultiplayerClicked);
            _signalCenter.Unsubscribe<MainMenuSettingsClickedSignal>(OnSettingsClicked);
            _signalCenter.Unsubscribe<MainMenuExitClickedSignal>(OnExitClicked);
            _signalCenter.Unsubscribe<MainMenuBackFromSettingsClickedSignal>(OnBackFromSettingsClicked);

            base.UnsubscribeFromEvents();
        }
    }
}