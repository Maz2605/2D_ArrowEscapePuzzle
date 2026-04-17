using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ArrowGame.UI.Components;
using ArrowGame.UI.VFX;

namespace ArrowGame.UI.Popups
{
    public class WinPopup : BasePopup
    {
        [Header("--- Level Info ---")] 
        [SerializeField] private TextMeshProUGUI txtLevel;

        [Header("--- UI Components ---")] 
        [SerializeField] private StarBarWidget starBarWidget;
        [SerializeField] private TextMeshProUGUI txtCoinReward;
        [SerializeField] private CoinWidget totalCoinWidget;

        [Header("--- Buttons ---")] 
        [SerializeField] private Button btnNextLevel;
        [SerializeField] private Button btnHome;

        [Header("--- VFX Coins ---")] 
        [SerializeField] private GameObject coinFlightVfxPrefab;
        [SerializeField] private Transform coinIconTarget; 
        [SerializeField] private int particleBurstCount = 20; 

        [Header("--- VFX Confetti (Pháo) ---")]
        [Tooltip("Pháo nổ 1 lần (Có thể để null nếu không thích)")]
        [SerializeField] private GameObject confettiBurstPrefab; 
        [Tooltip("Pháo rơi liên tục lả tả (Bắt buộc)")]
        [SerializeField] private GameObject confettiFallPrefab;  
        [Tooltip("Điểm sinh hạt (Nên tạo 1 Empty Object đặt ở cạnh trên cùng màn hình)")]
        [SerializeField] private Transform confettiSpawnPoint;   

        private Sequence _winSequence;
        private int _currentVisualTotalCoin;
        private GameObject _activeConfettiFall;

        protected override void Awake()
        {
            base.Awake();
            if (btnNextLevel != null) btnNextLevel.onClick.AddListener(OnNextClicked);
            if (btnHome != null) btnHome.onClick.AddListener(OnHomeClicked);
        }

        public void SetupAndAnimate(int levelIndex, int targetStars, int targetCoins)
        {
            int currentTotalCoin = DataManager.Instance.GetCurrentCoin();
            _currentVisualTotalCoin = currentTotalCoin - targetCoins;
            
            if (totalCoinWidget != null) totalCoinWidget.SetInitialValue(_currentVisualTotalCoin);
            if (txtLevel != null) txtLevel.text = $"LEVEL {levelIndex}";

            if (txtCoinReward != null)
            {
                txtCoinReward.text = $"+{targetCoins}"; 
                txtCoinReward.transform.localScale = Vector3.zero;
            }

            if (starBarWidget != null) starBarWidget.ResetAllStars();

            ClearConfetti();
            _winSequence?.Kill();

            _winSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _winSequence.AppendInterval(0.2f);

            // 1. Kích hoạt Pháo (Burst & Fall) chạy song song với Sao
            _winSequence.AppendCallback(() => {
                
                // Bắn pháo nổ cái đùng
                if (confettiBurstPrefab != null)
                {
                    SpawnVFX(confettiBurstPrefab, confettiSpawnPoint, 3f);
                }
                
                // Thả pháo rơi lả tả
                if (confettiFallPrefab != null)
                {
                    _activeConfettiFall = SpawnVFX(confettiFallPrefab, confettiSpawnPoint, 0f);
                    
                    // Khoá an toàn: Ép Particle chạy để fix lỗi "tàng hình"
                    if (_activeConfettiFall != null)
                    {
                        ParticleSystem[] pSystems = _activeConfettiFall.GetComponentsInChildren<ParticleSystem>(true);
                        foreach (var ps in pSystems) ps.Play(true);
                    }
                }
            });

            // 2. Chạy Animation Sao
            if (starBarWidget != null)
            {
                starBarWidget.AppendAnimationToSequence(_winSequence, targetStars);
            }

            _winSequence.AppendInterval(0.1f);

            // 3. Hiện text thưởng và bắn tiền vào quỹ
            if (txtCoinReward != null && totalCoinWidget != null)
            {
                _winSequence.Append(txtCoinReward.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                _winSequence.AppendCallback(() => PlayCoinFlightVFX(targetCoins, currentTotalCoin));
            }
        }

        #region VFX LOGIC (Pháo & Tiền)

        private GameObject SpawnVFX(GameObject prefab, Transform point, float duration)
        {
            if (prefab == null) return null;
            Vector3 pos = point != null ? point.position : transform.position;
            GameObject vfx = PoolingManager.Instance.Spawn(prefab, pos, Quaternion.identity);
            
            vfx.transform.SetParent(this.transform, true);
            vfx.transform.localScale = Vector3.one;

            // Nếu có duration > 0 thì tự động thu hồi (Dùng cho pháo nổ Burst)
            if (duration > 0)
            {
                DOVirtual.DelayedCall(duration, () => {
                    if (vfx != null && vfx.activeInHierarchy) PoolingManager.Instance.Despawn(vfx);
                }).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            return vfx;
        }

        private void ClearConfetti()
        {
            if (_activeConfettiFall != null && _activeConfettiFall.activeInHierarchy)
            {
                PoolingManager.Instance.Despawn(_activeConfettiFall);
                _activeConfettiFall = null;
            }
        }

        private void PlayCoinFlightVFX(int targetCoins, int finalTotalCoin)
        {
            if (coinFlightVfxPrefab == null) return;

            GameObject vfxObj = PoolingManager.Instance.Spawn(coinFlightVfxPrefab, txtCoinReward.transform.position, Quaternion.identity);
            vfxObj.transform.SetParent(transform, true);
            vfxObj.transform.localScale = Vector3.one;

            CoinParticleAttractor coinAttractor = vfxObj.GetComponent<CoinParticleAttractor>();
            if (coinAttractor != null)
            {
                int valuePerCoin = Mathf.CeilToInt((float)targetCoins / particleBurstCount);

                coinAttractor.onCoinReachedTarget.RemoveAllListeners();
                coinAttractor.onCoinReachedTarget.AddListener(() => {
                    totalCoinWidget.transform.DOKill(true);
                    totalCoinWidget.transform.DOPunchScale(Vector3.one * 0.15f, 0.1f, 1).SetUpdate(true);

                    _currentVisualTotalCoin += valuePerCoin;
                    if (_currentVisualTotalCoin > finalTotalCoin) 
                        _currentVisualTotalCoin = finalTotalCoin;

                    totalCoinWidget.SetInitialValue(_currentVisualTotalCoin);
                    EventManager<VisualEventID>.Post(VisualEventID.CoinCountTick);
                });

                Transform actualTarget = coinIconTarget != null ? coinIconTarget : totalCoinWidget.transform;
                coinAttractor.PlayCoinFlight(actualTarget);
            }

            DOVirtual.DelayedCall(6f, () => {
                if (vfxObj != null && vfxObj.activeInHierarchy) PoolingManager.Instance.Despawn(vfxObj);
            }, ignoreTimeScale: true).SetLink(vfxObj);
        }

        #endregion

        #region POPUP ACTIONS & ANIMATIONS

        private void OnNextClicked() { Hide(); EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel); }
        private void OnHomeClicked() { Hide(); GameManager.Instance.RequestBackHome(); }

        protected override void PlayShowAnimation()
        {
            transform.localScale = Vector3.one * 0.8f;
            transform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            ClearConfetti(); 
            transform.DOScale(Vector3.one * 0.8f, animDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        protected void OnDestroy()
        {
            if (btnNextLevel != null) btnNextLevel.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();

            ClearConfetti();
            _winSequence?.Kill();
            totalCoinWidget?.transform.DOKill();
            transform.DOKill();
        }

        #endregion
    }
}