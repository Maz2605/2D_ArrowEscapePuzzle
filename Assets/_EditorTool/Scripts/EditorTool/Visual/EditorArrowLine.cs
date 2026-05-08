using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorArrowLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Transform headTransform;

        public void Setup(string arrowID, List<Vector2Int> path, Color arrowColor, bool isHeadFirst)
        {
            gameObject.name = $"ArrowLine_{arrowID}";

            lineRenderer.startColor = arrowColor;
            lineRenderer.endColor = arrowColor;
            lineRenderer.sortingOrder = 8;
            headRenderer.color = arrowColor;
            headRenderer.sortingOrder = 9;

            if (path == null || path.Count == 0)
            {
                lineRenderer.positionCount = 0;
                headRenderer.gameObject.SetActive(false);
                return;
            }

            lineRenderer.positionCount = path.Count;
    
            for (int i = 0; i < path.Count; i++) 
            {
                lineRenderer.SetPosition(i, new Vector3(path[i].x, path[i].y, 0.15f));
            }

            headRenderer.gameObject.SetActive(true);
    
            Vector3 headPos;
            Vector2Int dir;

            if (isHeadFirst)
            {
                headPos = new Vector3(path[0].x, path[0].y, 0f); 
                dir = (path.Count > 1) ? path[0] - path[1] : Vector2Int.up;
            }
            else
            {
                headPos = new Vector3(path[path.Count - 1].x, path[path.Count - 1].y, 0f);
                dir = (path.Count > 1) ? path[path.Count - 1] - path[path.Count - 2] : Vector2Int.up;
            }

            headPos.z = 0.2f; 
            headTransform.position = headPos;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            headTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        public void PlayBounceEffect()
        {
            if (headTransform != null)
            {
                headTransform.DOKill();
                headTransform.localScale = Vector3.one;
                headTransform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 1f)
                    .OnComplete(() => headTransform.localScale = Vector3.one);
            }
        }
    }
}
