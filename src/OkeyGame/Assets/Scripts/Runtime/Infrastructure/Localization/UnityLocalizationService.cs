using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.Infrastructure.Localization
{
    public sealed class UnityLocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, LocalizedString> _cachedStrings;
        private bool _isInitialized;

        public UnityLocalizationService()
        {
            _cachedStrings = new Dictionary<string, LocalizedString>();
        }

        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Directly await the AsyncOperationHandle
                await LocalizationSettings.InitializationOperation;
                _isInitialized = true;
                Debug.Log("[UnityLocalizationService] Initialized successfully");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[UnityLocalizationService] Initialization failed: {exception.Message}");
                throw;
            }
        }

        public async UniTask<string> GetLocalizedTextAsync(string key)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (!_cachedStrings.TryGetValue(key, out LocalizedString localizedString))
            {
                localizedString = new LocalizedString("UI_Table", key);
                _cachedStrings[key] = localizedString;
            }

            try
            {
                AsyncOperationHandle<string> operation = localizedString.GetLocalizedStringAsync();
                await operation;
                string result = operation.Result;
                return result;
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[UnityLocalizationService] Failed to get localized text for key '{key}': {exception.Message}");
                return key;
            }
        }

        public async UniTask<string> GetLocalizedTextAsync(string key, params object[] args)
        {
            string localizedText = await GetLocalizedTextAsync(key);
            
            try
            {
                return string.Format(localizedText, args);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[UnityLocalizationService] Failed to format localized text for key '{key}': {exception.Message}");
                return localizedText;
            }
        }

        public void SetLanguage(SystemLanguage language)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[UnityLocalizationService] Service not initialized");
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(language);
            
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"[UnityLocalizationService] Language set to: {language}");
            }
            else
            {
                Debug.LogWarning($"[UnityLocalizationService] Language not supported: {language}");
            }
        }

        public SystemLanguage GetCurrentLanguage()
        {
            if (!_isInitialized || LocalizationSettings.SelectedLocale == null)
            {
                return SystemLanguage.English;
            }

            string languageCode = LocalizationSettings.SelectedLocale.Identifier.CultureInfo.TwoLetterISOLanguageName;
            
            return languageCode switch
            {
                "en" => SystemLanguage.English,
                "tr" => SystemLanguage.Turkish,
                _ => SystemLanguage.English
            };
        }

        public bool IsLanguageSupported(SystemLanguage language)
        {
            if (!_isInitialized)
            {
                return false;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(language);
            return locale != null;
        }

        public async UniTask LoadLanguageAsync(SystemLanguage language)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(language);
            
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                await LocalizationSettings.InitializationOperation.ToUniTask();
                Debug.Log($"[UnityLocalizationService] Language loaded: {language}");
            }
            else
            {
                Debug.LogWarning($"[UnityLocalizationService] Failed to load language: {language}");
            }
        }
    }
}