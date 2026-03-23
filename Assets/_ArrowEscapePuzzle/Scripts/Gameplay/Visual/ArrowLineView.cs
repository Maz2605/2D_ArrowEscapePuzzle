using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Utils;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.Visual
{
    public class ArrowLineView : MonoBehaviour, IPoolable
    {
        private const float MIN_NODE_DISTANCE = 0.02f;
        private const float DOT_THRESHOLD = 0.99f;
        private const float PULLBACK_OFFSET = -0.25f;
        private const float ESCAPE_SPEED = 18f;

        [Header("Hierarchy Setup")] [SerializeField]
        private Transform visualRoot;

        [Header("References")] [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField] private Transform headTransform;
        [SerializeField] private SpriteRenderer headSpriteRenderer;
        [SerializeField] private LineRenderer lineDirection;

        [Header("Color Settings")] 
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color blockedColor = Color.red;

        public string ArrowID { get; private set; }

        private Vector3 _escapeDirection;
        private Vector3[] _basePoints;
        private float _cellSize;
        private float _travelDistance;
        private bool _isBlocked;
        private Camera _mainCam;
        private Tween _scaleTween;

        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            _mainCam = Camera.main;

            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }
        
        public void OnSpawn()
        {
            // Reset state cơ bản khi kéo từ pool ra
            _travelDistance = 0f;
            headSpriteRenderer.color = defaultColor;
            headSpriteRenderer.DOFade(1f, 0f); // Reset alpha
            visualRoot.localScale = Vector3.one;
        }

        public void OnDespawn()
        {
            transform.DOKill();
            visualRoot.DOKill();
            DOTween.Kill(this + "block");
            _scaleTween?.Kill();

            lineRenderer.positionCount = 0;
            _rawPointsCache.Clear();
            _finalPointsCache.Clear();

            if (lineDirection != null) lineDirection.enabled = false;
        }

        public void Setup(List<ArrowData> sortedPath, float cellSize)
        {
            _isBlocked = false;
            ResetColor();

            if (sortedPath == null || sortedPath.Count == 0) return;

            ArrowID = sortedPath[0].ID;
            _cellSize = cellSize;

            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0);

            foreach (var node in sortedPath)
            {
                Vector3 pos = new Vector3(node.X * cellSize, node.Y * cellSize, 0);
                minBounds = Vector3.Min(minBounds, pos);
                maxBounds = Vector3.Max(maxBounds, pos);
            }

            Vector3 centerPivot = (minBounds + maxBounds) / 2f;
            transform.localPosition = centerPivot;

            _basePoints = new Vector3[sortedPath.Count];
            for (int i = 0; i < sortedPath.Count; i++)
            {
                _basePoints[i] = new Vector3(sortedPath[i].X * cellSize, sortedPath[i].Y * cellSize, 0) - centerPivot;
            }

            lineRenderer.positionCount = _basePoints.Length;
            lineRenderer.SetPositions(_basePoints);

            ArrowData headData = sortedPath[sortedPath.Count - 1];
            headTransform.localPosition = _basePoints[_basePoints.Length - 1];
            headTransform.localRotation = Quaternion.Euler(0, 0, GetHeadRotation(headData.Type));
            _escapeDirection = GetDirectionVector(headData.Type);

            if (lineDirection != null) lineDirection.enabled = false;
        }

        #region Animations

        public void PlayEscapeAnimation()
        {
            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float distanceToEdge = CameraUtils.GetDistanceToEdge(_mainCam, headTransform.position, _escapeDirection);
            float targetDistance = distanceToEdge + totalBodyLength + (_cellSize * 1.2f);
            float moveDuration = targetDistance / ESCAPE_SPEED; 

            Sequence snakeSeq = DOTween.Sequence();

            // Pullback effect
            snakeSeq.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, PULLBACK_OFFSET, 0.15f)
                .SetEase(Ease.OutQuad).OnUpdate(UpdateSnakeBody));

            // Escape move
            snakeSeq.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, targetDistance, moveDuration)
                .SetEase(Ease.InCubic).OnUpdate(UpdateSnakeBody));

            snakeSeq.Join(headSpriteRenderer.DOFade(0, moveDuration * 0.3f).SetDelay(moveDuration * 0.7f));
            
            snakeSeq.SetLink(gameObject).OnComplete(() => 
            {
                PoolingManager.Instance.Despawn(gameObject);
            });
        }

        public void PlayBlockedAnimation(float realBumpDistance)
        {
            DOTween.Kill(this + "block");
            _travelDistance = 0f;
            UpdateSnakeBody();

            float bumpTime = 0.08f + (realBumpDistance * 0.05f);
            Sequence bumpSeq = DOTween.Sequence().SetId(this + "block");

            bumpSeq.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, realBumpDistance, bumpTime)
                .SetEase(Ease.InQuad).OnUpdate(UpdateSnakeBody));

            if (!_isBlocked)
            {
                _isBlocked = true;
                bumpSeq.Join(DOTween.To(() => 0f, x => SetColor(Color.Lerp(defaultColor, blockedColor, x)), 1f, bumpTime));
            }

            bumpSeq.AppendCallback(() =>
            {
                EventManager<VisualEventID>.Post(VisualEventID.ArrowImpact);
                (visualRoot != null ? visualRoot : transform).DOShakePosition(0.3f, 0.08f);
            });

            bumpSeq.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, 0f, 0.8f)
                .SetEase(Ease.OutBack, 2f).OnUpdate(UpdateSnakeBody));

            bumpSeq.SetLink(gameObject);
        }

        #endregion

        #region Core Snake Logic

        private void UpdateSnakeBody()
        {
            if (_basePoints == null || _basePoints.Length == 0) return;

            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float headDist = _travelDistance + totalBodyLength;

            Vector3 headPos = GetPointAlongPathPrecise(headDist);
            Vector3 tailPos = GetPointAlongPathPrecise(Mathf.Max(_travelDistance, PULLBACK_OFFSET));

            CollectPathNodes(tailPos, headPos, headDist);
            SimplifyPath();
            ApplyPathToRenderer(headPos);
        }

        private void CollectPathNodes(Vector3 tailPos, Vector3 headPos, float headDist)
        {
            _rawPointsCache.Clear();
            _rawPointsCache.Add(tailPos);

            for (int i = 0; i < _basePoints.Length; i++)
            {
                float nodeDist = i * _cellSize;
                if (nodeDist > _travelDistance + 0.01f && nodeDist < headDist - 0.01f)
                {
                    _rawPointsCache.Add(_basePoints[i]);
                }
            }

            if (Vector3.Distance(_rawPointsCache[_rawPointsCache.Count - 1], headPos) > MIN_NODE_DISTANCE)
            {
                _rawPointsCache.Add(headPos);
            }
        }

        private void SimplifyPath()
        {
            _finalPointsCache.Clear();
            if (_rawPointsCache.Count < 2) return;

            _finalPointsCache.Add(_rawPointsCache[0]);

            for (int i = 1; i < _rawPointsCache.Count - 1; i++)
            {
                Vector3 prev = _finalPointsCache[_finalPointsCache.Count - 1];
                Vector3 curr = _rawPointsCache[i];
                Vector3 next = _rawPointsCache[i + 1];

                Vector3 dir1 = (curr - prev).normalized;
                Vector3 dir2 = (next - curr).normalized;

                if (Vector3.Dot(dir1, dir2) < DOT_THRESHOLD) _finalPointsCache.Add(curr);
            }

            _finalPointsCache.Add(_rawPointsCache[_rawPointsCache.Count - 1]);
        }

        private void ApplyPathToRenderer(Vector3 headPos)
        {
            if (_finalPointsCache.Count >= 2)
            {
                lineRenderer.positionCount = _finalPointsCache.Count;
                lineRenderer.SetPositions(_finalPointsCache.ToArray());

                headTransform.localPosition = headPos;
                Vector3 dir = (headPos - _finalPointsCache[_finalPointsCache.Count - 2]).normalized;
                if (dir == Vector3.zero || _travelDistance < 0) dir = _escapeDirection;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                headTransform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else if (_travelDistance < 0)
            {
                lineRenderer.positionCount = 0;
                headTransform.localPosition = headPos;
            }
        }

        #endregion

        private Vector3 GetPointAlongPathPrecise(float distance)
        {
            int maxIndex = _basePoints.Length - 1;
            float maxPathDist = maxIndex * _cellSize;

            if (distance >= maxPathDist) return _basePoints[maxIndex] + _escapeDirection * (distance - maxPathDist);
            if (distance <= 0)
                return _basePoints[0] + (_basePoints[0] - _basePoints[1]).normalized * Mathf.Abs(distance);

            int index = Mathf.FloorToInt(distance / _cellSize);
            float t = (distance % _cellSize) / _cellSize;
            return Vector3.Lerp(_basePoints[index], _basePoints[Mathf.Min(index + 1, maxIndex)], t);
        }

        public void PlayHoldEffect(bool isHolding)
        {
            _scaleTween?.Kill();
            float targetScale = isHolding ? 1.05f : 1.0f;
            _scaleTween = visualRoot.DOScale(targetScale, isHolding ? 0.3f : 0.1f)
                .SetEase(isHolding ? Ease.OutBack : Ease.OutQuad)
                .OnUpdate(() =>
                {
                    if (isHolding) UpdateDirectionLine();
                })
                .SetLink(visualRoot.gameObject);

            if (lineDirection != null) lineDirection.enabled = isHolding;
        }
        
        //Helpers
        private void UpdateDirectionLine()
        {
            if (lineDirection == null) return;
            lineDirection.useWorldSpace = true;

            Vector3 worldStart = headTransform.position + _escapeDirection * (_cellSize * 0.1f);
            float distance = CameraUtils.GetDistanceToEdge(_mainCam, worldStart, _escapeDirection);

            lineDirection.positionCount = 2;
            lineDirection.SetPositions(new[] { worldStart, worldStart + _escapeDirection * distance });
        }

        private void SetColor(Color color)
        {
            lineRenderer.startColor = lineRenderer.endColor = headSpriteRenderer.color = color;
        }

        private void ResetColor() => SetColor(defaultColor);

        private float GetHeadRotation(CellType type) => type switch
        {
            CellType.ArrowHeadUp => 0f, CellType.ArrowHeadRight => -90f,
            CellType.ArrowHeadDown => 180f, CellType.ArrowHeadLeft => 90f, _ => 0f
        };

        private Vector3 GetDirectionVector(CellType type) => type switch
        {
            CellType.ArrowHeadUp => Vector3.up, CellType.ArrowHeadRight => Vector3.right,
            CellType.ArrowHeadDown => Vector3.down, CellType.ArrowHeadLeft => Vector3.left, _ => Vector3.zero
        };
    }
}