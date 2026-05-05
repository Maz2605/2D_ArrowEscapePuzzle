using System;
using System.Collections.Generic;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GameCore.Input
{
    /// <summary>
    /// Core Input System cho Game. Đảm bảo Single Source of Truth cho mọi tương tác chạm/vuốt.
    /// Tối ưu GC Allocation và validate chặt chẽ Screen Bounds cho Mobile.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {   
        #region EVENTS
        public event Action<Vector2> OnTouchMove;
        public event Action<Vector2> OnTouchEnd;
        public event Action<Vector2> OnTouchStart;
        public event Action<float> OnZoomInput; 
        #endregion

        #region PRIVATE FIELDS
        private GameInput _inputActions;
        private Camera _mainCamera;
        private bool _isDragging;

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

                // Cảnh báo: Tọa độ ngón tay ngoài viền có thể gây lỗi logic tính khoảng cách
                if (!IsValidScreenPosition(touch0Pos) || !IsValidScreenPosition(touch1Pos)) return;

                float prevMagnitude = ((touch0Pos - touch0.delta) - (touch1Pos - touch1.delta)).magnitude;
                float currentMagnitude = (touch0Pos - touch1Pos).magnitude;

                float difference = prevMagnitude - currentMagnitude; 
                OnZoomInput?.Invoke(difference * 0.01f); 
            }
        }

        private void OnMouseScroll(InputAction.CallbackContext ctx)
        {
            Vector2 scrollValue = ctx.ReadValue<Vector2>();
            if (Mathf.Abs(scrollValue.y) > 0.1f)
            {
                OnZoomInput?.Invoke(-scrollValue.y * 0.05f); 
            }
        }
        #endregion

        #region TOUCH LOGIC & EVENT PUBLISHING
        private void OnTouchPress(InputAction.CallbackContext ctx)
        {
            Vector2 touchPos = ReadTouchPosition();
            
            // 1. Chặn tọa độ rác (Gây ra lỗi Out of view frustum)
            if (!IsValidScreenPosition(touchPos)) return;
            
            // 2. Chặn tương tác nếu đang bấm vào UI (Button, Panel...)
            if (IsPointerOverUI(touchPos)) return;
            
            _isDragging = true;
            OnTouchStart?.Invoke(touchPos);
        }

        private void OnTouchCancel(InputAction.CallbackContext ctx)
        {
            if (!_isDragging) return;
            
            _isDragging = false;
            // Dù nhả tay ngoài màn hình cũng cần báo Event End để reset state
            OnTouchEnd?.Invoke(ReadTouchPosition()); 
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
            
            // BẮT BUỘC CHECK: Ngăn lỗi Camera tính toán sai ma trận chiếu
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

        /// <summary>
        /// Kiểm tra xem điểm chạm có nằm trên một UI Element nào không.
        /// Đã tối ưu GC Allocation.
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            // Tái sử dụng PointerEventData thay vì dùng từ khóa 'new' mỗi lần chạm
            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(EventSystem.current);
            }

            _pointerEventData.position = screenPosition;
            _raycastResults.Clear();

            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }

        /// <summary>
        /// Rào chắn bảo vệ Core Logic khỏi dữ liệu Input rác từ phần cứng di động.
        /// </summary>
        private bool IsValidScreenPosition(Vector2 position)
        {
            return position.x >= 0 && position.x <= Screen.width && 
                   position.y >= 0 && position.y <= Screen.height;
        }
        #endregion
    }
}