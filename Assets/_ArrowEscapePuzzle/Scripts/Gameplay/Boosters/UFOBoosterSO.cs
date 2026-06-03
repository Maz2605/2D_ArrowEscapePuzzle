using ArrowGame.Data.Booster;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "UFOBooster", menuName = "ArrowGame/Boosters/UFO")]
    public class UFOBoosterSO : BoosterConfigSO
    {
        [Header("--- UFO Ship Specific Settings ---")]
        public GameObject ufoShipPrefab;
        public float ufoChargeUpTime = 1.5f;
        public float ufoShipTotalDuration = 2.5f;
        public Vector3 ufoSpawnPosition = Vector3.zero;
    }
}
