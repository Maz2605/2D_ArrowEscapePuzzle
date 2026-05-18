using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
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
        [SerializeField] private LineRenderer secondaryLineDirection;
        [SerializeField] private SpriteRenderer secondaryEndpointMarker;
        [SerializeField] private TrailRenderer escapeTrail;

        [Header("--- 2. ANIMATION: ESCAPE ---")]
        [SerializeField] private float escapeSpeed = 25f;
        [SerializeField] private float pullbackOffset = -0.25f;
        [SerializeField] private float pullbackDuration = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float fadeOutRatio = 0.7f;
        [SerializeField] private float escapeExtraDistanceFactor = 1.2f;

        [Header("--- 3. ANIMATION: BLOCKED ---")]
        [SerializeField] private float bumpDuration = 0.2f;
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
        [SerializeField] private AnimationCurve bumpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1f, 0));
        [SerializeField] private AnimationCurve escapeMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        
        public string ArrowID { get; private set; }
        public Vector3 HeadPosition => headTransform != null ? headTransform.position : transform.position;
        public Vector3 EscapeDirection => _escapeDirection;
        
        public Color BaseColor => _baseColor;

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
        private LineRenderer _secondaryLineDirection;
        private SpriteRenderer _secondaryEndpointMarker;
        private Vector2Int _lastGridPos = new Vector2Int(-999, -999);
        private readonly List<Vector2Int> _gridPath = new List<Vector2Int>();
        private readonly List<ArrowEndpoint> _availableEndpoints = new List<ArrowEndpoint>();
        private int _primaryEndpointPathIndex = -1;
        private int _activeEndpointPathIndex = -1;
        private EscapeTraceResult _activeTraceResult;
        private EscapeTraceResult _secondaryGuideTraceResult;

        private struct PathTrigger
        {
            public float distance;
            public Vector2Int gridPos;
        }
        private readonly List<PathTrigger> _pathTriggers = new List<PathTrigger>();
        private int _currentTriggerIndex = 0;

        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();
        private readonly List<Vector3> _directionPointsCache = new List<Vector3>();
        private readonly List<Vector3> _secondaryDirectionPointsCache = new List<Vector3>();
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
            _mainCam = Camera.main;

            if (lineRenderer == null)
            {
                Debug.LogError("[ArrowLineView] Missing LineRenderer reference. Please assign it in the prefab.");
                return;
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.numCornerVertices = 5;
            _mpb = new MaterialPropertyBlock();
            _secondaryLineDirection = secondaryLineDirection;
            _secondaryEndpointMarker = secondaryEndpointMarker;
        }

        public void OnSpawn()
        {
            KillAllActiveTweens();
            _currentState = ArrowState.Idle;
            _travelDistance = 0f;
            _isMarkedAsWrong = false;
            _activeTraceResult = null;
            _secondaryGuideTraceResult = null;

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localScale = Vector3.one;

            ResetColor();
            if (headSpriteRenderer != null)
                headSpriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);

            SetFlashIntensity(0f);

            if (lineDirection != null) lineDirection.enabled = false;
            if (_secondaryLineDirection != null) _secondaryLineDirection.enabled = false;
            if (_secondaryEndpointMarker != null) _secondaryEndpointMarker.enabled = false;

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
            _secondaryDirectionPointsCache.Clear();
            _activeTraceResult = null;
            _secondaryGuideTraceResult = null;
            
            if (lineDirection != null) lineDirection.enabled = false;
            if (_secondaryLineDirection != null) _secondaryLineDirection.enabled = false;
            if (_secondaryEndpointMarker != null) _secondaryEndpointMarker.enabled = false;

            if (escapeTrail != null)
            {
                escapeTrail.emitting = false;
                escapeTrail.Clear();
            }
        }

        private void OnDestroy() => KillAllActiveTweens();
    }
}
