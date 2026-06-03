using ArrowGame.Data.Booster;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "ArrowDashBoosterSO", menuName = "ArrowGame/Boosters/ArrowDashBoosterSO")]
    public class ArrowDashBoosterSO : BoosterConfigSO
    {
        [Header("--- Arrow Dash Settings ---")]
        public int targetCount = 5;
        public float delayBetweenEscapes = 0.15f;
        public float highlightDuration = 0.5f;
    }
}
