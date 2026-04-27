using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using DG.Tweening;
using GameCore.Input;
using System;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private Camera mainCam;

        [Header("Dynamic Zoom Settings")]
        [Tooltip("Tỉ lệ zoom sâu nhất so với toàn bộ map (0.3 = chỉ nhìn thấy 30% map)")]
        [SerializeField, Range(0.1f, 0.8f)] private float minZoomRatio = 0.3f; 
        [Tooltip("Giới hạn cứng để không bị zoom vào quá sát một pixel trên map siêu nhỏ")]
        [SerializeField] private float absoluteMinZoom = 3f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float zoomSmoothness = 10f;

        [Header("Pan & Inertia Settings")]
        [SerializeField] private float mapPadding = 3f;
        [SerializeField, Range(0f, 1f)] private float friction = 0.92f; 
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10f);
        
        [Header("Intro Animation Settings")]
        [SerializeField] private float introZoomOffset = 8f;   
        [SerializeField] private float introZoomDuration = 1.5f; 

        [Header("Reset View Settings")]
        [SerializeField] private float resetViewDuration = 0.35f;

        [Header("Shake Settings")]
        [SerializeField] private float wrongImpactShakeDuration = 0.18f;
        [SerializeField] private float wrongImpactShakeStrength = 0.12f;
        [SerializeField] private int wrongImpactShakeVibrato = 5;
        
        private Transform _camTransform;
        private Vector2 _mapSize;
        private Vector2 _mapCenter;
        private Vector3 _cameraBasePosition;
        private Vector3 _shakeOffset;
        
        private float _targetOrthographicSize;
        private float _initialOrthographicSize;
        
        // --- CÁC BIẾN ZOOM ĐỘNG ---
        private float _dynamicMinZoom;
        private float _dynamicMaxZoom;
        // --------------------------

        private float _camHalfHeight;
        private float _camHalfWidth;
        private bool _isBoundsDirty = true;

        private Vector3 _dragWorldOrigin;
        private Vector3 _lastWorldPos;
        private Vector3 _panVelocity;
        private bool _isDragging;

        private bool _isIntroZooming = false; 
        private bool _isResettingView = false;

        private void Awake()
        {
            if (mainCam == null) mainCam = Camera.main;
            _camTransform = mainCam.transform;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput += HandleZoomInput;
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<VisualEventID>.AddListener(VisualEventID.ArrowWrongImpact, HandleArrowWrongImpact);
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput -= HandleZoomInput;
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.ArrowWrongImpact, HandleArrowWrongImpact);
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            if (state == InGameState.Intro)
            {
                PlayIntroZoomAnimation();
            }
        }

        private void HandleArrowWrongImpact()
        {
            if (_isIntroZooming || _isResettingView) return;
            PlayWrongImpactShake();
        }

        private void LateUpdate()
        {
            if (_isIntroZooming || _isResettingView) return;

            HandleSmoothZoom();
            HandleInertia();
        }

        public void InitializeCamera(int gridWidth, int gridHeight, float cellSize)
        {
            float boardWidth = (gridWidth - 1) * cellSize;
            float boardHeight = (gridHeight - 1) * cellSize;

            _mapSize = new Vector2(boardWidth, boardHeight);
            _mapCenter = Vector2.zero; 

            float startPadding = 1.0f; 
            float sizeY = (boardHeight / 2f) + startPadding;
            float sizeX = ((boardWidth / 2f) + startPadding) / mainCam.aspect; 

            float sizeNeededToFitMap = Mathf.Max(sizeX, sizeY);

            _dynamicMaxZoom = Mathf.Max(sizeNeededToFitMap, absoluteMinZoom); 

            _dynamicMinZoom = Mathf.Max(absoluteMinZoom, _dynamicMaxZoom * minZoomRatio);

            _initialOrthographicSize = _dynamicMaxZoom;
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

            DOTween.Kill("CameraPan");
            DOTween.Kill("CameraZoom");
            DOTween.Kill("CameraResetView");
            DOTween.Kill("CameraShake");
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            _targetOrthographicSize = _initialOrthographicSize;

            Vector3 targetPos = new Vector3(_mapCenter.x, _mapCenter.y, cameraOffset.z);
            Vector3 clampedPos = GetClampedPosition(targetPos);

            Sequence resetSequence = DOTween.Sequence()
                .SetId("CameraResetView");

            resetSequence.Join(DOTween.To(() => _cameraBasePosition, SetCameraBasePosition, clampedPos, duration)
                .SetEase(Ease.InOutCubic));
            resetSequence.Join(mainCam.DOOrthoSize(_initialOrthographicSize, duration).SetEase(Ease.InOutCubic));
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
                onComplete?.Invoke();
            });
        }

        #region Panning & Inertia Logic

        public void StartPan(Vector2 screenPos)
        {
            DOTween.Kill("CameraPan"); 
            DOTween.Kill("CameraResetView");
            _isResettingView = false;
            _isDragging = true;
            _panVelocity = Vector3.zero;
            _dragWorldOrigin = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cameraOffset.z));
            _lastWorldPos = _dragWorldOrigin;
        }

        public void ProcessPan(Vector2 currentScreenPos)
        {
            Vector3 currentWorldPos = mainCam.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, -cameraOffset.z));
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

        private void HandleZoomInput(float zoomDelta)
        {
            _targetOrthographicSize += zoomDelta * zoomSpeed; 
            _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _dynamicMinZoom, _dynamicMaxZoom);
        }

        private void HandleSmoothZoom()
        {
            if (Mathf.Abs(mainCam.orthographicSize - _targetOrthographicSize) > 0.01f)
            {
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, _targetOrthographicSize, Time.deltaTime * zoomSmoothness);
                _isBoundsDirty = true; 
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

        #endregion
        
        public void PlayIntroZoomAnimation()
        {
            _isIntroZooming = true;
            _isResettingView = false;
            DOTween.Kill("CameraZoom");
            DOTween.Kill("CameraResetView");
            DOTween.Kill("CameraShake");
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();
           
            mainCam.orthographicSize = _initialOrthographicSize + introZoomOffset;
            _targetOrthographicSize = _initialOrthographicSize;

            mainCam.DOOrthoSize(_initialOrthographicSize, introZoomDuration)
                .SetId("CameraZoom")
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => 
                {
                    _isBoundsDirty = true; 
                    SetCameraBasePosition(GetClampedPosition(_cameraBasePosition));
                })
                .OnComplete(() => 
                {
                    _isIntroZooming = false; 
                });
        }
        
        public void FocusOn(Vector3 worldPos, float duration = 0.5f)
        {
            if (_isIntroZooming || _isResettingView) return;

            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, cameraOffset.z);
            Vector3 clampedPos = GetClampedPosition(targetPos);
            DOTween.Kill("CameraPan");

            DOTween.To(() => _cameraBasePosition, SetCameraBasePosition, clampedPos, duration)
                .SetId("CameraPan")
                .SetEase(Ease.InOutCubic);
        }

        public void PlayWrongImpactShake()
        {
            if (_isIntroZooming || _isResettingView || wrongImpactShakeDuration <= 0f || wrongImpactShakeStrength <= 0f)
            {
                return;
            }

            DOTween.Kill("CameraShake");
            _shakeOffset = Vector3.zero;
            ApplyCameraTransform();

            int steps = Mathf.Max(2, wrongImpactShakeVibrato);
            float stepDuration = wrongImpactShakeDuration / (steps + 1);

            Sequence shakeSequence = DOTween.Sequence().SetId("CameraShake");

            for (int i = 0; i < steps; i++)
            {
                float strengthFactor = 1f - ((float)i / steps);
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * (wrongImpactShakeStrength * strengthFactor);
                Vector3 targetOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);

                shakeSequence.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, targetOffset, stepDuration)
                    .SetEase(Ease.OutQuad));
            }

            shakeSequence.Append(DOTween.To(() => _shakeOffset, SetShakeOffset, Vector3.zero, stepDuration)
                .SetEase(Ease.OutQuad));
            shakeSequence.OnComplete(() =>
            {
                _shakeOffset = Vector3.zero;
                ApplyCameraTransform();
            });
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
        }
    }
}
