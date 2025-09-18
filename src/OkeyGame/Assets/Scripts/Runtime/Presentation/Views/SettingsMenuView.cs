using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Runtime.Core.SignalCenter;
using Runtime.Core.Signals;
using Zenject;

namespace Runtime.Presentation.Views
{
    public sealed class SettingsMenuView : BaseView
    {
        private ISignalCenter _signalCenter;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private SystemLanguage[] _languages;

        [Header("Audio")]
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Inject]
        private void Construct(ISignalCenter signalCenter)
        {
            _signalCenter = signalCenter;
        }

        protected override void Initialize()
        {
            base.Initialize();
            SubscribeToUI();
        }

        private void SubscribeToUI()
        {
            if (!ReferenceEquals(_languageDropdown, null))
            {
                _languageDropdown.onValueChanged.AddListener(HandleLanguageChanged);
            }

            if (!ReferenceEquals(_musicVolumeSlider, null))
            {
                _musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            }

            if (!ReferenceEquals(_sfxVolumeSlider, null))
            {
                _sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }
        }

        private void HandleLanguageChanged(int optionIndex)
        {
            if (_languages == null)
            {
                return;
            }

            int length = _languages.Length;
            if (optionIndex < 0)
            {
                return;
            }

            if (optionIndex >= length)
            {
                return;
            }

            SystemLanguage language = _languages[optionIndex];
            _signalCenter.Fire(new SettingsLanguageSelectedSignal(language));
        }

        private void HandleMusicVolumeChanged(float value)
        {
            _signalCenter.Fire(new SettingsMusicVolumeChangedSignal(value));
        }

        private void HandleSfxVolumeChanged(float value)
        {
            // _signalCenter.Fire(new SettingsSfxVolumeChangedSignal(value));
        }

        public void SetLanguage(SystemLanguage language)
        {
            if (ReferenceEquals(_languageDropdown, null))
            {
                return;
            }

            if (_languages == null)
            {
                return;
            }

            int length = _languages.Length;
            for (int index = 0; index < length; index++)
            {
                if (_languages[index] == language)
                {
                    _languageDropdown.SetValueWithoutNotify(index);
                    return;
                }
            }
        }

        public void SetMusicVolume(float value)
        {
            if (ReferenceEquals(_musicVolumeSlider, null))
            {
                return;
            }

            _musicVolumeSlider.SetValueWithoutNotify(value);
        }

        public void SetSfxVolume(float value)
        {
            if (ReferenceEquals(_sfxVolumeSlider, null))
            {
                return;
            }

            _sfxVolumeSlider.SetValueWithoutNotify(value);
        }

        protected override void Cleanup()
        {
            if (!ReferenceEquals(_languageDropdown, null))
            {
                _languageDropdown.onValueChanged.RemoveListener(HandleLanguageChanged);
            }

            if (!ReferenceEquals(_musicVolumeSlider, null))
            {
                _musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            }

            if (!ReferenceEquals(_sfxVolumeSlider, null))
            {
                _sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }

            base.Cleanup();
        }
    }
}
