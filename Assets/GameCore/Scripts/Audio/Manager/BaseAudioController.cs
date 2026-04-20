using GameCore.GameCore.Scripts.Audio;
using UnityEngine;

namespace GameCore.Audio.Manager
{
    public class BaseAudioController : MonoBehaviour
    {
        [SerializeField] BaseAudioConfigSO configSO;

        private AudioManager Audio => AudioManager.Instance;

        public void Initialize()
        {
            Audio.PlayMusic(configSO.backgroundMusic);
        }

        private void OnDestroy()
        {
            Audio?.StopMusic();
        }
    }
}