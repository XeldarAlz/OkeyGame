using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;

namespace Runtime.Services.Audio
{
    public interface IAudioService : IInitializableService, IDisposableService
    {
        UniTask PlaySoundAsync(string soundKey);
        UniTask PlayMusicAsync(string musicKey, bool loop = true);
        void StopMusic();
        void StopAllSounds();
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSoundVolume(float volume);
        float GetMasterVolume();
        float GetMusicVolume();
        float GetSoundVolume();
        void SetMuted(bool muted);
        bool IsMuted();
    }
}
