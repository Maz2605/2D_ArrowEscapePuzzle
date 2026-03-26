using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Data.Events;
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

        [Header("--- 1. REFERENCES ---")] 
        [SerializeField] private Transform visualRoot;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform headTransform;
        [SerializeField] private SpriteRenderer headSpriteRenderer;
        [SerializeField] private LineRenderer lineDirection;

        [Header("--- 2. COLOR SETTINGS ---")] 
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color blockedColor = Color.red;

        [Header("--- 3. ANIMATION: ESCAPE ---")]
        [Tooltip("Tốc độ bay thoát ra khỏi màn hình")]
        [SerializeField] private float escapeSpeed = 25f; 
        [Tooltip("Khoảng cách giật lùi lấy đà (số âm)")]
        [SerializeField] private float pullbackOffset = -0.25f;
        [Tooltip("Thời gian giật lùi lấy đà")]
        [SerializeField] private float pullbackDuration = 0.1f; 
        [Tooltip("Tỉ lệ quãng đường (0-1) mũi tên bắt đầu mờ dần")]
        [Range(0f, 1f)][SerializeField] private float fadeOutRatio = 0.7f; 
        [Tooltip("Khoảng cách bay dư ra ngoài màn hình để đảm bảo khuất hẳn")]
        [SerializeField] private float escapeExtraDistanceFactor = 1.2f;

        [Header("--- 4. ANIMATION: BLOCKED ---")]
        [Tooltip("Thời gian va chạm cơ bản")]
        [SerializeField] private float baseBumpTime = 0.05f; 
        [Tooltip("Hệ số nhân thời gian dựa trên khoảng cách va chạm")]
        [SerializeField] private float bumpDistMultiplier = 0.03f; 
        [Tooltip("Thời gian nảy về vị trí cũ")]
        [SerializeField] private float reboundDuration = 0.15f; 
        [Tooltip("Thời gian rung lắc khi đụng tường")]
        [SerializeField] private float shakeDuration = 0.15f;
        [Tooltip("Biên độ rung lắc")]
        [SerializeField] private float shakeStrength = 0.08f;

        [Header("--- 5. ANIMATION: SPAWN (Intro) ---")]
        [Tooltip("Tỉ lệ thời gian (0-1) dành cho việc hiện đầu mũi tên so với tổng thời gian mọc")]
        [Range(0.1f, 1f)][SerializeField] private float spawnRevealRatio = 0.5f;

        [Header("--- 6. ANIMATION: HOLD / INTERACT ---")]
        [Tooltip("Độ phình to khi chạm và giữ mũi tên")]
        [SerializeField] private float holdScaleTarget = 1.05f;
        [Tooltip("Thời gian phình to khi vừa chạm vào")]
        [SerializeField] private float holdScaleDurationIn = 0.2f;
        [Tooltip("Thời gian thu về bình thường khi nhả tay")]
        [SerializeField] private float holdScaleDurationOut = 0.15f;

        public string ArrowID { get; private set; }

        private Vector3 _escapeDirection;
        private Vector3[] _basePoints;
        private float _cellSize;
        private float _travelDistance;
        private Camera _mainCam;
        private bool _isBlocked; 
        
        // Cache Tween References để quản lý bộ nhớ
        private Tween _scaleTween;
        private Tween _shakeTween;
        private Sequence _actionSequence;

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
            _travelDistance = 0f;
            _isBlocked = false; 
            headSpriteRenderer.color = defaultColor;
            headSpriteRenderer.DOFade(1f, 0f); 
            visualRoot.localScale = Vector3.one;
            visualRoot.localPosition = Vector3.zero; 
        }

        public void OnDespawn()
        {
            KillAllActiveTweens();

            lineRenderer.positionCount = 0;
            _rawPointsCache.Clear();
            _finalPointsCache.Clear();

            if (lineDirection != null) lineDirection.enabled = false;
        }

        public void Setup(List<ArrowData> sortedPath, float cellSize)
        {
            KillAllActiveTweens(); 
            ResetColor();
            _isBlocked = false; 

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
            KillAllActiveTweens();
            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float distanceToEdge = CameraUtils.GetDistanceToEdge(_mainCam, headTransform.position, _escapeDirection);
            
            // Lấy thông số từ Inspector thay vì hard-code 1.2f
            float targetDistance = distanceToEdge + totalBodyLength + (_cellSize * escapeExtraDistanceFactor);
            
            float moveDuration = targetDistance / escapeSpeed; 

            _actionSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // 1. Pullback lấy đà
            _actionSequence.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, pullbackOffset, pullbackDuration)
                .SetEase(Ease.OutQuad).OnUpdate(UpdateSnakeBody));

            // 2. Phóng đi & Bắn Event
            _actionSequence.Append(
                DOTween.To(() => _travelDistance, x => _travelDistance = x, targetDistance, moveDuration)
                    .SetEase(Ease.InCubic)
                    .OnUpdate(UpdateSnakeBody)
                    .OnStart(() => EventManager<VisualEventID>.Post(VisualEventID.ArrowEscaped))
            );

            // 3. Tàng hình dần theo tỉ lệ quãng đường
            _actionSequence.Insert(pullbackDuration + (moveDuration * fadeOutRatio), headSpriteRenderer.DOFade(0, moveDuration * (1f - fadeOutRatio)));
            
            // 4. Trả về Pool
            _actionSequence.OnComplete(() => 
            {
                PoolingManager.Instance.Despawn(gameObject);
            });
        }

        public void PlayBlockedAnimation(float realBumpDistance)
        {
            if (_actionSequence != null && _actionSequence.IsActive() && _actionSequence.IsPlaying()) return;
            
            KillAllActiveTweens(); // Dùng chung hàm dọn dẹp cho chuẩn

            if (visualRoot != null) visualRoot.localPosition = Vector3.zero;

            float bumpTime = baseBumpTime + (realBumpDistance * bumpDistMultiplier);
            _actionSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // 1. Tông vào vật cản
            _actionSequence.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, realBumpDistance, bumpTime)
                .SetEase(Ease.OutQuad).OnUpdate(UpdateSnakeBody)); 

            // 2. Đổi màu báo lỗi
            if (!_isBlocked)
            {
                _isBlocked = true;
                _actionSequence.Join(DOTween.To(() => 0f, x => SetColor(Color.Lerp(defaultColor, blockedColor, x)), 1f, bumpTime));
            }

            // 3. Rung lắc & Bắn Event khi chạm đỉnh
            _actionSequence.AppendCallback(() =>
            {
                EventManager<VisualEventID>.Post(VisualEventID.ArrowWrongImpact);
                Transform targetShake = visualRoot != null ? visualRoot : transform;
                
                _shakeTween = targetShake.DOShakePosition(shakeDuration, shakeStrength)
                    .SetLink(targetShake.gameObject, LinkBehaviour.KillOnDisable);
            });

            // 4. Nảy về vị trí cũ
            _actionSequence.Append(DOTween.To(() => _travelDistance, x => _travelDistance = x, 0f, reboundDuration)
                .SetEase(Ease.OutBack, 1.5f).OnUpdate(UpdateSnakeBody));
        }
        
        public void PlaySpawnAnimation(float delay, float duration)
        {
            KillAllActiveTweens();
    
            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            _travelDistance = -totalBodyLength; 
            UpdateSnakeBody(); 

            // Sử dụng tỉ lệ spawnRevealRatio lấy từ Inspector thay vì 0.5 cứng
            float revealDuration = duration * spawnRevealRatio;

            headSpriteRenderer.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0f);
            headSpriteRenderer.DOFade(defaultColor.a, revealDuration).SetDelay(delay);
    
            visualRoot.localScale = Vector3.zero;
            visualRoot.DOScale(1f, revealDuration).SetDelay(delay).SetEase(Ease.OutBack);

            _actionSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _actionSequence.Insert(delay, DOTween.To(() => _travelDistance, x => _travelDistance = x, 0f, duration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(UpdateSnakeBody));
        }

        public void PlayHoldEffect(bool isHolding)
        {
            _scaleTween?.Kill(); // Chỉ kill scale tween để không ảnh hưởng luồng di chuyển
            
            // Dùng số từ Inspector
            float targetScale = isHolding ? holdScaleTarget : 1.0f;
            float duration = isHolding ? holdScaleDurationIn : holdScaleDurationOut;
            Ease ease = isHolding ? Ease.OutBack : Ease.OutQuad;

            _scaleTween = visualRoot.DOScale(targetScale, duration)
                .SetEase(ease)
                .OnUpdate(() =>
                {
                    if (isHolding) UpdateDirectionLine();
                })
                .SetLink(visualRoot.gameObject);

            if (lineDirection != null) lineDirection.enabled = isHolding;
        }

        #endregion

        #region Core Snake Logic
        
        private void UpdateSnakeBody()
        {
            if (_basePoints == null || _basePoints.Length == 0) return;

            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float headDist = _travelDistance + totalBodyLength;

            Vector3 headPos = GetPointAlongPathPrecise(headDist);
            Vector3 tailPos = GetPointAlongPathPrecise(Mathf.Max(_travelDistance, pullbackOffset));

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

        #endregion

        #region Helpers

        private void KillAllActiveTweens()
        {
            _actionSequence?.Kill();
            _actionSequence = null;
            
            _scaleTween?.Kill();
            _scaleTween = null;
            
            _shakeTween?.Kill();
            _shakeTween = null;
        }

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

        #endregion
    }
}