using UnityEngine;

namespace GameCore.Utils
{
    [ExecuteInEditMode] 
    public class BaseCameraFitScreen : MonoBehaviour
    {
        [Header("1. Vùng Bất Khả Xâm Phạm (World Units)")]
        public Vector2 targetSize = new Vector2(6f, 10f);
        public Vector2 targetCenter = Vector2.zero;

        [Header("2. Tinh chỉnh hiển thị")]
        public float padding = 0.5f;

        [Header("3. Né UI / Tai thỏ (Quan trọng)")]
        public float offsetY = 0f;

        private Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            FitCamera();
        }

#if UNITY_EDITOR
        void Update()
        {
            FitCamera(); 
        }
#endif

        void FitCamera()
        {
            if (_cam == null) _cam = GetComponent<Camera>();

            float requiredHeight = targetSize.y + (padding * 2);
            float requiredWidth = targetSize.x + (padding * 2);

            float screenAspect = (float)Screen.width / Screen.height;
            float targetAspect = requiredWidth / requiredHeight;
            
            if (screenAspect >= targetAspect)
            {
                _cam.orthographicSize = requiredHeight / 2f;
            }
            else
            {
                _cam.orthographicSize = (requiredWidth / screenAspect) / 2f;
            }
            
            transform.position = new Vector3(targetCenter.x, targetCenter.y + offsetY, -10f);
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green; 
            Gizmos.DrawWireCube(new Vector3(targetCenter.x, targetCenter.y, 0), new Vector3(targetSize.x, targetSize.y, 1));

            Gizmos.color = Color.yellow; 
            float pW = targetSize.x + padding * 2;
            float pH = targetSize.y + padding * 2;
            Gizmos.DrawWireCube(new Vector3(targetCenter.x, targetCenter.y, 0), new Vector3(pW, pH, 1));
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(new Vector3(targetCenter.x, targetCenter.y + offsetY, 0), 0.2f);
        }
    }
}