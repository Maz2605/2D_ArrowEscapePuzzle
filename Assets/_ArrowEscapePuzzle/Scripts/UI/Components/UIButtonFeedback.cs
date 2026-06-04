using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArrowGame.Audio;
using ArrowGame.Haptic;

namespace ArrowGame.UI.Components
{
    public class UIButtonFeedback : MonoBehaviour, IPointerClickHandler
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only play feedback if the button is interactable
            if (_button != null && !_button.interactable) return;

            // Play Sound
            if (AudioManager.Instance != null)
            {
                var clip = AudioManager.Instance.DefaultButtonSound;
                if (clip != null)
                {
                    AudioManager.Instance.PlaySfx(clip);
                }
                else
                {
                    // Fallback to ClickNormal if DefaultButtonSound is not configured
                    AudioManager.Instance.PlayUISound(GameCore.Data.UISoundType.ClickNormal);
                }
            }

            // Play Haptic Light (Selection vibration)
            if (HapticManager.Instance != null)
            {
                HapticManager.Instance.Selection();
            }
        }
    }
}
