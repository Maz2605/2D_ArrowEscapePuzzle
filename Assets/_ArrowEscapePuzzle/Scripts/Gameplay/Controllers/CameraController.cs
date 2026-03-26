using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using DG.Tweening;
using GameCore.Input;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private Camera mainCam;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 15f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float zoomSmoothness = 10f;

        [Header("Pan & Inertia Settings")]
        [SerializeField] private float mapPadding = 3f;
        [SerializeField, Range(0f, 1f)] private float friction = 0.92f; 
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10f);
        
        [Header("Intro Animation Settings")]
        [SerializeField] private float introZoomOffset = 8f;   
        [SerializeField] private float introZoomDuration = 1.5f; 
        
        private Transform _camTransform;
        private Vector2 _mapSize;
        private Vector2 _mapCenter;
        
        private float _targetOrthographicSize;
        private float _initialOrthographicSize;
        
        private float _camHalfHeight;
        private float _camHalfWidth;
        private bool _isBoundsDirty = true;

        private Vector3 _dragWorldOrigin;
        private Vector3 _lastWorldPos;
        private Vector3 _panVelocity;
        private bool _isDragging;

        private bool _isIntroZooming = false; 

        private void Awake()
        {
            if (mainCam == null) mainCam = Camera.main;
            _camTransform = mainCam.transform;
            _targetOrthographicSize = mainCam.orthographicSize;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput += HandleZoomInput;
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnZoomInput -= HandleZoomInput;
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.IntroLevel)
            {
                PlayIntroZoomAnimation();
            }
        }

        private void LateUpdate()
        {
            if (_isIntroZooming) return;

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

            _initialOrthographicSize = Mathf.Clamp(Mathf.Max(sizeX, sizeY), minZoom, maxZoom);
            _targetOrthographicSize = _initialOrthographicSize;
            
            _camTransform.position = new Vector3(_mapCenter.x, _mapCenter.y, cameraOffset.z);
            mainCam.orthographicSize = _targetOrthographicSize;
            
            _isBoundsDirty = true;
        }

        public void ResetZoom() => _targetOrthographicSize = _initialOrthographicSize;

        #region Panning & Inertia Logic

        public void StartPan(Vector2 screenPos)
        {
            _isDragging = true;
            _panVelocity = Vector3.zero;
            _dragWorldOrigin = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cameraOffset.z));
            _lastWorldPos = _dragWorldOrigin;
        }

        public void ProcessPan(Vector2 currentScreenPos)
        {
            Vector3 currentWorldPos = mainCam.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, -cameraOffset.z));
            Vector3 difference = _dragWorldOrigin - currentWorldPos;
            
            _camTransform.position = GetClampedPosition(_camTransform.position + difference);
            
            _panVelocity = (difference) / Time.deltaTime;
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
                _camTransform.position = GetClampedPosition(_camTransform.position + _panVelocity * Time.deltaTime);
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
            _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, minZoom, maxZoom);
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
                _camTransform.position = GetClampedPosition(_camTransform.position);
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
            DOTween.Kill("CameraZoom");

           
            mainCam.orthographicSize = _initialOrthographicSize + introZoomOffset;
            _targetOrthographicSize = _initialOrthographicSize;

            mainCam.DOOrthoSize(_initialOrthographicSize, introZoomDuration)
                .SetId("CameraZoom")
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => 
                {
                    _isBoundsDirty = true; 
                    _camTransform.position = GetClampedPosition(_camTransform.position);
                })
                .OnComplete(() => 
                {
                    _isIntroZooming = false; 
                });
        }
    }
}