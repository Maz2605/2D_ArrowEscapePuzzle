using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using DG.Tweening;
using GameCore.UI.Base;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BasePopup = ArrowGame.UI.Base.BasePopup;

namespace ArrowGame.UI.Components
{
    public class EnergyWidget : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI txtEnergy;
        [SerializeField] private TextMeshProUGUI txtTimer; // Kéo thả Text đồng hồ vào đây
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private Button btnOpenPopup;

        [Header("--- Display Settings ---")]
        [SerializeField] private bool showRecoveryCountdown = true;
        [SerializeField] private string fullEnergyText = "FULL";

        [Header("--- Animation Settings ---")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private Color gainColor = Color.green;
        [SerializeField] private Color spendColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;

        private int _lastEnergy = -1;
        private int _lastMaxEnergy = -1;
        private int _lastRemainingSeconds = -1;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.EnergyChanged, OnEnergyChanged);
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.EnergyTimerChanged, OnEnergyTimerChanged);
            if (btnOpenPopup != null)
            {
                btnOpenPopup.onClick.AddListener(OnWidgetClicked);
            }

            // Gọi trực tiếp, bỏ qua DelayedCall để tránh UI bị chớp (flicker)
            RefreshFromData();
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.EnergyChanged, OnEnergyChanged);
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.EnergyTimerChanged, OnEnergyTimerChanged);
            if (btnOpenPopup != null)
            {
                btnOpenPopup.onClick.RemoveListener(OnWidgetClicked);
            }

            iconRect?.DOKill();
            txtEnergy?.DOKill();
        }

        public void RefreshFromData()
        {
            if (DataManager.Instance == null) return;

            _lastEnergy = DataManager.Instance.GetCurrentEnergy();
            _lastMaxEnergy = DataManager.Instance.GetMaxEnergy();
            _lastRemainingSeconds = DataManager.Instance.GetRemainingRecoverySeconds();
            
            UpdateEnergyText();
            UpdateTimerText();
        }

        private void OnEnergyChanged(int currentEnergy)
        {
            bool isInitialized = _lastEnergy >= 0;
            if (isInitialized && currentEnergy != _lastEnergy)
            {
                PlayChangeFeedback(currentEnergy > _lastEnergy);
            }

            _lastEnergy = currentEnergy;
            
            if (DataManager.Instance != null)
            {
                _lastMaxEnergy = DataManager.Instance.GetMaxEnergy();
            }

            UpdateEnergyText();
            UpdateTimerText(); 
        }

        private void OnEnergyTimerChanged(int remainingSeconds)
        {
            _lastRemainingSeconds = remainingSeconds;
            UpdateTimerText();
        }

        private void UpdateEnergyText()
        {
            if (txtEnergy == null) return;

            if (_lastEnergy >= _lastMaxEnergy)
            {
                txtEnergy.text = fullEnergyText;
            }
            else
            {
                txtEnergy.SetText("{0}/{1}", _lastEnergy, _lastMaxEnergy);
            }
        }

        private void UpdateTimerText()
        {
            if (txtTimer == null || !showRecoveryCountdown) 
            {
                if (txtTimer != null && txtTimer.gameObject.activeSelf)
                    txtTimer.gameObject.SetActive(false);
                return;
            }

            if (_lastEnergy < _lastMaxEnergy)
            {
                // Bật GameObject lên nếu đang bị tắt
                if (!txtTimer.gameObject.activeSelf) 
                    txtTimer.gameObject.SetActive(true);

                int safeSeconds = Mathf.Max(0, _lastRemainingSeconds);
                int minutes = safeSeconds / 60;
                int seconds = safeSeconds % 60;

                // SetText trực tiếp số, không dùng toán tử cộng chuỗi
                txtTimer.SetText("{0:00}:{1:00}", minutes, seconds);
            }
            else
            {
                // Khi đầy năng lượng thì tắt hẳn object Text đi để tối ưu chi phí Render và Canvas Rebuild
                if (txtTimer.gameObject.activeSelf) 
                    txtTimer.gameObject.SetActive(false);
            }
        }

        private void PlayChangeFeedback(bool isGain)
        {
            if (txtEnergy != null)
            {
                txtEnergy.DOKill();
                txtEnergy.color = isGain ? gainColor : spendColor;
                txtEnergy.DOColor(normalColor, 0.3f).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            if (iconRect != null)
            {
                iconRect.DOKill();
                iconRect.localScale = Vector3.one;
                iconRect.DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 1f)
                    .SetUpdate(true)
                    .SetLink(iconRect.gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void OnWidgetClicked()
        {
            if (DataManager.Instance == null || UIManager.Instance == null) return;
            if (DataManager.Instance.GetCurrentEnergy() >= DataManager.Instance.GetMaxEnergy()) return;

            UIManager.Instance.ShowPopup<BasePopup>(PopupID.OutOfEnergyPopup);
        }
    }
}
