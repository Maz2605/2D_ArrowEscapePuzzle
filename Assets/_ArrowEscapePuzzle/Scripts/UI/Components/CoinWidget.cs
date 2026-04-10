using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.UI.Components
{
    public class CoinWidget : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI txtCoin;
        [SerializeField] private RectTransform iconRect;

        [Header("--- Settings ---")]
        [Tooltip("Bật: Tự động nghe Data (TopHUD). Tắt: Đợi Win/Lose Popup gọi thủ công.")]
        [SerializeField] private bool autoListenToEvent = true;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float jumpDuration = 0.5f;
        [SerializeField] private float punchScale = 0.3f;
        [SerializeField] private float punchDuration = 0.2f;
        
        [Header("--- Audio Spam Control ---")]
        [SerializeField] private float audioTickCooldown = 0.08f; 
        
        [Header("--- Color Feedback ---")]
        [SerializeField] private Color colorAdd = Color.green;
        [SerializeField] private Color colorSpend = Color.red;
        [SerializeField] private Color colorNormal = Color.white;

        private int _currentDisplayedCoin;
        private Tween _countTween;
        private float _lastAudioPlayTime; 

        private void OnEnable()
        {
            if (autoListenToEvent)
            {
                EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.CoinChanged, OnCoinChanged);
                
                DOVirtual.DelayedCall(0.1f, () => 
                {
                    if (this == null || !gameObject.activeInHierarchy) return; 

                    if (DataManager.Instance != null) 
                    {
                        SetInitialValue(DataManager.Instance.GetCurrentCoin());
                    }
                }).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            
            
        }

        private void OnDisable()
        {
            if (autoListenToEvent)
            {
                EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.CoinChanged, OnCoinChanged);
            }
            
            // Dọn dẹp Tween khi UI bị tắt để tránh memory leak
            _countTween?.Kill();
            if (iconRect != null) iconRect.DOKill();
            if (txtCoin != null) txtCoin.DOKill();
        }

        private void OnCoinChanged(int newCoinBalance)
        {
            if (newCoinBalance == _currentDisplayedCoin) return; 
            PlayCountAnimation(_currentDisplayedCoin, newCoinBalance, jumpDuration);
        }

        public Tween PlayCountAnimation(int startValue, int endValue, float duration)
        {
            _countTween?.Kill(); 
            if (txtCoin != null) txtCoin.DOKill(); 
            
            bool isAdding = endValue > startValue;
            int lastPlayedSoundValue = startValue; 
            
            _currentDisplayedCoin = startValue;

            // Dùng biến local để DOTween chạy đếm (chống các luồng bên ngoài ghi đè)
            int countingValue = startValue;

            _countTween = DOTween.To(() => countingValue, x => 
                {
                    countingValue = x;
                    _currentDisplayedCoin = countingValue; // Đồng bộ lại biến ngầm
                    UpdateText(countingValue);
                    
                    // Phát event âm thanh có giới hạn tốc độ (Cooldown)
                    if (countingValue != lastPlayedSoundValue)
                    {
                        if (Time.unscaledTime - _lastAudioPlayTime >= audioTickCooldown)
                        {
                            EventManager<VisualEventID>.Post(VisualEventID.CoinCountTick);
                            _lastAudioPlayTime = Time.unscaledTime;
                        }
                        lastPlayedSoundValue = countingValue;
                    }

                }, endValue, duration)
                .OnStart(() => 
                {
                    // Đổi màu ngay khi Tween BẮT ĐẦU chạy
                    if (txtCoin != null) txtCoin.color = isAdding ? colorAdd : colorSpend;
                    UpdateText(startValue);
                })
                .SetEase(Ease.OutExpo)
                .SetUpdate(true) 
                .OnComplete(() => 
                {
                    // Chốt sổ: Rung icon và Fade màu lại về trắng
                    PlayJuiceAnimation();
                    if (txtCoin != null) txtCoin.DOColor(colorNormal, 0.3f).SetUpdate(true);
                })
                .SetLink(gameObject, LinkBehaviour.KillOnDisable); // Tự hủy nếu GameObject bị destroy

            return _countTween;
        }

        public void PlayJuiceAnimation()
        {
            if (iconRect != null)
            {
                iconRect.DOKill();
                iconRect.localScale = Vector3.one;
                iconRect.DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 1f)
                    .SetUpdate(true)
                    .SetLink(iconRect.gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void UpdateText(int amount)
        {
            if (txtCoin != null) txtCoin.text = amount.ToString("N0"); 
        }
        
        
        public void SetInitialValue(int amount)
        {
            _countTween?.Kill(); 
            _currentDisplayedCoin = amount;
            UpdateText(_currentDisplayedCoin);
            if (txtCoin != null) txtCoin.color = colorNormal;
        }
    }
}