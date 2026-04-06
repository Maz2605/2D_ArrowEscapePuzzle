using System.Collections.Generic;
using ArrowGame.Interface;
using DG.Tweening;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Audio
{
    public class AudioManager : Singleton<AudioManager>, IAppService
    {
        // --- PHẦN MỚI THÊM VÀO ---
        [Header("Config Data")]
        [SerializeField] private UIAudioConfigSO uiAudioConfig;

        [Header("Setup")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSourcePrefab;
        [SerializeField] private int initialSfxPoolSize = 15;
        [SerializeField] private int maxPoolSize = 30; 
        
        [Header("Runtime Debug")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        
        public bool IsSfxEnabled { get; private set; } = true; 
        public bool IsMusicEnabled { get; private set; } = true;

        private Queue<AudioSource> _sfxPool;
        private Transform _poolRoot;

        // protected override void Awake()
        // {
        //     base.Awake();
        //     InitializePool();
        // }

        public void Init()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            _sfxPool = new Queue<AudioSource>();
            _poolRoot = new GameObject("SFX_Pool").transform;
            _poolRoot.SetParent(transform);

            for (int i = 0; i < initialSfxPoolSize; i++)
            {
                CreateNewSfxSource();
            }
        }

        private AudioSource CreateNewSfxSource()
        {
            var audioSource = Instantiate(sfxSourcePrefab, _poolRoot);
            audioSource.gameObject.SetActive(false);
            _sfxPool.Enqueue(audioSource);
            return audioSource;
        }

        //=== Public API ===

        public void PlayUISound(UISoundType type)
        {
            if (uiAudioConfig == null || !IsSfxEnabled) return;

            AudioClip clip = uiAudioConfig.GetClip(type);
            
            if (clip != null)
            {
                PlaySfx(clip, uiAudioConfig.uiVolume);
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }
        
        public void SetMusicState(bool state)
        {
            IsMusicEnabled = state;
            if (musicSource) musicSource.mute = !IsMusicEnabled;
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetSfxState(bool state)
        {
            IsSfxEnabled = state;
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 0.5f)
        {
            if (musicSource == null || clip == null) return;
            
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.DOKill();

            float targetVol = musicVolume * masterVolume;

            if (musicSource.clip == null || !musicSource.isPlaying)
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.volume = 0;
                musicSource.Play();
                musicSource.DOFade(targetVol, fadeTime).SetUpdate(true);
                return;
            }

            musicSource.DOFade(0, fadeTime / 2).SetUpdate(true).OnComplete(() =>
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.Play();
                musicSource.DOFade(targetVol, fadeTime / 2).SetUpdate(true);
            });
        }

        private void UpdateMusicVolume()
        {
            if (musicSource != null)
            {
                musicSource.DOKill(); 
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void PlaySfx(AudioClip clip, float volScale = 1f, float pitchVar = 0f)
        {
            if (clip == null || !IsSfxEnabled) return;

            AudioSource source = GetSfxSource();
            
            if (source == null) return;

            source.transform.SetParent(_poolRoot);
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volScale;
            source.pitch = 1f + (pitchVar > 0 ? Random.Range(-pitchVar, pitchVar) : 0f);
            
            source.gameObject.SetActive(true);
            source.Play();

            DOVirtual.DelayedCall(clip.length + 0.1f, () =>
            {
                ReturnToPool(source);
            }).SetId(source); 
        }

        public void StopMusic()
        {
            if (musicSource == null) return;
            musicSource.DOKill();
            musicSource.Stop();
        }

        //=== Pool Internal ===
        
        private AudioSource GetSfxSource()
        {
            if (_sfxPool.Count > 0)
            {
                return _sfxPool.Dequeue();
            }
            
            if (_poolRoot.childCount < maxPoolSize)
            {
                return Instantiate(sfxSourcePrefab, _poolRoot);
            }
            return null; 
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;
            
            source.Stop();
            source.clip = null; 
            source.gameObject.SetActive(false);
            
            if (_sfxPool != null) 
            {
                _sfxPool.Enqueue(source);
            }
        }
    }
}