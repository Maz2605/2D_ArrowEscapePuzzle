using GameCore.GameCore.Scripts.Audio;
using UnityEngine;

namespace ArrowGame.Data
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "ArrowGame/Audio/Audio Config")]
    public class ArrowAudioConfig : BaseAudioConfigSO
    {
        [Header("Core Gameplay")]
        public AudioClip arrowDash;
        public AudioClip arrowWrong;

        [Header("Booster")]
        public AudioClip boosterHint;
        public AudioClip booster2;
        public AudioClip booster3;
        
        [Header("Sound Effects")]
        public AudioClip fingerTap;

        [Header("Event Main")]
        public AudioClip win;
        public AudioClip lose;
        public AudioClip addCoin;
        public AudioClip addCoinFinal;
    }
}