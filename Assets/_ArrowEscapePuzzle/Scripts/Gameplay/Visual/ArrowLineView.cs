using System.Collections.Generic;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Visual
{
    public partial class ArrowLineView : MonoBehaviour, IPoolable
    {
        private enum ArrowState
        {
            Idle,
            Spawning,
            Blocked,
            Escaping
        }

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

        [Header("--- 7. ANIMATION CURVES (GAME FEEL) ---")]
        [SerializeField] private AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve spawnMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve bumpImpactCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve bumpReboundCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve escapeMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public string ArrowID { get; private set; }
        public Vector3 HeadPosition => headTransform != null ? headTransform.position : transform.position;
        public Vector3 EscapeDirection => _escapeDirection;

        private ArrowState _currentState = ArrowState.Idle;
        private Color _baseColor;
        private Color _blockedColor;
        private Color _loseColor;

        private Vector3 _escapeDirection;
        private Vector3 _defaultEscapeDirection;
        private Vector3[] _bodyPoints;
        private Vector3[] _movementPoints;
        private float[] _movementDistances;
        private float _bodyLength;
        private float _movementLength;
        private float _cellSize;
        private float _travelDistance;
        private Camera _mainCam;
        private Quaternion _currentHeadRotation;
        private bool _isMarkedAsWrong;
        private Vector3 _centerPivot;
        private bool _isDirectionLinePersistent;
        private float _directionLineProgress = 1f;

        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();
        private readonly List<Vector3> _directionPointsCache = new List<Vector3>();
        private Vector3[] _renderPositionsCache = new Vector3[100];
        private Tween _scaleTween;
        private Tween _colorTween;
        private Tween _focusGlowTween;
        private Sequence _actionSequence;

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
            lineRenderer.numCornerVertices = 5;
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

            ResetColor();
            if (headSpriteRenderer != null)
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
            if (lineRenderer != null) lineRenderer.positionCount = 0;
            _rawPointsCache.Clear();
            _finalPointsCache.Clear();
            _directionPointsCache.Clear();
            if (lineDirection != null) lineDirection.enabled = false;

            if (escapeTrail != null)
            {
                escapeTrail.emitting = false;
                escapeTrail.Clear();
            }
        }

        private void OnDestroy() => KillAllActiveTweens();

        public void Setup(List<ArrowData> sortedPath, float cellSize, Color assignedColor, ThemeConfigSO theme)
        {
            if (_currentState == ArrowState.Escaping) return;
            KillAllActiveTweens();

            _currentState = ArrowState.Idle;
            _isMarkedAsWrong = false;
            _baseColor = assignedColor;
            _blockedColor = theme.arrowBlockedColor;
            _loseColor = theme.arrowLoseColor;

            ResetColor();

            if (sortedPath == null || sortedPath.Count == 0) return;

            List<ArrowData> orderedPath = GetVisualOrderedPath(sortedPath);
            ArrowID = orderedPath[0].ID;
            _cellSize = cellSize;

            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0f);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0f);

            foreach (ArrowData node in orderedPath)
            {
                Vector3 pos = new Vector3(node.X * cellSize, node.Y * cellSize, 0f);
                minBounds = Vector3.Min(minBounds, pos);
                maxBounds = Vector3.Max(maxBounds, pos);
            }

            _centerPivot = (minBounds + maxBounds) / 2f;
            transform.localPosition = _centerPivot;

            _bodyPoints = new Vector3[orderedPath.Count];
            for (int i = 0; i < orderedPath.Count; i++)
            {
                _bodyPoints[i] = new Vector3(orderedPath[i].X * cellSize, orderedPath[i].Y * cellSize, 0f) - _centerPivot;
            }

            ArrowData headData = orderedPath[orderedPath.Count - 1];
            _currentHeadRotation = Quaternion.Euler(0f, 0f, GetHeadRotation(headData.Type));
            headTransform.localRotation = _currentHeadRotation;
            _defaultEscapeDirection = GetDirectionVector(headData.Type);

            ClearTraceRoute();
            UpdateSnakeBody();

            if (lineDirection != null) lineDirection.enabled = false;
        }

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

        public void SetTraceRoute(EscapeTraceResult traceResult)
        {
            if (_bodyPoints == null || _bodyPoints.Length == 0)
            {
                return;
            }

            _escapeDirection = traceResult != null ? traceResult.FinalDirection.ToVector3() : _defaultEscapeDirection;
            BuildMovementPath(traceResult?.RouteWaypoints);
            UpdateSnakeBody();
            UpdateDirectionLineIfEnabled();
        }

        public void UpdateDirectionLineIfEnabled()
        {
            if (lineDirection != null && lineDirection.enabled)
            {
                UpdateDirectionLine();
            }
        }

        public void ClearTraceRoute()
        {
            _escapeDirection = _defaultEscapeDirection;
            BuildMovementPath(null);
        }

        private List<ArrowData> GetVisualOrderedPath(List<ArrowData> path)
        {
            if (path == null || path.Count <= 1) return path;

            bool firstIsHead = IsHeadType(path[0].Type);
            bool lastIsHead = IsHeadType(path[path.Count - 1].Type);

            if (firstIsHead && !lastIsHead)
            {
                List<ArrowData> reversed = new List<ArrowData>(path);
                reversed.Reverse();
                return reversed;
            }

            return path;
        }

        private bool IsHeadType(CellType type) =>
            type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
            type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;

        private void BuildMovementPath(IReadOnlyList<EscapeTraceWaypoint> routeWaypoints)
        {
            if (_bodyPoints == null || _bodyPoints.Length == 0)
            {
                _movementPoints = null;
                _movementDistances = null;
                _bodyLength = 0f;
                _movementLength = 0f;
                return;
            }

            List<Vector3> points = new List<Vector3>(_bodyPoints.Length + (routeWaypoints?.Count ?? 0));
            List<float> distances = new List<float>(_bodyPoints.Length + (routeWaypoints?.Count ?? 0));

            for (int i = 0; i < _bodyPoints.Length; i++)
            {
                points.Add(_bodyPoints[i]);
                distances.Add(i * _cellSize);
            }

            _bodyLength = (_bodyPoints.Length - 1) * _cellSize;
            float currentDistance = _bodyLength;

            if (routeWaypoints != null)
            {
                for (int i = 0; i < routeWaypoints.Count; i++)
                {
                    EscapeTraceWaypoint waypoint = routeWaypoints[i];
                    currentDistance += waypoint.StepCost * _cellSize;
                    points.Add(GridToLocalPoint(waypoint.Position));
                    distances.Add(currentDistance);
                }
            }

            _movementPoints = points.ToArray();
            _movementDistances = distances.ToArray();
            _movementLength = currentDistance;
        }

        private Vector3 GridToLocalPoint(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * _cellSize, gridPosition.y * _cellSize, 0f) - _centerPivot;
        }
    }
}
