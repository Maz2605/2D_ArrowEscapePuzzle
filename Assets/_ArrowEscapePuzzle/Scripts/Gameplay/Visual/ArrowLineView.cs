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
    public partial class ArrowLineView : MonoBehaviour, IPoolable
    {
        private enum ArrowState
        {
            Idle, Spawning, Blocked, Escaping
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
        [Tooltip("Độ nảy (Scale) khi mũi tên vừa xuất hiện")]
        [SerializeField] private AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Gia tốc trườn ra (Move) của thân mũi tên khi spawn")]
        [SerializeField] private AnimationCurve spawnMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Độ giật lùi khi đâm trúng chướng ngại vật (Blocked)")]
        [SerializeField] private AnimationCurve bumpImpactCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Độ đàn hồi khi dội ngược lại vị trí cũ sau khi đâm")]
        [SerializeField] private AnimationCurve bumpReboundCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Gia tốc phóng đi khi mũi tên trốn thoát (Escape)")]
        [SerializeField] private AnimationCurve escapeMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // --- STATE VARIABLES ---
        public string ArrowID { get; private set; }
        public Vector3 HeadPosition => headTransform != null ? headTransform.position : transform.position;
        public Vector3 EscapeDirection => _escapeDirection;

        private ArrowState _currentState = ArrowState.Idle;
        private Color _baseColor;
        private Color _blockedColor;
        private Color _loseColor;
        
        private Vector3 _escapeDirection;
        private Vector3[] _basePoints;
        private float _cellSize;
        private float _travelDistance;
        private Camera _mainCam;
        private Quaternion _currentHeadRotation; 
        private bool _isMarkedAsWrong = false;

        // --- CACHE & TWEEN ---
        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();
        private Vector3[] _renderPositionsCache = new Vector3[100];
        private Tween _scaleTween;
        private Tween _colorTween;
        private Tween _focusGlowTween;
        private Sequence _actionSequence;

        // --- SHADER PROPERTIES ---
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
            lineRenderer.numCapVertices = 5;

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
            
            // Dọn dẹp trail khi lấy từ Pool ra
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
            if (lineDirection != null) lineDirection.enabled = false;
            
            // Dọn dẹp trail khi cất vào Pool
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
            
            _currentHeadRotation = Quaternion.Euler(0, 0, GetHeadRotation(headData.Type));
            headTransform.localRotation = _currentHeadRotation;
            
            _escapeDirection = GetDirectionVector(headData.Type);

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
    }
}