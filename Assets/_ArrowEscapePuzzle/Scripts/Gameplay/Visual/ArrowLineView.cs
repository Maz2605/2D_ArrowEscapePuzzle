using System;
using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Utils;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ArrowGame.Data.Theme;

namespace ArrowGame.Gameplay.Visual
{
    public class ArrowLineView : MonoBehaviour, IPoolable
    {
        private enum ArrowState
        {
            Idle,
            Spawning,
            Blocked,
            Escaping
        }

        private ArrowState _currentState = ArrowState.Idle;

        private const float MIN_NODE_DISTANCE = 0.02f;
        private const float DOT_THRESHOLD = 0.99f;

        [Header("--- 1. REFERENCES ---")] 
        [SerializeField] private Transform visualRoot;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform headTransform;
        [SerializeField] private SpriteRenderer headSpriteRenderer;
        [SerializeField] private LineRenderer lineDirection;
        [SerializeField] private TrailRenderer escapeTrail;

        [Header("--- 2. ANIMATION: ESCAPE ---")] 
        [SerializeField] private float escapeSpeed = 25f;
        [SerializeField] private float pullbackOffset = -0.25f;
        [SerializeField] private float pullbackDuration = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float fadeOutRatio = 0.7f;
        [SerializeField] private float escapeExtraDistanceFactor = 1.2f;

        [Header("--- 3. ANIMATION: BLOCKED ---")] 
        [SerializeField] private float baseBumpTime = 0.05f;
        [SerializeField] private float bumpDistMultiplier = 0.03f;
        [SerializeField] private float reboundDuration = 0.15f;
        [SerializeField] private float shakeDuration = 0.15f;
        [SerializeField] private float shakeStrength = 0.08f;

        [Header("--- 4. ANIMATION: SPAWN ---")] 
        [Range(0.1f, 1f)] [SerializeField] private float spawnRevealRatio = 0.5f;

        [Header("--- 5. ANIMATION: HOLD / INTERACT ---")] 
        [SerializeField] private float holdScaleTarget = 1.05f;
        [SerializeField] private float holdScaleDurationIn = 0.2f;
        [SerializeField] private float holdScaleDurationOut = 0.15f;

        [Header("--- 6. ANIMATION: LOSE ---")]
        [SerializeField] private float loseScaleTarget = 0.9f;
        [SerializeField] private float loseDuration = 0.6f;
        [SerializeField] private Ease loseEase = Ease.InOutSine;

        // --- CÁC BIẾN MÀU THEME ĐỘNG ---
        private Color _baseColor;
        private Color _blockedColor;
        private Color _loseColor;

        public string ArrowID { get; private set; }

        private Vector3 _escapeDirection;
        private Vector3[] _basePoints;
        private float _cellSize;
        private float _travelDistance;
        private Camera _mainCam;

        private Tween _scaleTween;
        private Tween _colorTween;
        private Tween _focusGlowTween;
        private Sequence _actionSequence;

        private bool _isMarkedAsWrong = false;

        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();
        private Vector3[] _renderPositionsCache = new Vector3[100];

        // --- SHADER FLASH PROPERTIES ---
        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");
        private MaterialPropertyBlock _mpb;
        private float _currentFlashIntensity;

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            _mainCam = Camera.main;

            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Stretch;

            _mpb = new MaterialPropertyBlock();
        }

        public void OnSpawn()
        {
            KillAllActiveTweens();

            _currentState = ArrowState.Idle;
            _travelDistance = 0f;
            _isMarkedAsWrong = false;

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localScale = Vector3.one;

            // Sẽ được set lại ngay trong Setup, gọi ở đây cho an toàn
            ResetColor(); 
            headSpriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);

            SetFlashIntensity(0f);

            if (lineDirection != null) lineDirection.enabled = false;

            if (escapeTrail != null)
            {
                escapeTrail.emitting = false;
                escapeTrail.Clear();
            }
        }

        public void OnDespawn()
        {
            KillAllActiveTweens();
            lineRenderer.positionCount = 0;
            _rawPointsCache.Clear();
            _finalPointsCache.Clear();
            if (lineDirection != null) lineDirection.enabled = false;

            if (escapeTrail != null)
            {
                escapeTrail.emitting = false;
                escapeTrail.Clear();
            }
        }

        // ĐÃ SỬA: Nhận tham số màu và theme
        public void Setup(List<ArrowData> sortedPath, float cellSize, Color assignedColor, ThemeConfigSO theme)
        {
            if (_currentState == ArrowState.Escaping) return;

            KillAllActiveTweens();

            _currentState = ArrowState.Idle;
            _isMarkedAsWrong = false;

            // Lưu trữ cấu hình màu cho mũi tên này
            _baseColor = assignedColor;
            _blockedColor = theme.arrowBlockedColor;
            _loseColor = theme.arrowLoseColor;

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

        // ĐÃ THÊM: Cập nhật màu Runtime khi user đổi Theme
        public void UpdateThemeColor(Color newBaseColor, ThemeConfigSO newTheme)
        {
            _baseColor = newBaseColor;
            _blockedColor = newTheme.arrowBlockedColor;
            _loseColor = newTheme.arrowLoseColor;

            if (_currentState == ArrowState.Idle)
            {
                ChangeColorSmooth(_baseColor, 0.4f);
            }
        }

        #region Animations

        public void PlayEscapeAnimation()
        {
            if (_currentState == ArrowState.Escaping) return;
            _currentState = ArrowState.Escaping;

            KillAllActiveTweens();
            if (lineDirection != null) lineDirection.enabled = false;

            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float distanceToEdge = CameraUtils.GetDistanceToEdge(_mainCam, headTransform.position, _escapeDirection);

            float targetDistance = distanceToEdge + totalBodyLength + (_cellSize * escapeExtraDistanceFactor);
            float moveDuration = targetDistance / escapeSpeed;

            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            float flashUpTime = 0.02f;
            float flashDownTime = 0.1f;
            float maxFlashIntensity = 2f;

            _actionSequence.Insert(0f, DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x),
                maxFlashIntensity, flashUpTime).SetEase(Ease.OutFlash));

            _actionSequence.Insert(flashUpTime, DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x),
                0f, flashDownTime).SetEase(Ease.InQuad));

            _actionSequence.Append(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, pullbackOffset, pullbackDuration).SetEase(Ease.OutQuad));

            _actionSequence.AppendCallback(() =>
            {
                if (escapeTrail != null) escapeTrail.emitting = true;
            });

            _actionSequence.Append(
                DOTween.To(() => _travelDistance, x =>
                    {
                        _travelDistance = x;
                        UpdateSnakeBody();
                    }, targetDistance, moveDuration)
                    .SetEase(Ease.InCubic)
                    .OnStart(() => EventManager<VisualEventID>.Post(VisualEventID.ArrowEscaped))
            );

            _actionSequence.Insert(pullbackDuration + (moveDuration * fadeOutRatio),
                headSpriteRenderer.DOFade(0, moveDuration * (1f - fadeOutRatio)));

            _actionSequence.OnComplete(() =>
            {
                _currentState = ArrowState.Idle;
                if (escapeTrail != null) escapeTrail.emitting = false;
                PoolingManager.Instance.Despawn(gameObject);
            });
        }

        public void PlayBlockedAnimation(float realBumpDistance)
        {
            if (_currentState == ArrowState.Escaping || _currentState == ArrowState.Spawning) return;
            _currentState = ArrowState.Blocked;
            _isMarkedAsWrong = true;

            KillAllActiveTweens();

            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localScale = Vector3.one;
            }

            if (lineDirection != null) lineDirection.enabled = false;

            float bumpTime = baseBumpTime + (realBumpDistance * bumpDistMultiplier);
            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _actionSequence.Append(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, realBumpDistance, bumpTime).SetEase(Ease.OutQuad));

            // ĐÃ SỬA: Dùng _blockedColor thay vì biến cũ
            _actionSequence.Join(DOTween.To(() => headSpriteRenderer.color, x => SetColor(x), _blockedColor, bumpTime));
            _actionSequence.AppendCallback(() => EventManager<VisualEventID>.Post(VisualEventID.ArrowWrongImpact));

            Transform targetShake = visualRoot != null ? visualRoot : transform;

            _actionSequence.Append(targetShake.DOShakePosition(shakeDuration, shakeStrength)).SetId(this);
            _actionSequence.Join(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, 0f, reboundDuration).SetEase(Ease.OutBack, 1.5f));

            _actionSequence.OnComplete(() => _currentState = ArrowState.Idle);
        }

        public void PlaySpawnAnimation(float delay, float duration)
        {
            if (_currentState == ArrowState.Escaping) return;
            _currentState = ArrowState.Spawning;

            KillAllActiveTweens();

            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            _travelDistance = -totalBodyLength;
            UpdateSnakeBody();

            float revealDuration = duration * spawnRevealRatio;

            // ĐÃ SỬA: Dùng _baseColor
            headSpriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            headSpriteRenderer.DOFade(_baseColor.a, revealDuration).SetDelay(delay).SetId(this)
                .SetLink(headSpriteRenderer.gameObject);

            visualRoot.localScale = Vector3.zero;
            visualRoot.DOScale(1f, revealDuration).SetDelay(delay).SetEase(Ease.OutBack).SetId(this)
                .SetLink(visualRoot.gameObject);

            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _actionSequence.Insert(delay, DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, 0f, duration).SetEase(Ease.OutCubic));

            _actionSequence.OnComplete(() => _currentState = ArrowState.Idle);
        }

        public void PlayHoldEffect(bool isHolding)
        {
            if (_currentState != ArrowState.Idle) return;

            _scaleTween?.Kill();

            float targetScale = isHolding ? holdScaleTarget : 1.0f;
            float duration = isHolding ? holdScaleDurationIn : holdScaleDurationOut;
            Ease ease = isHolding ? Ease.OutBack : Ease.OutQuad;

            _scaleTween = visualRoot.DOScale(targetScale, duration)
                .SetId(this)
                .SetEase(ease)
                .OnUpdate(() =>
                {
                    if (isHolding) UpdateDirectionLine();
                })
                .SetLink(visualRoot.gameObject);

            if (lineDirection != null) lineDirection.enabled = isHolding;

            // ĐÃ SỬA: Tự động tính màu Hover sáng hơn màu gốc 30%
            Color dynamicHoverColor = new Color(
                Mathf.Clamp01(_baseColor.r * 1.3f),
                Mathf.Clamp01(_baseColor.g * 1.3f),
                Mathf.Clamp01(_baseColor.b * 1.3f), 
                _baseColor.a
            );

            ChangeColorSmooth(isHolding ? dynamicHoverColor : (_isMarkedAsWrong ? _blockedColor : _baseColor), duration);
        }

        public void PlayLoseAnimation()
        {
            if (_currentState == ArrowState.Escaping) return;

            _currentState = ArrowState.Idle;

            KillAllActiveTweens();

            // ĐÃ SỬA: Dùng _loseColor động thay vì hardcode
            ChangeColorSmooth(_loseColor, loseDuration);

            visualRoot.DOScale(loseScaleTarget, loseDuration)
                .SetId(this)
                .SetEase(loseEase)
                .SetLink(visualRoot.gameObject);
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
                    _rawPointsCache.Add(_basePoints[i]);
            }

            if (Vector3.Distance(_rawPointsCache[_rawPointsCache.Count - 1], headPos) > MIN_NODE_DISTANCE)
                _rawPointsCache.Add(headPos);
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
                if (_finalPointsCache.Count > _renderPositionsCache.Length)
                    Array.Resize(ref _renderPositionsCache, _finalPointsCache.Count * 2);
                for (int i = 0; i < _finalPointsCache.Count; i++)
                    _renderPositionsCache[i] = _finalPointsCache[i];
                lineRenderer.positionCount = _finalPointsCache.Count;
                lineRenderer.SetPositions(_renderPositionsCache);
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
            Vector3 safeDirection = _escapeDirection == Vector3.zero ? Vector3.up : _escapeDirection;
            if (distance >= maxPathDist) return _basePoints[maxIndex] + safeDirection * (distance - maxPathDist);
            if (distance <= 0)
            {
                if (_basePoints.Length > 1)
                    return _basePoints[0] + (_basePoints[0] - _basePoints[1]).normalized * Mathf.Abs(distance);
                else return _basePoints[0] - safeDirection * Mathf.Abs(distance);
            }

            int index = Mathf.FloorToInt(distance / _cellSize);
            float t = (distance % _cellSize) / _cellSize;
            return Vector3.Lerp(_basePoints[index], _basePoints[Mathf.Min(index + 1, maxIndex)], t);
        }

        #endregion

        #region Helpers

        private void KillAllActiveTweens()
        {
            _actionSequence?.Kill();
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _focusGlowTween?.Kill();
            DOTween.Kill(this);

            if (visualRoot != null) visualRoot.DOKill();
            if (headSpriteRenderer != null) headSpriteRenderer.DOKill();
            if (lineRenderer != null) lineRenderer.DOKill();

            _actionSequence = null;
            _scaleTween = null;
            _colorTween = null;
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

        // ĐÃ SỬA: Đưa về dùng _baseColor
        private void ResetColor() => SetColor(_baseColor);

        private void ChangeColorSmooth(Color targetColor, float duration)
        {
            _colorTween?.Kill();
            Color startColor = headSpriteRenderer.color;
            _colorTween = DOTween.To(() => startColor, x => SetColor(x), targetColor, duration)
                .SetId(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

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

        private void OnDestroy()
        {
            KillAllActiveTweens();
        }

        #endregion

        public void SetFlashIntensity(float intensity)
        {
            _currentFlashIntensity = intensity;

            if (lineRenderer != null)
            {
                lineRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                lineRenderer.SetPropertyBlock(_mpb);
            }

            if (headSpriteRenderer != null)
            {
                headSpriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                headSpriteRenderer.SetPropertyBlock(_mpb);
            }
        }

        #region Booster Visuals

        public void PlayHintEffect()
        {
            if (_currentState != ArrowState.Idle) return;
            visualRoot.DOKill();
            visualRoot.localScale = Vector3.one;
            visualRoot.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 1f).SetLink(visualRoot.gameObject);
            
            // Tính Hover Color
            Color dynamicHoverColor = new Color(
                Mathf.Clamp01(_baseColor.r * 1.3f),
                Mathf.Clamp01(_baseColor.g * 1.3f),
                Mathf.Clamp01(_baseColor.b * 1.3f), 
                _baseColor.a
            );

            ChangeColorSmooth(dynamicHoverColor, 0.2f);
            DOVirtual.DelayedCall(0.5f, ResetColor).SetLink(gameObject);
        }

        public void ForceToggleDirectionLine(bool isOn, float delay = 0f)
        {
            if (lineDirection == null) return;

            lineDirection.DOKill();
            visualRoot.DOKill();

            if (isOn)
            {
                visualRoot.localScale = Vector3.one;
                lineDirection.enabled = true;

                Vector3 worldStart = headTransform.position + _escapeDirection * (_cellSize * 0.1f);
                float distance = CameraUtils.GetDistanceToEdge(_mainCam, worldStart, _escapeDirection);
                Vector3 worldEnd = worldStart + _escapeDirection * distance;

                lineDirection.positionCount = 2;
                lineDirection.SetPosition(0, worldStart);
                lineDirection.SetPosition(1, worldStart); 

                Sequence toggleSeq = DOTween.Sequence()
                    .SetId(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                if (delay > 0)
                {
                    toggleSeq.AppendInterval(delay);
                }

                toggleSeq.Append(visualRoot.DOPunchScale(Vector3.one * 0.15f, 0.2f, 1, 0.5f));

                toggleSeq.Append(DOVirtual.Float(0f, 1f, 0.2f, (t) =>
                {
                    if (lineDirection != null)
                    {
                        lineDirection.SetPosition(1, Vector3.Lerp(worldStart, worldEnd, t));
                    }
                }).SetEase(Ease.OutQuad));
            }
            else
            {
                lineDirection.enabled = false;
            }
        }
        
        public void ToggleTargetSelectionState(bool isSelecting)
        {
            if (_currentState != ArrowState.Idle) return;

            // Dọn dẹp các Tween đang chạy để tránh xung đột
            visualRoot.DOKill(); 
            _focusGlowTween?.Kill();

            if (isSelecting)
            {
                visualRoot.localScale = Vector3.one;
                visualRoot.DOScale(1.03f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetId(this)
                    .SetLink(visualRoot.gameObject);
                
                Color pulseColor = new Color(
                    Mathf.Min(_baseColor.r * 1.5f, 2f), 
                    Mathf.Min(_baseColor.g * 1.5f, 2f), 
                    Mathf.Min(_baseColor.b * 1.5f, 2f), 
                    _baseColor.a
                );
                ChangeColorSmooth(pulseColor, 0.3f);

                SetFlashIntensity(0f); // Reset trước khi chạy
                _focusGlowTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 0.65f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo) 
                    .SetEase(Ease.InOutSine)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            else
            {
                visualRoot.localScale = Vector3.one;
                ResetColor();
                
                SetFlashIntensity(0f); 
            }
        }
        
        
        
        public Vector3 HeadPosition => headTransform != null ? headTransform.position : transform.position;
        public Vector3 EscapeDirection => _escapeDirection;
        #endregion
    }
}