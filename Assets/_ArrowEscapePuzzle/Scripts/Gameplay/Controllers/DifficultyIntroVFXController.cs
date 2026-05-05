using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.VFX;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class DifficultyIntroVFXController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Difficulty Intro Mapping")]
        [SerializeField] private List<DifficultyIntroEntry> difficultyEntries = new();

        private readonly Dictionary<LevelDifficulty, DifficultyIntroProfileSO> _profileLookup = new();

        private LevelDifficulty _currentDifficulty = LevelDifficulty.None;
        private DifficultyIntroProfileSO _activeProfile;
        private GameObject _activeInstance;
        private Tween _startDelayTween;
        private Tween _autoDespawnTween;

        private void Awake()
        {
            RebuildLookup();
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<VisualEventID>.AddListener(VisualEventID.IntroAnimationComplete, HandleIntroAnimationComplete);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.IntroAnimationComplete, HandleIntroAnimationComplete);
            ResetActiveVFX();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public void SetCurrentDifficulty(LevelDifficulty difficulty)
        {
            _currentDifficulty = difficulty;
            ResetActiveVFX();
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            if (state == InGameState.Intro)
            {
                PlayIntroForCurrentDifficulty();
                return;
            }

            if (_activeProfile != null && _activeProfile.stopOnIntroComplete)
            {
                ResetActiveVFX();
            }
        }

        private void HandleIntroAnimationComplete()
        {
            if (_activeProfile != null && _activeProfile.stopOnIntroComplete)
            {
                ResetActiveVFX();
            }
        }

        private void PlayIntroForCurrentDifficulty()
        {
            if (!_profileLookup.TryGetValue(_currentDifficulty, out DifficultyIntroProfileSO profile) || profile == null)
            {
                ResetActiveVFX();
                return;
            }

            if (profile.vfxPrefab == null)
            {
                Debug.LogWarning($"[DifficultyIntroVFXController] Profile '{profile.name}' thiếu prefab cho difficulty {_currentDifficulty}.");
                ResetActiveVFX();
                return;
            }

            if (_activeInstance != null && _activeProfile == profile && profile.reuseIfAlreadyPlaying)
            {
                return;
            }

            ResetActiveVFX();
            _activeProfile = profile;

            if (profile.startDelay > 0f)
            {
                _startDelayTween = DOVirtual.DelayedCall(profile.startDelay, SpawnActiveProfileVFX)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                return;
            }

            SpawnActiveProfileVFX();
        }

        private void SpawnActiveProfileVFX()
        {
            _startDelayTween = null;

            if (_activeProfile == null || _activeProfile.vfxPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(_activeProfile);
            _activeInstance = PoolingManager.Instance.Spawn(_activeProfile.vfxPrefab, spawnPosition, Quaternion.identity);

            if (_activeProfile.duration > 0f)
            {
                _autoDespawnTween = DOVirtual.DelayedCall(_activeProfile.duration, DespawnActiveInstance)
                    .SetLink(_activeInstance, LinkBehaviour.KillOnDisable);
            }
        }

        private Vector3 ResolveSpawnPosition(DifficultyIntroProfileSO profile)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return profile.worldOffset;
            }

            Vector3 viewportPoint = new Vector3(0.5f, 0.5f, Mathf.Max(0f, profile.cameraDistance));
            return cam.ViewportToWorldPoint(viewportPoint) + profile.worldOffset;
        }

        private void ResetActiveVFX()
        {
            _startDelayTween?.Kill();
            _startDelayTween = null;

            _autoDespawnTween?.Kill();
            _autoDespawnTween = null;

            DespawnActiveInstance();
            _activeProfile = null;
        }

        private void DespawnActiveInstance()
        {
            if (_activeInstance == null)
            {
                return;
            }

            PoolingManager.Instance.Despawn(_activeInstance);
            _activeInstance = null;
        }

        private void RebuildLookup()
        {
            _profileLookup.Clear();

            if (difficultyEntries == null)
            {
                return;
            }

            for (int i = 0; i < difficultyEntries.Count; i++)
            {
                DifficultyIntroEntry entry = difficultyEntries[i];
                if (_profileLookup.ContainsKey(entry.difficulty))
                {
                    Debug.LogWarning($"[DifficultyIntroVFXController] Duplicate config for difficulty {entry.difficulty}. Keeping the first entry on {name}.");
                    continue;
                }

                _profileLookup.Add(entry.difficulty, entry.profile);
            }
        }
    }
}
