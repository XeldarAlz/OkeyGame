using Cysharp.Threading.Tasks;
using Runtime.Core.Configs;
using Runtime.Presentation.Views;
using Runtime.Services.Navigation;
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

            if (ReferenceEquals(_view, null))
            {
                return;
            }

            _view.OnSinglePlayerClicked += OnSinglePlayerClicked;
            _view.OnMultiplayerClicked += HandleMultiplayerClicked;
            _view.OnSettingsClicked += HandleSettingsClicked;
            _view.OnExitClicked += HandleExitClicked;
            _view.OnBackFromSettingsClicked += HandleBackFromSettingsClicked;
        }

        private void OnSinglePlayerClicked()
        {
            HandleSinglePlayerClicked().Forget();
        }

        private async UniTask HandleSinglePlayerClicked()
        {
            await _sceneNavigator.LoadScene((int)SceneConfigs.PlayScene);
        }

        private void HandleMultiplayerClicked()
        {
            // TODO: Implement multiplayer functionality in future phases
        }

        private void HandleSettingsClicked()
        {
            _view?.ShowSettingsPanel();
        }

        private void HandleExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleBackFromSettingsClicked()
        {
            _view?.ShowMainPanel();
        }

        protected override void UnsubscribeFromEvents()
        {
            if (ReferenceEquals(_view, null))
            {
                return;
            }

            _view.OnSinglePlayerClicked -= OnSinglePlayerClicked;
            _view.OnMultiplayerClicked -= HandleMultiplayerClicked;
            _view.OnSettingsClicked -= HandleSettingsClicked;
            _view.OnExitClicked -= HandleExitClicked;
            _view.OnBackFromSettingsClicked -= HandleBackFromSettingsClicked;

            base.UnsubscribeFromEvents();
        }
    }
}