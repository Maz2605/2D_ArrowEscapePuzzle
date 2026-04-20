using GameCore.Audio.Manager;
using GameCore.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore.Audio.UI_Sound
{
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        // Biến public để Editor truy cập dễ dàng
        public UISoundType soundType = UISoundType.ClickNormal;
        
        // Chỉ dùng khi type = Custom
        public AudioClip customClip; 
        [Range(0f, 1f)] public float volumeScale = 1f;

        public void OnPointerClick(PointerEventData eventData)
        {
            PlaySound();
        }

        public void PlaySound()
        {
            if (AudioManager.Instance == null) return;

            if (soundType == UISoundType.Custom)
            {
                // Logic Custom: Play file riêng
                if (customClip != null) 
                    AudioManager.Instance.PlaySfx(customClip, volumeScale);
            }
            else
            {
                // Logic Standard: Gọi qua Enum
                AudioManager.Instance.PlayUISound(soundType);
            }
        }
    }
}