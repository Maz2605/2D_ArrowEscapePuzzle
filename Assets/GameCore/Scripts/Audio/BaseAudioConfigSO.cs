using UnityEngine;

namespace GameCore.GameCore.Scripts.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Core/ Base Audio Config")]
    public class BaseAudioConfigSO : ScriptableObject
    {
        public AudioClip backgroundMusic;
    }
}