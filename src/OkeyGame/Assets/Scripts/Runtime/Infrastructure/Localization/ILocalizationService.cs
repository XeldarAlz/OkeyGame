using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using UnityEngine;

namespace Runtime.Infrastructure.Localization
{
    public interface ILocalizationService : IInitializableService
    {
        UniTask<string> GetLocalizedTextAsync(string key);
        UniTask<string> GetLocalizedTextAsync(string key, params object[] args);
        void SetLanguage(SystemLanguage language);
        SystemLanguage GetCurrentLanguage();
        bool IsLanguageSupported(SystemLanguage language);
        UniTask LoadLanguageAsync(SystemLanguage language);
    }
}
