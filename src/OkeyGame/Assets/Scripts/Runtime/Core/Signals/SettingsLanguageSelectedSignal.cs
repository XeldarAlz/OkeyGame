using UnityEngine;

namespace Runtime.Core.Signals
{
    public readonly struct SettingsLanguageSelectedSignal
    {
        public readonly SystemLanguage Language;

        public SettingsLanguageSelectedSignal(SystemLanguage language)
        {
            Language = language;
        }
    }
}
