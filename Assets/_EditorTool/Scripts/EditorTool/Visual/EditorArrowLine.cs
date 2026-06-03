using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorArrowLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Transform headTransform;

        private SpriteRenderer _secondaryHeadRenderer;
        private Transform _secondaryHeadTransform;

        public void Setup(string arrowID, List<Vector2Int> path, EditorArrowMetadataData metadata, Color arrowColor)
        {
            gameObject.name = $"ArrowLine_{arrowID}";

            lineRenderer.startColor = arrowColor;
            lineRenderer.endColor = arrowColor;
            lineRenderer.sortingOrder = 8;
            headRenderer.color = arrowColor;
            headRenderer.sortingOrder = 9;

            EnsureSecondaryHead();
            _secondaryHeadRenderer.color = Color.Lerp(arrowColor, Color.white, 0.35f);
            _secondaryHeadRenderer.sortingOrder = 9;

            if (path == null || path.Count == 0 || metadata == null)
            {
                lineRenderer.positionCount = 0;
                headRenderer.gameObject.SetActive(false);
                if (_secondaryHeadRenderer != null) _secondaryHeadRenderer.gameObject.SetActive(false);
                return;
            }

            lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(path[i].x, path[i].y, 0.15f));
            }

            ConfigureHead(headTransform, headRenderer, path, metadata.PrimaryEndpointPathIndex, true);

            if (metadata.HasSecondaryEndpoint)
            {
                ConfigureHead(_secondaryHeadTransform, _secondaryHeadRenderer, path, metadata.SecondaryEndpointPathIndex, false);
            }
            else if (_secondaryHeadRenderer != null)
            {
                _secondaryHeadRenderer.gameObject.SetActive(false);
            }
        }

        public void PlayBounceEffect()
        {
            BounceTransform(headTransform, Vector3.one);
            BounceTransform(_secondaryHeadTransform, Vector3.one * 0.85f);
        }

        private void ConfigureHead(Transform targetTransform, SpriteRenderer targetRenderer, IReadOnlyList<Vector2Int> path,
            int endpointPathIndex, bool isPrimary)
        {
            if (targetTransform == null || targetRenderer == null) return;

            targetRenderer.gameObject.SetActive(true);
            int safeIndex = Mathf.Clamp(endpointPathIndex, 0, path.Count - 1);

            Vector3 headPos = new Vector3(path[safeIndex].x, path[safeIndex].y, isPrimary ? 0.2f : 0.19f);
            Vector2Int dir = GetEndpointDirection(path, safeIndex);

            targetTransform.position = headPos;
            targetTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            targetTransform.localScale = isPrimary ? Vector3.one : Vector3.one * 0.85f;
        }

        private static Vector2Int GetEndpointDirection(IReadOnlyList<Vector2Int> path, int endpointIndex)
        {
            if (path == null || path.Count <= 1)
            {
                return Vector2Int.up;
            }

            int neighborIndex = endpointIndex == 0 ? 1 : path.Count - 2;
            return path[endpointIndex] - path[neighborIndex];
        }

        private void EnsureSecondaryHead()
        {
            if (_secondaryHeadRenderer != null && _secondaryHeadTransform != null) return;
            if (headRenderer == null || headTransform == null) return;

            _secondaryHeadRenderer = Instantiate(headRenderer, headRenderer.transform.parent);
            _secondaryHeadRenderer.name = $"{headRenderer.name}_Secondary";
            _secondaryHeadTransform = _secondaryHeadRenderer.transform;
        }

        private static void BounceTransform(Transform target, Vector3 baseScale)
        {
            if (target == null) return;

            target.DOKill();
            target.localScale = baseScale;
            target.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 1f)
                .OnComplete(() => target.localScale = baseScale);
        }

        public void PlayFlashEffect(Color flashColor, float duration = 1.0f)
        {
            if (lineRenderer == null) return;

            DOTween.Kill(this);

            Color originalStartColor = lineRenderer.startColor;
            Color originalEndColor = lineRenderer.endColor;
            Color originalHeadColor = headRenderer != null ? headRenderer.color : Color.white;
            Color originalSecColor = _secondaryHeadRenderer != null ? _secondaryHeadRenderer.color : Color.white;

            float val = 0f;
            DOTween.To(() => val, x => val = x, 1f, duration / 2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetId(this)
                .OnUpdate(() =>
                {
                    Color currentLineColor = Color.Lerp(originalStartColor, flashColor, val);
                    lineRenderer.startColor = currentLineColor;
                    lineRenderer.endColor = currentLineColor;

                    if (headRenderer != null)
                    {
                        headRenderer.color = Color.Lerp(originalHeadColor, flashColor, val);
                    }
                    if (_secondaryHeadRenderer != null)
                    {
                        _secondaryHeadRenderer.color = Color.Lerp(originalSecColor, flashColor, val);
                    }
                })
                .OnComplete(() =>
                {
                    lineRenderer.startColor = originalStartColor;
                    lineRenderer.endColor = originalEndColor;
                    if (headRenderer != null) headRenderer.color = originalHeadColor;
                    if (_secondaryHeadRenderer != null) _secondaryHeadRenderer.color = originalSecColor;
                });
        }
    }
}
