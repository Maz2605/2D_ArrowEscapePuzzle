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
            EventManager<VisualEventID>.AddListener(VisualEventID.DifficultyIntroComplete, HandleDifficultyIntroComplete);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.DifficultyIntroComplete, HandleDifficultyIntroComplete);
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

        public bool HasIntroVFX(LevelDifficulty difficulty)
        {
            return _profileLookup.TryGetValue(difficulty, out var profile) && profile != null && profile.vfxPrefab != null;
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            if (state == InGameState.Intro)
            {
                PlayIntroForCurrentDifficulty();
                return;
            }

            // Khi game bắt đầu chơi hoặc kết thúc, dọn dẹp nếu cần
            if (_activeProfile != null && _activeProfile.stopOnIntroComplete)
            {
                if (state == InGameState.Playing || state == InGameState.Win || state == InGameState.Lose)
                {
                    ResetActiveVFX();
                }
            }
        }

        private void HandleDifficultyIntroComplete()
        {
            // Nếu nhận được tín hiệu hoàn thành từ VFX (thông qua EventManager), thực hiện thu hồi
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

            Transform root = UI.Manager.UIManager.Instance != null ? UI.Manager.UIManager.Instance.TopRoot : null;
            
            _activeInstance = PoolingManager.Instance.Spawn(_activeProfile.vfxPrefab, Vector3.zero, Quaternion.identity, root);
            
            // Đảm bảo UI object được đặt đúng vị trí từ Profile và scale chuẩn
            RectTransform rt = _activeInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = _activeProfile.anchoredPosition;
                rt.localScale = Vector3.one;
            }

            // Kiểm tra xem Prefab có script tự báo cáo hoàn thành không
            bool hasScript = _activeInstance.GetComponent<ArrowGame.VFX.SuperHardVFXBehavior>() != null;

            // Nếu Prefab không có script báo cáo sự kiện, chúng ta sẽ dựa vào duration của SO để bắn sự kiện
            if (!hasScript && _activeProfile.duration > 0f)
            {
                _autoDespawnTween = DOVirtual.DelayedCall(_activeProfile.duration, () =>
                {
                    // Tự bắn event nếu script trên prefab không có
                    EventManager<VisualEventID>.Post(VisualEventID.DifficultyIntroComplete);
                }).SetLink(_activeInstance, LinkBehaviour.KillOnDisable);
            }
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
                if (entry.profile == null) continue;
                
                if (_profileLookup.ContainsKey(entry.difficulty))
                {
                    Debug.LogWarning($"[DifficultyIntroVFXController] Duplicate config for difficulty {entry.difficulty} on {name}.");
                    continue;
                }

                _profileLookup.Add(entry.difficulty, entry.profile);
            }
        }
    }
}
