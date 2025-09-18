using Cysharp.Threading.Tasks;
using Runtime.Infrastructure.Localization;
using Runtime.Presentation.Views;
using Runtime.Services.Audio;
using UnityEngine;
using Runtime.Core.SignalCenter;
using Runtime.Core.Signals;
using Zenject;

namespace Runtime.Presentation.Presenters
{
    public sealed class SettingsMenuPresenter : BasePresenter<SettingsMenuView>
    {
        private readonly ILocalizationService _localizationService;
        private readonly IAudioService _audioService;
        private readonly ISignalCenter _signalCenter;

        [Inject]
        public SettingsMenuPresenter(ILocalizationService localizationService, IAudioService audioService, ISignalCenter signalCenter)
        {
            _localizationService = localizationService;
            _audioService = audioService;
            _signalCenter = signalCenter;
        }

        protected override void InitializeView()
        {
            if (ReferenceEquals(_view, null))
            {
                return;
            }

            SystemLanguage currentLanguage = _localizationService.GetCurrentLanguage();
            _view.SetLanguage(currentLanguage);

            float musicVolume = _audioService.GetMusicVolume();
            _view.SetMusicVolume(musicVolume);

            float sfxVolume = _audioService.GetSoundVolume();
            _view.SetSfxVolume(sfxVolume);
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            _signalCenter.Subscribe<SettingsLanguageSelectedSignal>(OnLanguageSelected);
            _signalCenter.Subscribe<SettingsMusicVolumeChangedSignal>(OnMusicVolumeChanged);
            _signalCenter.Subscribe<SettingsSfxVolumeChangedSignal>(OnSfxVolumeChanged);
        }

        private void OnLanguageSelected(SettingsLanguageSelectedSignal signal)
        {
            HandleLanguageSelected(signal.Language).Forget();
        }

        private async UniTask HandleLanguageSelected(SystemLanguage language)
        {
            await _localizationService.LoadLanguageAsync(language);
        }

        private void OnMusicVolumeChanged(SettingsMusicVolumeChangedSignal signal)
        {
            _audioService.SetMusicVolume(signal.Value);
        }

        private void OnSfxVolumeChanged(SettingsSfxVolumeChangedSignal signal)
        {
            _audioService.SetSoundVolume(signal.Value);
        }

        protected override void UnsubscribeFromEvents()
        {
            _signalCenter.Unsubscribe<SettingsLanguageSelectedSignal>(OnLanguageSelected);
            _signalCenter.Unsubscribe<SettingsMusicVolumeChangedSignal>(OnMusicVolumeChanged);
            _signalCenter.Unsubscribe<SettingsSfxVolumeChangedSignal>(OnSfxVolumeChanged);

            base.UnsubscribeFromEvents();
        }
    }
}
