using UnityEngine;

namespace GameCore.Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform panel;
        Rect lastSafeArea = new Rect(0, 0, 0, 0);
        Vector2Int lastScreenSize = new Vector2Int(0, 0);
        ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        void Awake()
        {
            panel = GetComponent<RectTransform>();
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            Rect safeArea = Screen.safeArea;

            if (safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y || Screen.orientation != lastOrientation)
            {
                lastScreenSize.x = Screen.width;
                lastScreenSize.y = Screen.height;
                lastOrientation = Screen.orientation;
                lastSafeArea = safeArea;
                
                ApplySafeArea(safeArea);
            }
        }

        void ApplySafeArea(Rect r)
        {
            if (panel == null) return;

            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            
                panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}