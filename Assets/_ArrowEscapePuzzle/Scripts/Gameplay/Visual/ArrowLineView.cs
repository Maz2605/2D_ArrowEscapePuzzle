using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.Gameplay.Visual
{
    public class ArrowLineView : MonoBehaviour
    {
        [Header("Hierarchy Setup (New)")]
        [Tooltip("Kéo GameObject con 'Visual' vào đây. Bắt buộc để không lệch scale/rung")]
        [SerializeField] private Transform visualRoot;

        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform headTransform;
        [SerializeField] private SpriteRenderer headSpriteRenderer;

        [HideInInspector] public float moveDuration = 0.7f; 
        public string ArrowID { get; private set; }
        
        private Vector3 _escapeDirection;
        private Vector3[] _basePoints; 
        private float _cellSize;
        private float _travelDistance; 
        
        private Tween _scaleTween;

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            
            lineRenderer.useWorldSpace = false; 
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }

        public void Setup(List<ArrowData> sortedPath, float cellSize)
        {
            if (sortedPath == null || sortedPath.Count == 0) return;

            ArrowID = sortedPath[0].ID;
            _cellSize = cellSize;

            _basePoints = new Vector3[sortedPath.Count];
            for (int i = 0; i < sortedPath.Count; i++)
            {
                _basePoints[i] = new Vector3(sortedPath[i].X * cellSize, sortedPath[i].Y * cellSize, 0);
            }
            
            lineRenderer.positionCount = _basePoints.Length;
            lineRenderer.SetPositions(_basePoints);

            ArrowData headData = sortedPath[sortedPath.Count - 1]; 
            headTransform.localPosition = _basePoints[_basePoints.Length - 1];
            headTransform.localRotation = Quaternion.Euler(0, 0, GetHeadRotation(headData.Type));
            
            _escapeDirection = GetDirectionVector(headData.Type);
        }

        public void PlayEscapeAnimation()
        {
            // (Giữ nguyên 100% logic snake của sếp)
            float maxBaseDistance = (_basePoints.Length - 1) * _cellSize;
            float targetDistance = maxBaseDistance + 15f;
            
            moveDuration = 0.7f + (_basePoints.Length * 0.08f);

            float safePullback = -0.25f;
            float pullbackTime = 0.15f;

            Sequence snakeSeq = DOTween.Sequence();

            snakeSeq.Append(
                DOTween.To(() => _travelDistance, x => _travelDistance = x, safePullback, pullbackTime)
                    .SetEase(Ease.OutQuad)
                    .OnUpdate(UpdateSnakeBody)
            );

            snakeSeq.Append(
                DOTween.To(() => _travelDistance, x => _travelDistance = x, targetDistance, moveDuration)
                    .SetEase(Ease.InCubic)
                    .OnUpdate(UpdateSnakeBody)
            );

            snakeSeq.Join(headSpriteRenderer.DOFade(0, moveDuration * 0.5f).SetDelay(moveDuration * 0.5f + pullbackTime));
            
            snakeSeq.SetLink(gameObject);
            snakeSeq.OnComplete(() => gameObject.SetActive(false));
        }
        
        private void UpdateSnakeBody()
        {
            // (Giữ nguyên 100% thuật toán của sếp)
            if (_basePoints == null || _basePoints.Length == 0) return;

            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float safePullback = -0.25f; 

            if (_travelDistance < safePullback - 0.01f) _travelDistance = safePullback;

            Vector3 tailPos;
            Vector3 headPos;
            float headDist;

            if (_travelDistance < 0)
            {
                tailPos = _basePoints[0];
                headDist = _travelDistance + totalBodyLength;
                headPos = GetPointAlongPathPrecise(headDist);
            }
            else
            {
                tailPos = GetPointAlongPathPrecise(_travelDistance);
                headDist = _travelDistance + totalBodyLength;
                headPos = GetPointAlongPathPrecise(headDist);
            }

            List<Vector3> rawPoints = new List<Vector3>();

            if (_travelDistance < 0) 
            {
                rawPoints.Add(_basePoints[0]); 

                for (int i = 1; i < _basePoints.Length; i++)
                {
                    float nodeDist = i * _cellSize;
                    if (nodeDist < headDist - 0.01f) rawPoints.Add(_basePoints[i]);
                }
                
                if (Vector3.Distance(_basePoints[0], headPos) > 0.05f) 
                {
                    rawPoints.Add(headPos);
                }
            }
            else 
            {
                rawPoints.Add(tailPos);

                for (int i = 0; i < _basePoints.Length; i++)
                {
                    float nodeDist = i * _cellSize;
                    if (nodeDist > _travelDistance + 0.01f && nodeDist < headDist - 0.01f)
                    {
                        rawPoints.Add(_basePoints[i]);
                    }
                }

                if (Vector3.Distance(tailPos, headPos) > 0.01f) 
                {
                    rawPoints.Add(headPos);
                }
            }

            List<Vector3> cleanPoints = new List<Vector3>();
            foreach (var p in rawPoints)
            {
                if (cleanPoints.Count == 0 || Vector3.Distance(cleanPoints[cleanPoints.Count - 1], p) > 0.02f)
                {
                    cleanPoints.Add(p);
                }
            }

            List<Vector3> finalPoints = new List<Vector3>();
            if (cleanPoints.Count >= 2)
            {
                finalPoints.Add(cleanPoints[0]);

                for (int i = 1; i < cleanPoints.Count - 1; i++)
                {
                    Vector3 prev = finalPoints[finalPoints.Count - 1];
                    Vector3 curr = cleanPoints[i];
                    Vector3 next = cleanPoints[i + 1];

                    Vector3 dir1 = (curr - prev).normalized;
                    Vector3 dir2 = (next - curr).normalized;

                    if (dir1 == Vector3.zero || dir2 == Vector3.zero || Vector3.Dot(dir1, dir2) < 0.99f)
                    {
                        finalPoints.Add(curr);
                    }
                }
                finalPoints.Add(cleanPoints[cleanPoints.Count - 1]);
            }

            if (finalPoints.Count >= 2)
            {
                lineRenderer.positionCount = finalPoints.Count;
                lineRenderer.SetPositions(finalPoints.ToArray());
                
                Vector3 lastNodePos = finalPoints[finalPoints.Count - 2];
                Vector3 headP = finalPoints[finalPoints.Count - 1];
                
                headTransform.localPosition = headP;

                Vector3 dir = (headP - lastNodePos).normalized;
                
                if (dir == Vector3.zero || _travelDistance < 0) 
                    dir = _escapeDirection;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                headTransform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else if (finalPoints.Count == 1 && _travelDistance < 0)
            {
                lineRenderer.positionCount = 0; 
                headTransform.localPosition = headPos; 
                float angle = Mathf.Atan2(_escapeDirection.y, _escapeDirection.x) * Mathf.Rad2Deg;
                headTransform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        private Vector3 GetPointAlongPathPrecise(float distance)
        {
            // (Giữ nguyên logic precise)
            int maxIndex = _basePoints.Length - 1;
            float maxPathDist = maxIndex * _cellSize;

            if (distance >= maxPathDist)
            {
                return _basePoints[maxIndex] + _escapeDirection * (distance - maxPathDist);
            }

            if (distance <= 0)
            {
                if (maxIndex == 0) return _basePoints[0] + _escapeDirection * distance;
                Vector3 tailDir = (_basePoints[0] - _basePoints[1]).normalized;
                return _basePoints[0] + tailDir * Mathf.Abs(distance);
            }

            int index = Mathf.FloorToInt(distance / _cellSize);
            if (index >= maxIndex) return _basePoints[maxIndex];

            float t = (distance % _cellSize) / _cellSize;
            return Vector3.Lerp(_basePoints[index], _basePoints[index + 1], t);
        }

        public void PlayBlockedAnimation(float realBumpDistance)
        {
            DOTween.Kill(this + "block");
            _travelDistance = 0f;
            ResetColor();
            UpdateSnakeBody();

            float bumpTime = 0.08f + (realBumpDistance * 0.05f);
            float recoilTime = 0.8f;

            Sequence bumpSeq = DOTween.Sequence().SetId(this + "block");

            bumpSeq.Append(
                DOTween.To(() => _travelDistance, x => _travelDistance = x, realBumpDistance, bumpTime)
                    .SetEase(Ease.InQuad) 
                    .OnUpdate(UpdateSnakeBody)
            );

            bumpSeq.AppendCallback(() => 
            {
                lineRenderer.startColor = lineRenderer.endColor = headSpriteRenderer.color = Color.red;
                EventManager<VisualEventID>.Post(VisualEventID.ArrowImpact);
                
                // [FIXED] Lắc thẳng cái thùng chứa VisualRoot thay vì Transform gốc
                if (visualRoot != null) {
                    visualRoot.DOShakePosition(0.3f, 0.08f, 10, 90, false, true).SetLink(visualRoot.gameObject);
                } else {
                    transform.DOShakePosition(0.3f, 0.08f, 10, 90, false, true).SetLink(gameObject);
                }
            });

            bumpSeq.Append(
                DOTween.To(() => _travelDistance, x => _travelDistance = x, 0f, recoilTime)
                    .SetEase(Ease.OutBack, 2f) 
                    .OnUpdate(UpdateSnakeBody)
            );

            bumpSeq.SetLink(gameObject);
        }
        
        public void PlayHoldEffect(bool isHolding)
        {
            if (visualRoot == null) return;

            _scaleTween?.Kill(); 

            float targetScale = isHolding ? 1.2f : 1.0f;
            float duration = isHolding ? 0.15f : 0.1f;

            // [FIXED] Scale thẳng cái thùng chứa VisualRoot
            _scaleTween = visualRoot.DOScale(targetScale, duration)
                .SetEase(isHolding ? Ease.OutBack : Ease.OutQuad)
                .SetLink(visualRoot.gameObject); 
        }

        private void ResetColor() 
        {
            if (lineRenderer != null && headSpriteRenderer != null)
                lineRenderer.startColor = lineRenderer.endColor = headSpriteRenderer.color = Color.white;
        }

        private float GetHeadRotation(CellType type) => type switch {
            CellType.ArrowHeadUp => 0f, CellType.ArrowHeadRight => -90f,
            CellType.ArrowHeadDown => 180f, CellType.ArrowHeadLeft => 90f, _ => 0f
        };

        private Vector3 GetDirectionVector(CellType type) => type switch {
            CellType.ArrowHeadUp => Vector3.up, CellType.ArrowHeadRight => Vector3.right,
            CellType.ArrowHeadDown => Vector3.down, CellType.ArrowHeadLeft => Vector3.left, _ => Vector3.zero
        };

        private void OnDisable()
        {
            transform.DOKill();
            if (visualRoot != null) visualRoot.DOKill();
        }
    }
}