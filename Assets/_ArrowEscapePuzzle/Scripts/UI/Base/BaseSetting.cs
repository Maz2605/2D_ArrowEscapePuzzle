using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Base
{
    public class SettingPopup : BasePopup
    {
        [Header("--- Setting UI ---")]
        [SerializeField] private Transform panelContainer;
        [SerializeField] private Button btnClose;
        
        [Header("Toggles")]
        [SerializeField] private Toggle tglMusic;
        [SerializeField] private Toggle tglSound;
        [SerializeField] private Toggle tglHaptic; // Rung (Vibration)

        protected override void Awake()
        {
            base.Awake();
            
            if (btnClose != null)
                btnClose.onClick.AddListener(Hide);

            // Bind sự kiện
            tglMusic.onValueChanged.AddListener(OnMusicToggled);
            tglSound.onValueChanged.AddListener(OnSoundToggled);
            tglHaptic.onValueChanged.AddListener(OnHapticToggled);
        }

        public override void Show(Action onOpenedCallback = null)
        {
            // Sync trạng thái UI với Data lưu trong máy (PlayerPrefs / DataManager)
            // Ví dụ: tglMusic.isOn = AudioManager.Instance.IsMusicOn;
            
            base.Show(onOpenedCallback);
        }

        private void OnMusicToggled(bool isOn)
        {
            // Gọi logic AudioManager ở đây
            // AudioManager.Instance.SetMusic(isOn);
        }

        private void OnSoundToggled(bool isOn)
        {
            // AudioManager.Instance.SetSound(isOn);
        }

        private void OnHapticToggled(bool isOn)
        {
            // HapticManager.Instance.SetHaptic(isOn);
        }

        protected override void PlayShowAnimation()
        {
            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.localScale = Vector3.one * 0.8f;
                panelContainer.DOScale(Vector3.one, animDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.DOScale(Vector3.one * 0.8f, animDuration)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}