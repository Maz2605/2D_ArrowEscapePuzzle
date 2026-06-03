using UnityEngine;

namespace ArrowGame.Data.VFX
{
    [CreateAssetMenu(fileName = "DifficultyIntroProfile_", menuName = "ArrowGame/VFX/Difficulty Intro Profile")]
    public class DifficultyIntroProfileSO : ScriptableObject
    {
        [Header("--- Prefab Settings ---")]
        public GameObject vfxPrefab;
        
        [Header("--- Timing Settings ---")]
        public float startDelay = 0f;
        [Tooltip("Nếu Prefab không có script báo cáo hoàn thành, sự kiện sẽ tự bắn sau khoảng duration này.")]
        public float duration = 2.0f;
        
        [Header("--- UI Layout ---")]
        public Vector2 anchoredPosition = Vector2.zero;
        
        [Header("--- Lifecycle ---")]
        public bool stopOnIntroComplete = true;
        public bool reuseIfAlreadyPlaying = false;
    }
}
