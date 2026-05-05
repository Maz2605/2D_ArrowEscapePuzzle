using UnityEngine;

namespace ArrowGame.Data.VFX
{
    [CreateAssetMenu(fileName = "DifficultyIntroProfile_", menuName = "ArrowGame/VFX/Difficulty Intro Profile")]
    public class DifficultyIntroProfileSO : ScriptableObject
    {
        public GameObject vfxPrefab;
        public float startDelay = 0f;
        public float duration = 1.5f;
        public float cameraDistance = 10f;
        public Vector3 worldOffset = Vector3.zero;
        public bool stopOnIntroComplete = true;
        public bool reuseIfAlreadyPlaying = false;
    }
}
