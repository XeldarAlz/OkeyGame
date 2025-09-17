using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.Services.Audio
{
    public sealed class AudioService : IAudioService
    {
        private AudioSource _musicSource;
        private AudioSource _soundSource;
        
        private float _masterVolume = 1.0f;
        private float _musicVolume = 1.0f;
        private float _soundVolume = 1.0f;
        private bool _isMuted = false;
        
        private bool _isInitialized = false;

        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await CreateAudioSources();
            _isInitialized = true;
            
            Debug.Log("[AudioService] Initialized successfully");
        }

        public void Dispose()
        {
            if (_musicSource != null)
            {
                UnityEngine.Object.Destroy(_musicSource.gameObject);
                _musicSource = null;
            }
            
            if (_soundSource != null)
            {
                UnityEngine.Object.Destroy(_soundSource.gameObject);
                _soundSource = null;
            }
            
            _isInitialized = false;
            Debug.Log("[AudioService] Disposed");
        }

        public async UniTask PlaySoundAsync(string soundKey)
        {
            if (!_isInitialized || _soundSource == null)
            {
                Debug.LogWarning("[AudioService] Cannot play sound - not initialized");
                return;
            }

            // TODO: Load sound clip from Addressables using soundKey
            // For now, just log the request
            Debug.Log($"[AudioService] Playing sound: {soundKey}");
            
            await UniTask.CompletedTask;
        }

        public async UniTask PlayMusicAsync(string musicKey, bool loop = true)
        {
            if (!_isInitialized || _musicSource == null)
            {
                Debug.LogWarning("[AudioService] Cannot play music - not initialized");
                return;
            }

            // TODO: Load music clip from Addressables using musicKey
            // For now, just log the request
            Debug.Log($"[AudioService] Playing music: {musicKey}, loop: {loop}");
            
            _musicSource.loop = loop;
            
            await UniTask.CompletedTask;
        }

        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
                Debug.Log("[AudioService] Music stopped");
            }
        }

        public void StopAllSounds()
        {
            if (_soundSource != null && _soundSource.isPlaying)
            {
                _soundSource.Stop();
            }
            
            StopMusic();
            Debug.Log("[AudioService] All sounds stopped");
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSoundVolume(float volume)
        {
            _soundVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public float GetMasterVolume()
        {
            return _masterVolume;
        }

        public float GetMusicVolume()
        {
            return _musicVolume;
        }

        public float GetSoundVolume()
        {
            return _soundVolume;
        }

        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            UpdateVolumes();
        }

        public bool IsMuted()
        {
            return _isMuted;
        }

        private async UniTask CreateAudioSources()
        {
            GameObject audioManagerObject = new GameObject("AudioManager");
            UnityEngine.Object.DontDestroyOnLoad(audioManagerObject);

            _musicSource = audioManagerObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;

            GameObject soundObject = new GameObject("SoundSource");
            soundObject.transform.SetParent(audioManagerObject.transform);
            _soundSource = soundObject.AddComponent<AudioSource>();
            _soundSource.playOnAwake = false;
            _soundSource.loop = false;

            UpdateVolumes();
            
            await UniTask.CompletedTask;
        }

        private void UpdateVolumes()
        {
            float effectiveVolume = _isMuted ? 0.0f : _masterVolume;

            if (_musicSource != null)
            {
                _musicSource.volume = effectiveVolume * _musicVolume;
            }

            if (_soundSource != null)
            {
                _soundSource.volume = effectiveVolume * _soundVolume;
            }
        }
    }
}
