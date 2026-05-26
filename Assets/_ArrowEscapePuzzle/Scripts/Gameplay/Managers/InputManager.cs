using System;
using System.Collections.Generic;
using GameCore.Utils.DesignPattern.Singleton;
using GameCore.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ArrowGame.Gameplay.Managers
{
    /// <summary>
    /// Input System dành riêng cho ArrowGame.
    /// Tách biệt khỏi GameCore để dễ dàng tùy chỉnh theo logic game mà không ảnh hưởng shared core.
    /// </summary>
    public class InputManager : Singleton<InputManager>, IAppService
    {   
        public void Init()
        {
            Debug.Log("[ArrowGame.InputManager] Initialized.");
        }

        #region EVENTS
        public event Action<Vector2> OnTouchMove;
        public event Action<Vector2> OnTouchEnd;
        public event Action<Vector2> OnTouchStart;
        public event Action<Vector2> OnAnyTouchStart;
        public event Action<Vector2> OnDiscreteTap;
        public event Action<ZoomInputData> OnZoomInput;
        #endregion

        #region PRIVATE FIELDS
        private GameInput _inputActions;
        private Camera _mainCamera;
        private bool _isDragging;
        private Vector2 _tapStartPosition;
        private float _tapStartTime;
        private const float TapDistanceThreshold = 30f;
        private const float TapTimeThreshold = 0.4f;
        private const float MouseWheelStepNormalizer = 120f;

        [Header("Zoom Settings")]
        [FormerlySerializedAs("zoomSpeed")]
        [SerializeField] private float mouseWheelZoomStep = 3f;
        [SerializeField] private float pinchPixelsPerZoomStep = 80f;
        [SerializeField] private bool blockZoomWhenPointerOverUI = true;

        // Cache để tránh GC Allocation mỗi lần Raycast UI
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(10);
        private PointerEventData _pointerEventData;
        #endregion

        #region PROPERTIES
        private Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }
                return _mainCamera;
            }
        }
        #endregion

        #region LIFECYCLE
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return; 
            
            KeepAlive(true);
            _inputActions = new GameInput();
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Enable();
            
            // Đăng ký Event
            _inputActions.Touch.TouchContact.started += OnTouchPress;
            _inputActions.Touch.TouchContact.canceled += OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed += OnTouchPosition;
            _inputActions.Touch.Scroll.performed += OnMouseScroll;

            // Bật EnhancedTouch cho Multi-touch
            EnhancedTouchSupport.Enable();
            ETouch.onFingerMove += OnFingerMove;
        }

        private void OnDisable()
        {
            if (_inputActions == null) return;

            // Hủy đăng ký Event để tránh Memory Leak
            _inputActions.Touch.TouchContact.started -= OnTouchPress;
            _inputActions.Touch.TouchContact.canceled -= OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed -= OnTouchPosition;
            _inputActions.Touch.Scroll.performed -= OnMouseScroll;
            _inputActions.Disable();

            ETouch.onFingerMove -= OnFingerMove;
            EnhancedTouchSupport.Disable();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
        }
        #endregion

        #region ZOOM & MULTI-TOUCH
        private void OnFingerMove(Finger finger)
        {
            if (ETouch.activeTouches.Count != 2) return;

            var touch0 = ETouch.activeTouches[0];
            var touch1 = ETouch.activeTouches[1];

            if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 touch0Pos = touch0.screenPosition;
                Vector2 touch1Pos = touch1.screenPosition;

                if (!IsValidScreenPosition(touch0Pos) || !IsValidScreenPosition(touch1Pos)) return;

                float prevMagnitude = ((touch0Pos - touch0.delta) - (touch1Pos - touch1.delta)).magnitude;
                float currentMagnitude = (touch0Pos - touch1Pos).magnitude;

                float difference = prevMagnitude - currentMagnitude;
                float pinchZoomDelta = difference / Mathf.Max(1f, pinchPixelsPerZoomStep);
                RaiseZoomInput(pinchZoomDelta, (touch0Pos + touch1Pos) * 0.5f, ZoomInputSource.Pinch, false);
            }
        }

        private void OnMouseScroll(InputAction.CallbackContext ctx)
        {
            Vector2 scrollValue = ctx.ReadValue<Vector2>();
            if (Mathf.Abs(scrollValue.y) <= 0.1f) return;

            if (GetTouchCount() >= 2) return;

            Vector2 pointerPosition = ReadTouchPosition();
            if (!IsValidScreenPosition(pointerPosition)) return;

            if (blockZoomWhenPointerOverUI && IsPointerOverUI(pointerPosition)) return;

            float wheelZoomDelta = -(scrollValue.y / MouseWheelStepNormalizer) * mouseWheelZoomStep;
            if (Mathf.Abs(wheelZoomDelta) > Mathf.Epsilon)
            {
                RaiseZoomInput(wheelZoomDelta, pointerPosition, ZoomInputSource.MouseWheel, true);
            }
        }
        #endregion

        #region TOUCH LOGIC & EVENT PUBLISHING
        private void OnTouchPress(InputAction.CallbackContext ctx)
        {
            Vector2 touchPos = ReadTouchPosition();
            
            if (!IsValidScreenPosition(touchPos)) return;
            
            OnAnyTouchStart?.Invoke(touchPos);
            _tapStartPosition = touchPos;
            _tapStartTime = Time.time;

            if (IsPointerOverUI(touchPos)) return;
            
            _isDragging = true;
            OnTouchStart?.Invoke(touchPos);
        }

        private void OnTouchCancel(InputAction.CallbackContext ctx)
        {
            Vector2 touchPos = ReadTouchPosition();
            _isDragging = false;
            OnTouchEnd?.Invoke(touchPos);

            float dist = Vector2.Distance(_tapStartPosition, touchPos);
            float duration = Time.time - _tapStartTime;
            if (dist < TapDistanceThreshold && duration < TapTimeThreshold)
            {
                OnDiscreteTap?.Invoke(touchPos);
            }
        }

        private void OnTouchPosition(InputAction.CallbackContext ctx)
        {
            if (!_isDragging) return;

            Vector2 touchPos = ctx.ReadValue<Vector2>();
            if (IsValidScreenPosition(touchPos))
            {
                OnTouchMove?.Invoke(touchPos);
            }
        }
        #endregion

        #region UTILITIES & VALIDATIONS
        
        public Vector2 ReadTouchPosition()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
            return _inputActions != null ? _inputActions.Touch.TouchPosition.ReadValue<Vector2>() : Vector2.zero;
        }

        public Vector3 GetWorldPosition()
        {
            if (MainCamera == null) return Vector3.zero;

            Vector2 screenPos = ReadTouchPosition();
            
            if (!IsValidScreenPosition(screenPos)) return Vector3.zero;
            
            float distanceToPlane = Mathf.Abs(MainCamera.transform.position.z);
            Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, distanceToPlane);
            
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPosWithZ);
            worldPos.z = 0; 
            
            return worldPos;
        }

        public int GetTouchCount()
        {
            return ETouch.activeTouches.Count;
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(EventSystem.current);
            }

            _pointerEventData.position = screenPosition;
            _raycastResults.Clear();

            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }

        private bool IsValidScreenPosition(Vector2 position)
        {
            return position.x >= 0 && position.x <= Screen.width && 
                   position.y >= 0 && position.y <= Screen.height;
        }

        private void RaiseZoomInput(float delta, Vector2 screenPosition, ZoomInputSource source, bool usePointerAnchor)
        {
            if (Mathf.Abs(delta) <= Mathf.Epsilon) return;

            OnZoomInput?.Invoke(new ZoomInputData(delta, screenPosition, source, usePointerAnchor));
        }
        #endregion
    }
}
