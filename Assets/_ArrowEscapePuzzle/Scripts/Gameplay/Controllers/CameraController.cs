using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using DG.Tweening;
using ArrowGame.Gameplay.Managers;
using ShareCore.Scripts;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem.EnhancedTouch;

namespace ArrowGame.Gameplay.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("Core References")] [SerializeField]
        private Camera mainCam;

        [Header("Dynamic Zoom Settings")]
        [Tooltip("Tỉ lệ zoom sâu nhất so với toàn bộ map (0.3 = chỉ nhìn thấy 30% map)")]
        [SerializeField, Range(0.1f, 0.8f)]
        private float minZoomRatio = 0.3f;

        [Tooltip("Giới hạn cứng để không bị zoom vào quá sát một pixel trên map siêu nhỏ")] [SerializeField]
        private float absoluteMinZoom = 3f;

        [SerializeField] private float pinchZoomSensitivity = 1f;
        [FormerlySerializedAs("zoomSpeed")]
        [SerializeField] private float mouseWheelZoomStepPercent = 0.6f;
        [SerializeField] private float zoomSmoothness = 10f;
        [SerializeField] private bool usePointerAnchoredWheelZoom = true;

        [Tooltip("Độ mượt mà của pinch zoom trên mobile")]
        [SerializeField] private float pinchZoomSmoothness = 15f;

        [Header("Grid Fitting Settings")]
        [Tooltip("Khoảng cách padding tùy chỉnh giữa viền camera và grid")]
        [SerializeField] private float gridPadding = 1.0f;

        [Tooltip("Hệ số tỉ lệ zoom camera chơi game (1.0 = fit grid chuẩn, < 1.0 = zoom sâu hơn)")]
        [SerializeField, Range(0.5f, 1.5f)] private float gameplayZoomScale = 1.0f;

        [Header("Pan & Inertia Settings")] [SerializeField]
        private float mapPadding = 3f;

        [SerializeField, Range(0f, 1f)] private float friction = 0.92f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10f);

        [Header("Intro Animation Settings")]
        [Tooltip("Độ lệch zoom ban đầu (số càng lớn thì camera ban đầu càng ở xa)")]
        [SerializeField] private float introZoomOffset = 8f;

        [Tooltip("Thời gian chạy intro zoom khi dùng Animation Curve")]
        [SerializeField] private float introZoomDuration = 1.5f;

        [Tooltip("Sử dụng Animation Curve để tùy chỉnh chuyển động zoom")]
        [SerializeField] private bool useIntroZoomCurve = false;

        [Tooltip("Animation Curve tùy chỉnh cho intro zoom (chỉ dùng khi useIntroZoomCurve = true)")]
        [SerializeField] private AnimationCurve introZoomCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Intro Step Animation Settings")]
        [Tooltip("Hệ số zoom vào sâu trong intro (số càng nhỏ zoom càng sâu/mạnh)")]
        [SerializeField, Range(0.1f, 1f)] private float introZoomInFactor = 0.5f;

        [Tooltip("Thời gian thực hiện zoom vào sâu")]
        [SerializeField] private float introZoomInDuration = 0.8f;

        [Tooltip("Thời gian thực hiện zoom trở lại để fit grid")]
        [SerializeField] private float introZoomOutDuration = 1.0f;

        [Tooltip("Kiểu easing khi zoom vào sâu")]
        [SerializeField] private Ease introZoomInEase = Ease.OutCubic;

        [Tooltip("Kiểu easing khi zoom ra fit grid (ví dụ OutBack để tạo cú giật nảy)")]
        [SerializeField] private Ease introZoomOutEase = Ease.OutBack;

        [Header("Intro Level-Based Zoom Settings")]
        [Tooltip("Mốc số lượng arrow để kích hoạt zoom sâu hơn")]
        [SerializeField] private int arrowCountThreshold = 30;

        [Tooltip("Hệ số zoom sâu thêm cho các level có số lượng arrow lớn hơn hoặc bằng mốc (ví dụ: 0.8 = zoom sâu thêm 20%)")]
        [SerializeField, Range(0.5f, 1f)] private float deepZoomMultiplierForLargeLevels = 0.8f;

        [Header("Reset View Settings")] [SerializeField]
        private float resetViewDuration = 0.35f;

        [Header("Shake Settings")] [SerializeField]
        private float wrongImpactShakeDuration = 0.18f;

        [SerializeField] private float wrongImpactShakeStrength = 0.12f;
        [SerializeField] private int wrongImpactShakeVibrato = 5;

        private Transform _camTransform;
        private Vector2 _mapSize;
        private Vector2 _mapCenter;
        private Vector3 _cameraBasePosition;
        private Vector3 _shakeOffset;

        private float _targetOrthographicSize;
        private float _initialOrthographicSize;
        private int _levelArrowCount;

        // --- CÁC BIẾN ZOOM ĐỘNG ---
        private float _dynamicMinZoom;
        private float _dynamicMaxZoom;
        // --------------------------

        // --- Pinch Zoom Variables ---
        private bool _isPinching = false;
        private float _initialPinchDistance;
        private float _initialOrthoSize;
        private Vector2 _initialPinchScreenCenter;
        private Vector3 _initialPinchWorldCenter;
        
        private float _camHalfHeight;
        private float _camHalfWidth;
        private bool _isBoundsDirty = true;
        private Vector2 _lastZoomScreenPosition;
        private bool _hasPendingPointerAnchor;

        private Vector3 _dragWorldOrigin;
        private Vector3 _lastWorldPos;
        private Vector3 _panVelocity;
        private bool _isDragging;

        private Tween _panTween;
        private Tween _zoomTween;
        private Tween _resetViewTween;
        private Tween _shakeTween;

        private bool _isIntroZooming = false;
        public bool IsIntroZooming => _isIntroZooming;
        private bool _isResettingView = false;

        private void Start()
        {
            if (mainCam == null) mainCam = Camera.main;
            _camTransform = mainCam.transform;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput += HandleZoomInput;
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged,
                HandleInGameStateChanged);
            EventManager<VisualEventID>.AddListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleArrowWrongImpact);
            EventManager<VisualEventID>.AddListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput -= HandleZoomInput;
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged,
                HandleInGameStateChanged);
            EventManager<VisualEventID>.RemoveListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleArrowWrongImpact);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
            KillAllTweens();
            _isPinching = false;
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            if (state == InGameState.Intro)
            {
                PlayIntroZoomAnimation();
            }
        }

        private void HandleTapArrowHit()
        {
            if (_isIntroZooming || _isResettingView) return;
            // PlayTapMicroShake();
        }

        private void HandleArrowWrongImpact(Vector3 impactPosition)
        {
            if (_isIntroZooming || _isResettingView) return;
            PlayWrongImpactShake();
        }

        private void LateUpdate()
        {
            if (_isIntroZooming || _isResettingView) return;

            if (InputManager.Instance != null && InputManager.Instance.GetTouchCount() == 2)
            {
                HandlePinchZoom();
            }
            else
            {
                if (_isPinching)
                {
                    _isPinching = false;
                    _isBoundsDirty = true;
                    _panVelocity = Vector3.zero;
                }

                HandleSmoothZoom();
                HandleInertia();
            }
        }

        public void InitializeCamera(int gridWidth, int gridHeight, float cellSize, int arrowCount = 0)
        {
            _levelArrowCount = arrowCount;
            float boardWidth = (gridWidth - 1) * cellSize;
            float boardHeight = (gridHeight - 1) * cellSize;

            _mapSize = new Vector2(boardWidth, boardHeight);
            _mapCenter = Vector2.zero;

            float sizeY = (boardHeight / 2f) + gridPadding;
            float sizeX = ((boardWidth / 2f) + gridPadding) / mainCam.aspect;

            float sizeNeededToFitMap = Mathf.Max(sizeX, sizeY);

            _dynamicMaxZoom = Mathf.Max(sizeNeededToFitMap, absoluteMinZoom);

            _dynamicMinZoom = Mathf.Max(absoluteMinZoom, _dynamicMaxZoom * minZoomRatio);

            _initialOrthographicSize = _dynamicMaxZoom * gameplayZoomScale;
            _targetOrthographicSize = _initialOrthographicSize;

            _cameraBasePosition = new Vector3(_mapCenter.x, _mapCenter.y, cameraOffset.z);
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();
            mainCam.orthographicSize = _targetOrthographicSize;

            _isBoundsDirty = true;
        }

        public void ResetView()
        {
            ResetView(resetViewDuration);
        }

        public void ResetView(float duration, Action onComplete = null)
        {
            if (_isIntroZooming)
            {
                onComplete?.Invoke();
                return;
            }

            _isResettingView = true;
            _isDragging = false;
            _panVelocity = Vector3.zero;
            _hasPendingPointerAnchor = false;

            KillTween(ref _panTween);
            KillTween(ref _zoomTween);
            KillTween(ref _resetViewTween);
            KillTween(ref _shakeTween);
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            _targetOrthographicSize = _initialOrthographicSize;

            Vector3 targetPos = new Vector3(_mapCenter.x, _mapCenter.y, cameraOffset.z);
            Vector3 clampedPos = GetClampedPosition(targetPos);

            Sequence resetSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            resetSequence.Join(DOTween.To(() => _cameraBasePosition, SetCameraBasePosition, clampedPos, duration)
                .SetEase(Ease.InOutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            resetSequence.Join(mainCam.DOOrthoSize(_initialOrthographicSize, duration)
                .SetEase(Ease.InOutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            resetSequence.OnUpdate(() =>
            {
                _isBoundsDirty = true;
                SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
            });
            resetSequence.OnComplete(() =>
            {
                _isResettingView = false;
                _isBoundsDirty = true;
                SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
                _resetViewTween = null;
                onComplete?.Invoke();
            });
            resetSequence.OnKill(() =>
            {
                if (_resetViewTween == resetSequence) _resetViewTween = null;
            });
            _resetViewTween = resetSequence;
        }

        #region Panning & Inertia Logic

        public void StartPan(Vector2 screenPos)
        {
            KillTween(ref _panTween);
            KillTween(ref _resetViewTween);
            _isResettingView = false;
            _isDragging = true;
            _panVelocity = Vector3.zero;
            _dragWorldOrigin = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cameraOffset.z));
            _lastWorldPos = _dragWorldOrigin;
        }

        public void ProcessPan(Vector2 currentScreenPos)
        {
            Vector3 currentWorldPos =
                mainCam.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, -cameraOffset.z));
            Vector3 difference = _dragWorldOrigin - currentWorldPos;

            SetCameraBasePosition(GetClampedPosition(_cameraBasePosition + difference));

            if (Time.deltaTime > 0.001f)
            {
                _panVelocity = difference / Time.deltaTime;
            }

            _lastWorldPos = currentWorldPos;
        }

        public void EndPan()
        {
            _isDragging = false;
        }

        private void HandleInertia()
        {
            if (_isDragging) return;

            if (_panVelocity.magnitude > 0.01f)
            {
                SetCameraBasePosition(GetClampedPosition(_cameraBasePosition + _panVelocity * Time.deltaTime));
                _panVelocity *= friction;
            }
            else
            {
                _panVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Zoom & Clamping Logic

        private void HandleZoomInput(ZoomInputData zoomData)
        {
            if (_isIntroZooming || _isResettingView) return;

            if (zoomData.Source == ZoomInputSource.Pinch) return;

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                if (!TutorialManager.Instance.IsCameraStep())
                {
                    return;
                }
            }

            float zoomDelta = zoomData.Source == ZoomInputSource.Pinch
                ? zoomData.Delta * pinchZoomSensitivity
                : zoomData.Delta;

            float newTargetSize = CameraZoomUtility.CalculateOrthographicSize(
                _targetOrthographicSize,
                zoomDelta,
                mouseWheelZoomStepPercent,
                _dynamicMinZoom,
                _dynamicMaxZoom);

            if (Mathf.Approximately(newTargetSize, _targetOrthographicSize)) return;

            _targetOrthographicSize = newTargetSize;
            _lastZoomScreenPosition = zoomData.ScreenPosition;
            _hasPendingPointerAnchor = zoomData.UsePointerAnchor && usePointerAnchoredWheelZoom;
        }

        private void HandleSmoothZoom()
        {
            if (Mathf.Abs(mainCam.orthographicSize - _targetOrthographicSize) > 0.01f)
            {
                float currentSize = mainCam.orthographicSize;
                float nextSize = Mathf.Lerp(currentSize, _targetOrthographicSize, Time.deltaTime * zoomSmoothness);

                if (_hasPendingPointerAnchor)
                {
                    Vector3 anchoredPosition = CameraZoomUtility.CalculatePointerAnchoredPosition(
                        _cameraBasePosition,
                        currentSize,
                        nextSize,
                        mainCam.aspect,
                        _lastZoomScreenPosition,
                        new Vector2(Screen.width, Screen.height));

                    SetCameraBasePosition(GetClampedPosition(anchoredPosition));
                }

                mainCam.orthographicSize = nextSize;
                _isBoundsDirty = true;

                if (Mathf.Abs(mainCam.orthographicSize - _targetOrthographicSize) <= 0.01f)
                {
                    _hasPendingPointerAnchor = false;
                }
            }
            else
            {
                _hasPendingPointerAnchor = false;
            }

            if (_isBoundsDirty || !_isDragging)
            {
                SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
            }
        }

        private Vector3 GetClampedPosition(Vector3 targetPos)
        {
            if (_isBoundsDirty)
            {
                _camHalfHeight = mainCam.orthographicSize;
                _camHalfWidth = _camHalfHeight * mainCam.aspect;
                _isBoundsDirty = false;
            }

            float minX = _mapCenter.x - (_mapSize.x / 2f) - mapPadding + _camHalfWidth;
            float maxX = _mapCenter.x + (_mapSize.x / 2f) + mapPadding - _camHalfWidth;
            float minY = _mapCenter.y - (_mapSize.y / 2f) - mapPadding + _camHalfHeight;
            float maxY = _mapCenter.y + (_mapSize.y / 2f) + mapPadding - _camHalfHeight;

            Vector3 clampedPos = targetPos;
            clampedPos.x = (minX > maxX) ? _mapCenter.x : Mathf.Clamp(targetPos.x, minX, maxX);
            clampedPos.y = (minY > maxY) ? _mapCenter.y : Mathf.Clamp(targetPos.y, minY, maxY);
            clampedPos.z = cameraOffset.z;

            return clampedPos;
        }

        private void HandlePinchZoom()
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                if (!TutorialManager.Instance.IsCameraStep())
                {
                    _isPinching = false;
                    return;
                }
            }

            var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (activeTouches.Count < 2)
            {
                _isPinching = false;
                return;
            }

            var touch0 = activeTouches[0];
            var touch1 = activeTouches[1];

            Vector2 touch0Pos = touch0.screenPosition;
            Vector2 touch1Pos = touch1.screenPosition;

            float currentDistance = Vector2.Distance(touch0Pos, touch1Pos);
            Vector2 currentScreenCenter = (touch0Pos + touch1Pos) * 0.5f;

            if (!_isPinching)
            {
                _isPinching = true;
                _initialPinchDistance = currentDistance;
                _initialOrthoSize = mainCam.orthographicSize;
                _initialPinchScreenCenter = currentScreenCenter;

                KillTween(ref _panTween);
                KillTween(ref _zoomTween);
                KillTween(ref _resetViewTween);
                _isDragging = false;
                _panVelocity = Vector3.zero;

                _initialPinchWorldCenter = mainCam.ScreenToWorldPoint(new Vector3(_initialPinchScreenCenter.x, _initialPinchScreenCenter.y, -cameraOffset.z));
            }

            if (_initialPinchDistance > 10f)
            {
                float zoomFactor = _initialPinchDistance / currentDistance;
                float targetOrthoSize = _initialOrthoSize * zoomFactor;

                _targetOrthographicSize = Mathf.Clamp(targetOrthoSize, _dynamicMinZoom, _dynamicMaxZoom);

                float nextSize = Mathf.Lerp(mainCam.orthographicSize, _targetOrthographicSize, Time.deltaTime * pinchZoomSmoothness);
                mainCam.orthographicSize = nextSize;
                _isBoundsDirty = true;

                Vector2 viewport = new Vector2(currentScreenCenter.x / Screen.width, currentScreenCenter.y / Screen.height);
                Vector2 centeredViewport = viewport - new Vector2(0.5f, 0.5f);

                float halfHeight = nextSize;
                float halfWidth = nextSize * mainCam.aspect;

                Vector3 targetCamPos = new Vector3(
                    _initialPinchWorldCenter.x - (centeredViewport.x * 2f * halfWidth),
                    _initialPinchWorldCenter.y - (centeredViewport.y * 2f * halfHeight),
                    cameraOffset.z
                );

                Vector3 clampedCamPos = GetClampedPosition(targetCamPos);
                Vector3 nextCamPos = Vector3.Lerp(_cameraBasePosition, clampedCamPos, Time.deltaTime * pinchZoomSmoothness);
                SetCameraBasePosition(nextCamPos);
            }
        }

        #endregion

        public void PlayIntroZoomAnimation()
        {
            _isIntroZooming = true;
            _isResettingView = false;
            _hasPendingPointerAnchor = false;
            KillTween(ref _zoomTween);
            KillTween(ref _resetViewTween);
            KillTween(ref _shakeTween);
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            float startSize = _initialOrthographicSize + introZoomOffset;
            
            // Tính toán độ zoom sâu dựa trên số lượng arrow
            float zoomInFactor = introZoomInFactor;
            if (_levelArrowCount >= arrowCountThreshold)
            {
                zoomInFactor *= deepZoomMultiplierForLargeLevels;
            }
            float deepZoomInSize = _initialOrthographicSize * zoomInFactor;
            float finalFitSize = _initialOrthographicSize;

            mainCam.orthographicSize = startSize;
            _targetOrthographicSize = finalFitSize;

            if (useIntroZoomCurve && introZoomCurve != null)
            {
                Tween zoomTween = mainCam.DOOrthoSize(finalFitSize, introZoomDuration)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .SetEase(introZoomCurve)
                    .OnUpdate(() =>
                    {
                        _isBoundsDirty = true;
                        SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
                    })
                    .OnComplete(() =>
                    {
                        _isIntroZooming = false;
                        _zoomTween = null;
                    });
                zoomTween.OnKill(() =>
                {
                    if (_zoomTween == zoomTween) _zoomTween = null;
                });
                _zoomTween = zoomTween;
            }
            else
            {
                Sequence introSeq = DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                // Phase 1: Zoom strongly in (lao nhanh vào sâu)
                introSeq.Append(mainCam.DOOrthoSize(deepZoomInSize, introZoomInDuration)
                    .SetEase(introZoomInEase));

                // Phase 2: Giật ra rồi zoom vào lại (sử dụng Ease.OutBack từ deepZoomInSize -> finalFitSize)
                introSeq.Append(mainCam.DOOrthoSize(finalFitSize, introZoomOutDuration)
                    .SetEase(introZoomOutEase));

                introSeq.OnUpdate(() =>
                {
                    _isBoundsDirty = true;
                    SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
                });

                introSeq.OnComplete(() =>
                {
                    _isIntroZooming = false;
                    _zoomTween = null;
                });

                introSeq.OnKill(() =>
                {
                    if (_zoomTween == introSeq) _zoomTween = null;
                });

                _zoomTween = introSeq;
            }
        }

        public void FocusOn(Vector3 worldPos, float duration = 0.5f)
        {
            if (_isIntroZooming || _isResettingView) return;

            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, cameraOffset.z);
            Vector3 clampedPos = GetClampedPosition(targetPos);
            KillTween(ref _panTween);

            Tween panTween = DOTween.To(() => _cameraBasePosition, SetCameraBasePosition, clampedPos, duration)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .SetEase(Ease.InOutCubic);
            panTween.OnKill(() =>
            {
                if (_panTween == panTween) _panTween = null;
            });
            _panTween = panTween;
        }

        public void FocusAndZoomOn(Vector3 worldPos, float targetZoom, float duration = 0.5f)
        {
            if (_isIntroZooming || _isResettingView) return;

            _targetOrthographicSize = Mathf.Clamp(targetZoom, _dynamicMinZoom, _dynamicMaxZoom);

            KillTween(ref _zoomTween);
            _zoomTween = mainCam.DOOrthoSize(_targetOrthographicSize, duration)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .SetEase(Ease.InOutCubic)
                .OnUpdate(() =>
                {
                    _isBoundsDirty = true;
                    SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
                });
            _zoomTween.OnComplete(() =>
            {
                _zoomTween = null;
            });
            _zoomTween.OnKill(() =>
            {
                if (_zoomTween == _zoomTween) _zoomTween = null;
            });

            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, cameraOffset.z);
            Vector3 clampedPos = GetClampedPosition(targetPos);
            KillTween(ref _panTween);

            Tween panTween = DOTween.To(() => _cameraBasePosition, SetCameraBasePosition, clampedPos, duration)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .SetEase(Ease.InOutCubic);
            panTween.OnKill(() =>
            {
                if (_panTween == panTween) _panTween = null;
            });
            _panTween = panTween;
        }

        public void PlayTapMicroShake()
        {
            if (_isIntroZooming || _isResettingView) return;

            float microStrength = wrongImpactShakeStrength * 0.25f;
            float microDuration = wrongImpactShakeDuration * 0.5f;
            if (microStrength <= 0f || microDuration <= 0f) return;

            if (_shakeTween != null && _shakeTween.IsActive()) return;

            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            int steps = 2;
            float stepDuration = microDuration / (steps + 1);

            Sequence microShake = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            for (int i = 0; i < steps; i++)
            {
                float strengthFactor = 1f - ((float)i / steps);
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * (microStrength * strengthFactor);
                Vector3 targetOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
                microShake.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, targetOffset, stepDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            }

            microShake.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, Vector3.zero, stepDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            microShake.OnComplete(() =>
            {
                _shakeOffset = Vector3.zero;
                ApplyCameraTransform();
            });
        }

        public void PlayWrongImpactShake()
        {
            if (_isIntroZooming || _isResettingView || wrongImpactShakeDuration <= 0f || wrongImpactShakeStrength <= 0f)
            {
                return;
            }

            KillTween(ref _shakeTween);
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            int steps = Mathf.Max(2, wrongImpactShakeVibrato);
            float stepDuration = wrongImpactShakeDuration / (steps + 1);

            Sequence shakeSequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            for (int i = 0; i < steps; i++)
            {
                float strengthFactor = 1f - ((float)i / steps);
                Vector2 randomOffset =
                    UnityEngine.Random.insideUnitCircle * (wrongImpactShakeStrength * strengthFactor);
                Vector3 targetOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);

                shakeSequence.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, targetOffset, stepDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            }

            shakeSequence.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, Vector3.zero, stepDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable));
            shakeSequence.OnComplete(() =>
            {
                _shakeOffset = Vector3.zero;
                ApplyCameraTransform();
                _shakeTween = null;
            });
            shakeSequence.OnKill(() =>
            {
                if (_shakeTween == shakeSequence) _shakeTween = null;
            });
            _shakeTween = shakeSequence;
        }

        private void KillAllTweens()
        {
            KillTween(ref _panTween);
            KillTween(ref _zoomTween);
            KillTween(ref _resetViewTween);
            KillTween(ref _shakeTween);
        }

        private void KillTween<T>(ref T tween) where T : Tween
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
        }

        private void SetCameraBasePosition(Vector3 position)
        {
            _cameraBasePosition = position;
            ApplyCameraTransform();
        }

        private void SetShakeOffset(Vector3 offset)
        {
            _shakeOffset = offset;
            ApplyCameraTransform();
        }

        private void ApplyCameraTransform()
        {
            _camTransform.position = _cameraBasePosition + _shakeOffset;
            EventManager<VisualEventID>.Post(VisualEventID.CameraMoved);
        }
    }
}
