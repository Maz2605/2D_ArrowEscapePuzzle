using System;
using System.Collections.Generic;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public sealed class ArrowLineView : MonoBehaviour, IPoolable
    {
        private const float MinNodeDistance = 0.02f;
        private const float DotThreshold = 0.99f;

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

        [Header("--- 8. FLASH INTENSITY (VISUAL) ---")]
        [SerializeField] private float escapeFlashIntensity = 2f;
        [SerializeField] private float escapeFlashPeakIntensity = 0.8f;
        [SerializeField] private float collisionFlashIntensity = 1.5f;
        [SerializeField] private float focusGlowIntensity = 0.8f;
        [SerializeField] private float hintGlowIntensity = 0.65f;
        [SerializeField] private float selectionGlowIntensity = 0.65f;

        [Header("--- 9. PORTAL CONTINUITY FEEL ---")]
        [SerializeField] private float teleportHeadCompressRatio = 0.12f;
        [SerializeField] private float teleportHeadReleaseRatio = 0.08f;
        [SerializeField] private float teleportFeelDistanceFactor = 0.28f;
        [SerializeField] private float teleportBoundaryHeadLengthFactor = 0.18f;

        public string ArrowID
        {
            get => _state.ArrowId;
            private set => _state.ArrowId = value;
        }

        public Vector3 HeadPosition => _context != null && _context.HeadTransform != null
            ? _context.HeadTransform.position
            : transform.position;

        public Vector3 EscapeDirection => _state.EscapeDirection;
        public Color BaseColor => _state.BaseColor;

        private readonly List<Vector3> _rawPointsCache = new List<Vector3>();
        private readonly List<Vector3> _finalPointsCache = new List<Vector3>();
        private readonly List<Vector3> _directionPointsCache = new List<Vector3>();
        private readonly List<Vector3> _secondaryDirectionPointsCache = new List<Vector3>();
        private readonly List<ArrowLinePathModel.VisibleBodyChunk> _visibleBodyChunksCache = new List<ArrowLinePathModel.VisibleBodyChunk>();

        private ArrowLineViewContext _context;
        private ArrowLineRuntimeState _state;
        private ArrowLinePathPresenter _pathPresenter;
        private ArrowLineRendererPool _rendererPool;
        private ArrowTrailPool _trailPool;
        private ArrowLineAppearanceController _appearance;
        private ArrowLineAnimationCoordinator _animationCoordinator;
        private Vector3[] _renderPositionsCache = new Vector3[64];

        internal ArrowLineViewContext Context => _context;
        internal ArrowLineRuntimeState State => _state;
        internal ArrowLinePathPresenter PathPresenter => _pathPresenter;
        internal ArrowLineRendererPool RendererPool => _rendererPool;
        internal ArrowTrailPool TrailPool => _trailPool;
        internal ArrowLineAppearanceController Appearance => _appearance;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void OnSpawn()
        {
            EnsureInitialized();
            KillAllActiveTweens();

            _state.ResetForSpawn();
            _pathPresenter.Clear();
            ClearFrameCaches();

            if (_context.VisualRoot != null)
            {
                _context.VisualRoot.localPosition = Vector3.zero;
                _context.VisualRoot.localScale = Vector3.one;
            }

            _appearance.ResetVisualState(_state.BaseColor);

            if (_context.HeadSpriteRenderer != null)
            {
                Color headColor = _state.BaseColor;
                headColor.a = 0f;
                _context.HeadSpriteRenderer.color = headColor;
            }

            if (_context.SecondaryEndpointMarker != null)
            {
                _context.SecondaryEndpointMarker.enabled = false;
            }

            _rendererPool.ClearAll();
            _trailPool.ClearAll();
        }

        public void OnDespawn()
        {
            EnsureInitialized();
            KillAllActiveTweens();

            ClearFrameCaches();
            _state.ResetForDespawn();
            _pathPresenter.Clear();
            _rendererPool.ClearAll();
            _trailPool.ClearAll();
            _appearance.ResetHeadScale();

            if (_context.SecondaryEndpointMarker != null)
            {
                _context.SecondaryEndpointMarker.enabled = false;
            }
        }

        public void Setup(ArrowModel arrowModel, List<ArrowData> sortedPath, float cellSize, ThemeConfigSO theme)
        {
            EnsureInitialized();
            if (_state.CurrentState == ArrowLineVisualState.Escaping) return;

            KillAllActiveTweens();
            _state.CurrentState = ArrowLineVisualState.Idle;
            _state.IsMarkedAsWrong = false;
            _state.CellSize = cellSize;

            ArrowID = arrowModel != null
                ? arrowModel.ArrowId
                : (sortedPath != null && sortedPath.Count > 0 ? sortedPath[0].ID : "unknown");

            ResolveBaseColor(theme);

            if (arrowModel != null && arrowModel.Path != null && arrowModel.Path.Count > 0)
            {
                _state.GridPath.Clear();
                _state.GridPath.AddRange(arrowModel.Path);
                _state.AvailableEndpoints.Clear();

                if (arrowModel.Endpoints != null)
                {
                    for (int i = 0; i < arrowModel.Endpoints.Count; i++)
                    {
                        if (arrowModel.Endpoints[i] != null)
                        {
                            _state.AvailableEndpoints.Add(arrowModel.Endpoints[i]);
                        }
                    }
                }

                _state.PrimaryEndpointPathIndex = arrowModel.PrimaryEndpoint != null
                    ? arrowModel.PrimaryEndpoint.PathIndex
                    : _state.GridPath.Count - 1;

                RecalculateBoundsFromGridPath();
                ConfigureEndpointVisual(arrowModel.PrimaryEndpoint);
            }
            else if (sortedPath != null && sortedPath.Count > 0)
            {
                _state.GridPath.Clear();
                _state.AvailableEndpoints.Clear();
                _state.PrimaryEndpointPathIndex = -1;
                _state.ActiveEndpointPathIndex = -1;

                if (_context.SecondaryEndpointMarker != null)
                {
                    _context.SecondaryEndpointMarker.enabled = false;
                }

                List<ArrowData> orderedPath = GetVisualOrderedPath(sortedPath);

                Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0f);
                Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0f);
                for (int i = 0; i < orderedPath.Count; i++)
                {
                    Vector3 pos = new Vector3(orderedPath[i].X * cellSize, orderedPath[i].Y * cellSize, 0f);
                    minBounds = Vector3.Min(minBounds, pos);
                    maxBounds = Vector3.Max(maxBounds, pos);
                }

                _state.CenterPivot = (minBounds + maxBounds) * 0.5f;
                transform.localPosition = _state.CenterPivot;

                _state.BodyPoints = new Vector3[orderedPath.Count];
                for (int i = 0; i < orderedPath.Count; i++)
                {
                    _state.BodyPoints[i] = new Vector3(orderedPath[i].X * cellSize, orderedPath[i].Y * cellSize, 0f) - _state.CenterPivot;
                }

                ArrowData headData = orderedPath[orderedPath.Count - 1];
                _state.CurrentHeadRotation = Quaternion.Euler(0f, 0f, GetHeadRotation(headData.Type));
                if (_context.HeadTransform != null)
                {
                    _context.HeadTransform.localRotation = _state.CurrentHeadRotation;
                }

                _state.DefaultEscapeDirection = GetDirectionVector(headData.Type);
            }

            ClearTraceRoute();
            UpdateSnakeBody();
            DisableDirectionRenderers();
        }

        public void UpdateThemeColor(ThemeConfigSO newTheme)
        {
            EnsureInitialized();
            ResolveBaseColor(newTheme);

            if (_state.CurrentState == ArrowLineVisualState.Idle)
            {
                _animationCoordinator.ChangeColorSmooth(_state.BaseColor, 0.4f);
            }

            if (_context.SecondaryEndpointMarker != null && _context.SecondaryEndpointMarker.enabled)
            {
                _context.SecondaryEndpointMarker.color = _state.BaseColor;
            }
        }

        public void SetTraceRoute(EscapeTraceResult traceResult)
        {
            SetTraceRoutes(traceResult, null);
        }

        public void SetTraceRoutes(EscapeTraceResult traceResult, EscapeTraceResult secondaryGuideTraceResult)
        {
            EnsureInitialized();

            _state.ActiveTraceResult = traceResult;
            _state.SecondaryGuideTraceResult = secondaryGuideTraceResult;

            if (_state.BodyPoints == null || _state.BodyPoints.Length == 0)
            {
                return;
            }

            if (_state.GridPath.Count > 0)
            {
                ConfigureEndpointVisual(traceResult != null
                    ? ResolveEndpoint(traceResult.StartPathIndex)
                    : ResolveEndpoint(_state.PrimaryEndpointPathIndex));
            }

            _state.EscapeDirection = traceResult != null
                ? traceResult.FinalDirection.ToVector3()
                : _state.DefaultEscapeDirection;

            _pathPresenter.RebuildPath(_state.BodyPoints, traceResult, GridToLocalPoint, _state.CellSize,
                GetActiveEndpointGridPosition());

            UpdateSecondaryEndpointStateForTrace(traceResult);
            UpdateSnakeBody();
            UpdateDirectionLineIfEnabled();
        }

        public void UpdateDirectionLineIfEnabled()
        {
            EnsureInitialized();
            if (_context.PrimaryDirectionRenderer != null && _context.PrimaryDirectionRenderer.enabled)
            {
                UpdateDirectionLine();
            }
        }

        public void ClearTraceRoute()
        {
            EnsureInitialized();

            _state.ActiveTraceResult = null;
            _state.SecondaryGuideTraceResult = null;

            if (_state.GridPath.Count > 0)
            {
                int targetPathIndex = _state.ActiveEndpointPathIndex >= 0
                    ? _state.ActiveEndpointPathIndex
                    : _state.PrimaryEndpointPathIndex;
                ConfigureEndpointVisual(ResolveEndpoint(targetPathIndex));
            }

            _state.EscapeDirection = _state.DefaultEscapeDirection;
            _pathPresenter.RebuildPath(_state.BodyPoints, null, GridToLocalPoint, _state.CellSize,
                GetActiveEndpointGridPosition());
        }

        public void SetActiveEndpoint(int selectedPathIndex)
        {
            EnsureInitialized();
            if (_state.GridPath.Count == 0) return;

            ArrowEndpoint selectedEndpoint = ResolveEndpoint(selectedPathIndex);
            if (selectedEndpoint == null) return;

            ConfigureEndpointVisual(selectedEndpoint);
            _pathPresenter.RebuildPath(_state.BodyPoints, null, GridToLocalPoint, _state.CellSize,
                GetActiveEndpointGridPosition());
            UpdateSnakeBody();
        }

        public void PlaySpawnAnimation(float delay, float duration, bool showLine = false, Action onComplete = null)
        {
            EnsureInitialized();
            _animationCoordinator.PlaySpawnAnimation(delay, duration, showLine, onComplete);
        }

        public void PlayEscapeAnimation(float startDelay = 0f, Action onEscapeStart = null, Action onEscapeComplete = null)
        {
            EnsureInitialized();
            _animationCoordinator.PlayEscapeAnimation(startDelay, onEscapeStart, onEscapeComplete);
        }

        public void PlayBlockedAnimation(float realBumpDistance, Action onImpact = null)
        {
            EnsureInitialized();
            _animationCoordinator.PlayBlockedAnimation(realBumpDistance, onImpact);
        }

        public void PlayCollisionFlash()
        {
            EnsureInitialized();
            _animationCoordinator.PlayCollisionFlash();
        }

        public void PlayHoldEffect(bool isHolding)
        {
            EnsureInitialized();
            _animationCoordinator.PlayHoldEffect(isHolding);
        }

        public void PlayLoseAnimation()
        {
            EnsureInitialized();
            _animationCoordinator.PlayLoseAnimation();
        }

        public void RestoreFromLose(float duration = 0.4f)
        {
            EnsureInitialized();
            _animationCoordinator.PlayRestoreFromLoseAnimation(duration);
        }

        public void PlayFocusHighlight(bool isOn)
        {
            EnsureInitialized();
            _animationCoordinator.PlayFocusHighlight(isOn);
        }

        public void PlayHintEffect()
        {
            EnsureInitialized();
            _animationCoordinator.PlayHintEffect();
        }

        public void ForceToggleDirectionLine(bool isOn, float delay = 0f)
        {
            EnsureInitialized();
            _animationCoordinator.ForceToggleDirectionLine(isOn, delay);
        }

        public void ToggleTargetSelectionState(bool isSelecting)
        {
            EnsureInitialized();
            _animationCoordinator.ToggleTargetSelectionState(isSelecting);
        }

        public void SetFlashIntensity(float intensity)
        {
            EnsureInitialized();
            _appearance.SetFlashIntensity(intensity);
        }

        internal ArrowEndpoint GetSecondaryEndpoint(int activeEndpointPathIndex)
        {
            for (int i = 0; i < _state.AvailableEndpoints.Count; i++)
            {
                ArrowEndpoint candidate = _state.AvailableEndpoints[i];
                if (candidate != null && candidate.PathIndex != activeEndpointPathIndex)
                {
                    return candidate;
                }
            }

            return null;
        }

        internal bool HasSecondaryEndpointForActivePath()
        {
            return GetSecondaryEndpoint(_state.ActiveEndpointPathIndex) != null;
        }

        internal void KillAllActiveTweens()
        {
            _animationCoordinator?.KillAllActiveTweens();
        }

        internal void ClearBodyRenderers()
        {
            _rendererPool?.ClearBodyRenderers();
        }

        internal void DisableDirectionRenderers()
        {
            _rendererPool?.ClearDirectionRenderers();
        }

        internal void ClearEscapeTrails()
        {
            _trailPool?.ClearAll();
        }

        internal void RefreshBody()
        {
            UpdateSnakeBody();
        }

        internal Quaternion GetHeadRotationFromDirection(Vector3 direction)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
            float targetAngle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, targetAngle - 90f);
        }

        private void EnsureInitialized()
        {
            if (_state != null) return;

            _state = new ArrowLineRuntimeState();
            _context = new ArrowLineViewContext(
                visualRoot,
                lineRenderer,
                headTransform,
                headSpriteRenderer,
                lineDirection,
                secondaryLineDirection,
                secondaryEndpointMarker,
                escapeTrail,
                Camera.main,
                escapeSpeed,
                pullbackOffset,
                pullbackDuration,
                fadeOutRatio,
                escapeExtraDistanceFactor,
                bumpDuration,
                shakeDuration,
                shakeStrength,
                spawnRevealRatio,
                holdScaleTarget,
                holdScaleDurationIn,
                holdScaleDurationOut,
                loseScaleTarget,
                loseDuration,
                loseEase,
                spawnScaleCurve,
                spawnMoveCurve,
                bumpCurve,
                escapeMoveCurve,
                escapeFlashIntensity,
                escapeFlashPeakIntensity,
                collisionFlashIntensity,
                focusGlowIntensity,
                hintGlowIntensity,
                selectionGlowIntensity,
                teleportHeadCompressRatio,
                teleportHeadReleaseRatio,
                teleportFeelDistanceFactor,
                teleportBoundaryHeadLengthFactor);

            if (_context.BodyRenderer == null)
            {
                Debug.LogError("[ArrowLineView] Missing LineRenderer reference. Please assign it in the prefab.");
                return;
            }

            _context.BodyRenderer.useWorldSpace = false;
            _context.BodyRenderer.alignment = LineAlignment.TransformZ;
            _context.BodyRenderer.textureMode = LineTextureMode.Stretch;
            _context.BodyRenderer.numCornerVertices = 5;

            _rendererPool = new ArrowLineRendererPool(_context);
            _trailPool = new ArrowTrailPool(_context.PrimaryEscapeTrail);
            _appearance = new ArrowLineAppearanceController(_context, _rendererPool, _trailPool);
            _pathPresenter = new ArrowLinePathPresenter();
            _animationCoordinator = new ArrowLineAnimationCoordinator(this);
        }

        private void ResolveBaseColor(ThemeConfigSO theme)
        {
            if (theme == null) return;

            if (!theme.isRandomArrowColor)
            {
                _state.BaseColor = theme.arrowDefaultColor;
            }
            else if (theme.arrowColorPalette != null && theme.arrowColorPalette.Count > 0)
            {
                int seed = Mathf.Abs(ArrowID.GetHashCode());
                _state.BaseColor = theme.arrowColorPalette[seed % theme.arrowColorPalette.Count];
            }
            else
            {
                _state.BaseColor = theme.arrowDefaultColor;
            }

            _state.BlockedColor = theme.arrowBlockedColor;
            _state.LoseColor = theme.arrowLoseColor;
            _appearance.ApplyColor(_state.BaseColor);
        }

        private void RecalculateBoundsFromGridPath()
        {
            Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, 0f);
            Vector3 maxBounds = new Vector3(float.MinValue, float.MinValue, 0f);

            for (int i = 0; i < _state.GridPath.Count; i++)
            {
                Vector3 pos = new Vector3(_state.GridPath[i].x * _state.CellSize, _state.GridPath[i].y * _state.CellSize, 0f);
                minBounds = Vector3.Min(minBounds, pos);
                maxBounds = Vector3.Max(maxBounds, pos);
            }

            _state.CenterPivot = (minBounds + maxBounds) * 0.5f;
            transform.localPosition = _state.CenterPivot;
        }

        private ArrowEndpoint ResolveEndpoint(int pathIndex)
        {
            for (int i = 0; i < _state.AvailableEndpoints.Count; i++)
            {
                if (_state.AvailableEndpoints[i].PathIndex == pathIndex)
                {
                    return _state.AvailableEndpoints[i];
                }
            }

            return _state.AvailableEndpoints.Count > 0 ? _state.AvailableEndpoints[0] : null;
        }

        private void ConfigureEndpointVisual(ArrowEndpoint endpoint)
        {
            if (_state.GridPath.Count == 0) return;

            endpoint ??= ResolveEndpoint(_state.PrimaryEndpointPathIndex);
            int endpointPathIndex = endpoint != null ? endpoint.PathIndex : Mathf.Max(0, _state.GridPath.Count - 1);
            _state.ActiveEndpointPathIndex = endpointPathIndex;

            bool reversePath = _state.GridPath.Count > 1 && endpointPathIndex == 0;
            int count = _state.GridPath.Count;
            _state.BodyPoints = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector2Int source = reversePath ? _state.GridPath[count - 1 - i] : _state.GridPath[i];
                _state.BodyPoints[i] = new Vector3(source.x * _state.CellSize, source.y * _state.CellSize, 0f) - _state.CenterPivot;
            }

            _state.DefaultEscapeDirection = endpoint != null ? endpoint.ExitDirection.ToVector3() : Vector3.up;
            _state.CurrentHeadRotation = GetHeadRotationFromDirection(_state.DefaultEscapeDirection);
            if (_context.HeadTransform != null)
            {
                _context.HeadTransform.localRotation = _state.CurrentHeadRotation;
            }

            UpdateSecondaryEndpointMarker(endpointPathIndex);
        }

        private void UpdateSecondaryEndpointMarker(int activeEndpointPathIndex)
        {
            if (_context.SecondaryEndpointMarker == null) return;

            if (_context.HeadSpriteRenderer != null)
            {
                _context.SecondaryEndpointMarker.sprite = _context.HeadSpriteRenderer.sprite;
                _context.SecondaryEndpointMarker.sharedMaterial = _context.HeadSpriteRenderer.sharedMaterial;
                _context.SecondaryEndpointMarker.sortingLayerID = _context.HeadSpriteRenderer.sortingLayerID;
                _context.SecondaryEndpointMarker.sortingOrder = _context.HeadSpriteRenderer.sortingOrder;
            }

            ArrowEndpoint secondaryEndpoint = GetSecondaryEndpoint(activeEndpointPathIndex);
            if (secondaryEndpoint == null)
            {
                _context.SecondaryEndpointMarker.enabled = false;
                return;
            }

            _context.SecondaryEndpointMarker.enabled = true;
            _context.SecondaryEndpointMarker.transform.localPosition = GridToLocalPoint(secondaryEndpoint.Position);
            _context.SecondaryEndpointMarker.transform.localRotation = GetHeadRotationFromDirection(secondaryEndpoint.ExitDirection.ToVector3());
            _context.SecondaryEndpointMarker.transform.localScale = _appearance.DefaultHeadLocalScale;
            _context.SecondaryEndpointMarker.color = _state.BaseColor;
        }

        private void UpdateSecondaryEndpointStateForTrace(EscapeTraceResult traceResult)
        {
            if (_context.SecondaryEndpointMarker == null) return;
            _context.SecondaryEndpointMarker.enabled = GetSecondaryEndpoint(_state.ActiveEndpointPathIndex) != null;
        }

        private void UpdateSnakeBody()
        {
            if (!_pathPresenter.HasMovementPath) return;

            float headDist = _state.TravelDistance + _pathPresenter.BodyLength;
            Vector3 headPos = _pathPresenter.GetPointAtDistance(headDist, _state.EscapeDirection);

            float tailDist = _state.CurrentState switch
            {
                ArrowLineVisualState.Spawning => 0f,
                ArrowLineVisualState.Escaping => Mathf.Max(_state.TravelDistance, _context.PullbackOffset),
                _ => Mathf.Max(_state.TravelDistance, 0f)
            };

            Vector3 tailPos = _pathPresenter.GetPointAtDistance(tailDist, _state.EscapeDirection);

            if (_pathPresenter.HasPortalSegments)
            {
                ApplySegmentedPathToRenderer(headPos, headDist, tailPos, tailDist);
            }
            else
            {
                _pathPresenter.BuildContinuousPathPoints(tailPos, headPos, tailDist, headDist, _state.EscapeDirection,
                    _rawPointsCache, _finalPointsCache, MinNodeDistance, DotThreshold);
                ApplyPathToRenderer(headPos, headDist, tailPos, tailDist);
            }

            if (_state.CurrentState == ArrowLineVisualState.Escaping)
            {
                _pathPresenter.DispatchReachedTriggers(headDist, payload =>
                    EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.ArrowPassedGridPosition, payload));
            }
        }

        private void ApplyPathToRenderer(Vector3 headPos, float headDist, Vector3 tailPos, float tailDist)
        {
            if (_finalPointsCache.Count >= 2)
            {
                ApplyPointsToRenderer(_context.BodyRenderer, _finalPointsCache, false);
                _rendererPool.DisableUnusedBodyRenderers(1);
            }
            else
            {
                _rendererPool.DisableUnusedBodyRenderers(0);
            }

            UpdateHeadVisuals(headPos, headDist);
            UpdateSecondaryEndpointVisual(tailPos, tailDist);
            UpdateEscapeTrail(tailPos, tailDist);
        }

        private void ApplySegmentedPathToRenderer(Vector3 headPos, float headDist, Vector3 tailPos, float tailDist)
        {
            _pathPresenter.BuildVisibleBodyChunks(tailDist, headDist, _visibleBodyChunksCache);

            int activeRendererCount = 0;
            for (int i = 0; i < _visibleBodyChunksCache.Count; i++)
            {
                if (!_pathPresenter.BuildVisibleSegmentPoints(_visibleBodyChunksCache[i], _state.EscapeDirection,
                        _rawPointsCache, _finalPointsCache, MinNodeDistance, DotThreshold))
                {
                    continue;
                }

                LineRenderer targetRenderer = _rendererPool.GetBodyRenderer(activeRendererCount);
                ApplyPointsToRenderer(targetRenderer, _finalPointsCache, false);
                activeRendererCount++;
            }

            _rendererPool.DisableUnusedBodyRenderers(activeRendererCount);
            UpdateHeadVisuals(headPos, headDist);
            UpdateSecondaryEndpointVisual(tailPos, tailDist);
            UpdateEscapeTrail(tailPos, tailDist);
        }

        private void UpdateHeadVisuals(Vector3 headPos, float headDist)
        {
            if (_context.HeadTransform != null)
            {
                _context.HeadTransform.localPosition = headPos;
            }

            UpdateHeadRotation(headPos, headDist);
            _appearance.UpdateTeleportHeadFeel(_state.ActiveTraceResult, _pathPresenter, _state.BodyPoints, _state.CellSize,
                headDist);
        }

        private void UpdateEscapeTrail(Vector3 tailPos, float tailDist)
        {
            if (_context.PrimaryEscapeTrail == null) return;

            if (_state.CurrentState == ArrowLineVisualState.Escaping && _pathPresenter.HasPortalSegments)
            {
                int tailSegmentIndex = _pathPresenter.GetSegmentIndexForDistance(tailDist, true);
                _trailPool.UpdateEscapeTrail(true, tailSegmentIndex, tailPos);
                return;
            }

            if (_state.CurrentState != ArrowLineVisualState.Escaping)
            {
                _trailPool.ResetIdlePosition(tailPos);
                return;
            }

            _trailPool.UpdateEscapeTrail(false, 0, tailPos);
        }

        private void UpdateSecondaryEndpointVisual(Vector3 tailPos, float tailDist)
        {
            if (_state.CurrentState == ArrowLineVisualState.Escaping)
            {
                if (_context.SecondaryEndpointMarker != null)
                {
                    _context.SecondaryEndpointMarker.enabled = false;
                }
                return;
            }

            if (_context.SecondaryEndpointMarker == null) return;

            ArrowEndpoint secondaryEndpoint = GetSecondaryEndpoint(_state.ActiveEndpointPathIndex);
            if (secondaryEndpoint == null)
            {
                _context.SecondaryEndpointMarker.enabled = false;
                return;
            }

            _context.SecondaryEndpointMarker.enabled = true;
            _context.SecondaryEndpointMarker.transform.localPosition = tailPos;

            Vector3 tailLookDirection = -_pathPresenter.GetDirectionAtDistance(tailDist, _state.EscapeDirection);
            if (tailLookDirection.sqrMagnitude <= 0.0001f)
            {
                tailLookDirection = secondaryEndpoint.ExitDirection.ToVector3();
            }

            _context.SecondaryEndpointMarker.transform.localRotation = GetHeadRotationFromDirection(tailLookDirection);
            _context.SecondaryEndpointMarker.transform.localScale = _appearance.DefaultHeadLocalScale;
        }

        private void UpdateHeadRotation(Vector3 headPos, float headDist)
        {
            int segmentIndex = _pathPresenter.GetSegmentIndexForDistance(headDist, true);
            Vector3 direction;

            if (segmentIndex >= 0)
            {
                ArrowLinePathModel.Segment segment = _pathPresenter.GetSegment(segmentIndex);
                float lookBackDistance = Mathf.Max(segment.StartDistance, headDist - 0.1f);
                Vector3 lookBackPos = _pathPresenter.GetPointAlongSegment(segmentIndex, lookBackDistance, _state.EscapeDirection,
                    segment.EndPointIndex == _pathPresenter.MovementPointCount - 1,
                    segment.StartPointIndex == 0);
                direction = (headPos - lookBackPos).normalized;
                if (direction == Vector3.zero)
                {
                    direction = _pathPresenter.GetDirectionForSegment(segmentIndex, _state.EscapeDirection);
                }
            }
            else
            {
                Vector3 lookBackPos = _pathPresenter.GetPointAtDistance(Mathf.Max(0f, headDist - 0.1f), _state.EscapeDirection);
                direction = (headPos - lookBackPos).normalized;
                if (direction == Vector3.zero)
                {
                    direction = _pathPresenter.GetDirectionAtDistance(headDist, _state.EscapeDirection);
                }
            }

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle - 90f);

            if (_state.CurrentState == ArrowLineVisualState.Spawning && headDist <= 0.05f)
            {
                _state.CurrentHeadRotation = targetRotation;
            }
            else
            {
                _state.CurrentHeadRotation = Quaternion.RotateTowards(_state.CurrentHeadRotation, targetRotation, 2000f * Time.deltaTime);
            }

            if (_context.HeadTransform != null)
            {
                _context.HeadTransform.localRotation = _state.CurrentHeadRotation;
            }
        }

        private void UpdateDirectionLine()
        {
            ArrowEndpoint activeEndpoint = ResolveEndpoint(_state.ActiveEndpointPathIndex);
            Vector3 activeFallbackDirection = activeEndpoint != null
                ? activeEndpoint.ExitDirection.ToVector3()
                : (_state.EscapeDirection.sqrMagnitude > 0f ? _state.EscapeDirection.normalized : Vector3.up);

            UpdateDirectionLineRenderer(_context.PrimaryDirectionRenderer, false,
                _context.HeadTransform != null ? _context.HeadTransform.position : transform.position,
                _state.ActiveTraceResult, activeFallbackDirection, _state.DirectionLineProgress, _directionPointsCache);

            ArrowEndpoint secondaryEndpoint = GetSecondaryEndpoint(_state.ActiveEndpointPathIndex);
            if (_context.SecondaryDirectionRenderer == null) return;

            if (secondaryEndpoint == null || _context.SecondaryEndpointMarker == null || !_context.SecondaryEndpointMarker.enabled)
            {
                _context.SecondaryDirectionRenderer.positionCount = 0;
                _context.SecondaryDirectionRenderer.enabled = false;
                _rendererPool.DisableExtraDirectionRenderers(true);
                return;
            }

            UpdateDirectionLineRenderer(_context.SecondaryDirectionRenderer, true,
                _context.SecondaryEndpointMarker.transform.position, _state.SecondaryGuideTraceResult,
                secondaryEndpoint.ExitDirection.ToVector3(), _state.DirectionLineProgress, _secondaryDirectionPointsCache);
        }

        private void UpdateDirectionLineRenderer(LineRenderer primaryRenderer, bool secondary, Vector3 worldStart,
            EscapeTraceResult traceResult, Vector3 fallbackDirection, float progress, List<Vector3> pointsCache)
        {
            if (primaryRenderer == null) return;

            primaryRenderer.useWorldSpace = true;
            _rendererPool.DisableExtraDirectionRenderers(secondary);
            pointsCache.Clear();
            pointsCache.Add(worldStart);

            Vector3 exitDirection = fallbackDirection.sqrMagnitude > 0f ? fallbackDirection.normalized : Vector3.up;
            if (traceResult != null)
            {
                exitDirection = traceResult.FinalDirection.ToVector3();
                if (exitDirection.sqrMagnitude <= 0.0001f)
                {
                    exitDirection = fallbackDirection.sqrMagnitude > 0f ? fallbackDirection.normalized : Vector3.up;
                }
            }

            if (traceResult != null && traceResult.RouteWaypoints != null && traceResult.RouteWaypoints.Count > 0)
            {
                bool containsTeleport = false;
                for (int i = 0; i < traceResult.RouteWaypoints.Count; i++)
                {
                    if (traceResult.RouteWaypoints[i].IsTeleportExit)
                    {
                        containsTeleport = true;
                        break;
                    }
                }

                if (containsTeleport && _pathPresenter.HasPortalSegments)
                {
                    RenderSplitDirectionLine(primaryRenderer, secondary, worldStart, exitDirection, progress, pointsCache);
                    return;
                }

                for (int i = 0; i < traceResult.RouteWaypoints.Count; i++)
                {
                    pointsCache.Add(transform.TransformPoint(GridToLocalPoint(traceResult.RouteWaypoints[i].Position)));
                }

                Vector3 lastPoint = pointsCache[pointsCache.Count - 1];
                float distanceToEdge = ArrowGame.Utils.CameraUtils.GetDistanceToEdge(_context.MainCamera, lastPoint, exitDirection);
                pointsCache.Add(lastPoint + exitDirection * distanceToEdge);
            }
            else
            {
                Vector3 offsetStart = worldStart + exitDirection * (_state.CellSize * 0.1f);
                float distance = ArrowGame.Utils.CameraUtils.GetDistanceToEdge(_context.MainCamera, offsetStart, exitDirection);
                pointsCache[0] = offsetStart;
                pointsCache.Add(offsetStart + exitDirection * distance);
            }

            TrimPointsByProgress(pointsCache, progress);
            ApplyPointsToRenderer(primaryRenderer, pointsCache, true);
        }

        private void RenderSplitDirectionLine(LineRenderer primaryRenderer, bool secondary, Vector3 worldStart,
            Vector3 exitDirection, float progress, List<Vector3> pointsCache)
        {
            if (primaryRenderer == null || !_pathPresenter.HasMovementPath || _pathPresenter.SegmentCount == 0)
            {
                return;
            }

            float totalLength = 0f;
            for (int i = 0; i < _pathPresenter.SegmentCount; i++)
            {
                if (!_pathPresenter.TryBuildStaticSegmentPoints(i, worldStart, i == 0, transform.TransformPoint, pointsCache))
                {
                    continue;
                }

                if (i == _pathPresenter.SegmentCount - 1)
                {
                    Vector3 lastPoint = pointsCache[pointsCache.Count - 1];
                    float distanceToEdge = ArrowGame.Utils.CameraUtils.GetDistanceToEdge(_context.MainCamera, lastPoint, exitDirection);
                    pointsCache.Add(lastPoint + exitDirection * distanceToEdge);
                }

                totalLength += _pathPresenter.CalculatePointListLength(pointsCache);
            }

            float remainingLength = totalLength * Mathf.Clamp01(progress);
            int activeRendererCount = 0;
            for (int i = 0; i < _pathPresenter.SegmentCount; i++)
            {
                if (!_pathPresenter.TryBuildStaticSegmentPoints(i, worldStart, i == 0, transform.TransformPoint, pointsCache))
                {
                    continue;
                }

                if (i == _pathPresenter.SegmentCount - 1)
                {
                    Vector3 lastPoint = pointsCache[pointsCache.Count - 1];
                    float distanceToEdge = ArrowGame.Utils.CameraUtils.GetDistanceToEdge(_context.MainCamera, lastPoint, exitDirection);
                    pointsCache.Add(lastPoint + exitDirection * distanceToEdge);
                }

                float segmentLength = _pathPresenter.CalculatePointListLength(pointsCache);
                if (remainingLength <= 0f)
                {
                    break;
                }

                if (remainingLength < segmentLength)
                {
                    _pathPresenter.TrimPointListToLength(pointsCache, remainingLength);
                    segmentLength = remainingLength;
                }

                LineRenderer target = _rendererPool.GetDirectionRenderer(activeRendererCount, secondary);
                ApplyPointsToRenderer(target, pointsCache, true);
                activeRendererCount++;

                remainingLength -= segmentLength;
                if (remainingLength <= 0f)
                {
                    break;
                }
            }

            if (activeRendererCount == 0)
            {
                primaryRenderer.positionCount = 0;
            }

            _rendererPool.DisableUnusedDirectionRenderers(secondary, activeRendererCount);
        }

        private void TrimPointsByProgress(List<Vector3> pointsCache, float progress)
        {
            if (progress >= 0.999f || pointsCache.Count < 2) return;

            float totalLength = 0f;
            for (int i = 0; i < pointsCache.Count - 1; i++)
            {
                totalLength += Vector3.Distance(pointsCache[i], pointsCache[i + 1]);
            }

            float targetLength = totalLength * progress;
            float currentLength = 0f;
            int finalCount = pointsCache.Count;

            for (int i = 0; i < pointsCache.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(pointsCache[i], pointsCache[i + 1]);
                if (currentLength + segmentLength >= targetLength)
                {
                    float remaining = targetLength - currentLength;
                    Vector3 direction = (pointsCache[i + 1] - pointsCache[i]).normalized;
                    pointsCache[i + 1] = pointsCache[i] + direction * remaining;
                    finalCount = i + 2;
                    break;
                }

                currentLength += segmentLength;
            }

            if (finalCount < pointsCache.Count)
            {
                pointsCache.RemoveRange(finalCount, pointsCache.Count - finalCount);
            }
        }

        private void ApplyPointsToRenderer(LineRenderer targetRenderer, List<Vector3> points, bool useWorldSpace)
        {
            if (targetRenderer == null) return;

            targetRenderer.useWorldSpace = useWorldSpace;
            if (points == null || points.Count < 2)
            {
                targetRenderer.positionCount = 0;
                return;
            }

            if (!targetRenderer.enabled)
            {
                targetRenderer.enabled = true;
            }

            if (points.Count > _renderPositionsCache.Length)
            {
                Array.Resize(ref _renderPositionsCache, points.Count * 2);
            }

            for (int i = 0; i < points.Count; i++)
            {
                _renderPositionsCache[i] = points[i];
            }

            targetRenderer.positionCount = points.Count;
            targetRenderer.SetPositions(_renderPositionsCache);
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

        private static bool IsHeadType(CellType type)
        {
            return type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
                   type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;
        }

        private static float GetHeadRotation(CellType type)
        {
            return type switch
            {
                CellType.ArrowHeadUp => 0f,
                CellType.ArrowHeadRight => -90f,
                CellType.ArrowHeadDown => 180f,
                CellType.ArrowHeadLeft => 90f,
                _ => 0f
            };
        }

        private static Vector3 GetDirectionVector(CellType type)
        {
            return type switch
            {
                CellType.ArrowHeadUp => Vector3.up,
                CellType.ArrowHeadRight => Vector3.right,
                CellType.ArrowHeadDown => Vector3.down,
                CellType.ArrowHeadLeft => Vector3.left,
                _ => Vector3.zero
            };
        }

        private Vector2Int GetActiveEndpointGridPosition()
        {
            if (_state.ActiveEndpointPathIndex >= 0 && _state.ActiveEndpointPathIndex < _state.GridPath.Count)
            {
                return _state.GridPath[_state.ActiveEndpointPathIndex];
            }

            return _state.GridPath.Count > 0 ? _state.GridPath[_state.GridPath.Count - 1] : Vector2Int.zero;
        }

        private Vector3 GridToLocalPoint(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * _state.CellSize, gridPosition.y * _state.CellSize, 0f) - _state.CenterPivot;
        }

        private void ClearFrameCaches()
        {
            _rawPointsCache.Clear();
            _finalPointsCache.Clear();
            _directionPointsCache.Clear();
            _secondaryDirectionPointsCache.Clear();
            _visibleBodyChunksCache.Clear();
        }

        private void OnDestroy()
        {
            KillAllActiveTweens();
        }
    }
}
