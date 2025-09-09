using Runtime.Core.Configs;
using Runtime.Presentation.Views;
using Runtime.Services.Navigation;
using UnityEngine;
using Zenject;

namespace Runtime.Presentation.Presenters
{
    public sealed class MainMenuPresenter : BasePresenter<MainMenuView>
    {
        private readonly ISceneNavigator _sceneNavigator;

        [Inject]
        public MainMenuPresenter(ISceneNavigator sceneNavigator)
        {
            _sceneNavigator = sceneNavigator;
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            if (_view != null)
            {
                _view.OnSinglePlayerClicked += HandleSinglePlayerClicked;
                _view.OnMultiplayerClicked += HandleMultiplayerClicked;
                _view.OnSettingsClicked += HandleSettingsClicked;
                _view.OnExitClicked += HandleExitClicked;
                _view.OnBackFromSettingsClicked += HandleBackFromSettingsClicked;
            }
        }

        protected override void UnsubscribeFromEvents()
        {
            if (_view != null)
            {
                _view.OnSinglePlayerClicked -= HandleSinglePlayerClicked;
                _view.OnMultiplayerClicked -= HandleMultiplayerClicked;
                _view.OnSettingsClicked -= HandleSettingsClicked;
                _view.OnExitClicked -= HandleExitClicked;
                _view.OnBackFromSettingsClicked -= HandleBackFromSettingsClicked;
            }

            base.UnsubscribeFromEvents();
        }

        private async void HandleSinglePlayerClicked()
        {
            Debug.Log("[MainMenuPresenter] Single Player clicked");
            
            try
            {
                await _sceneNavigator.LoadScene((int)SceneConfigs.PlayScene);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[MainMenuPresenter] Failed to load GameScene: {exception.Message}");
            }
        }

        private void HandleMultiplayerClicked()
        {
            Debug.Log("[MainMenuPresenter] Multiplayer clicked");
            // TODO: Implement multiplayer functionality in future phases
        }

        private void HandleSettingsClicked()
        {
            Debug.Log("[MainMenuPresenter] Settings clicked");
            _view?.ShowSettingsPanel();
        }

        private void HandleExitClicked()
        {
            Debug.Log("[MainMenuPresenter] Exit clicked");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleBackFromSettingsClicked()
        {
            Debug.Log("[MainMenuPresenter] Back from settings clicked");
            _view?.ShowMainPanel();
        }
    }
}
