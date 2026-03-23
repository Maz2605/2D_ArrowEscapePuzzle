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
    public class InputManager : Singleton<InputManager>
    {   
        public event Action<Vector2> OnTouchMove;
        public event Action<Vector2> OnTouchEnd;
        public event Action<Vector2> OnTouchStart;
        public event Action<float> OnZoomInput; 

        private GameInput _inputActions;
        private Camera _mainCamera;
        private bool _isDragging;

        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private PointerEventData _pointerEventData;

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

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this) 
            {
                return; 
            }
            
            KeepAlive(true);
            _inputActions = new GameInput();
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Enable();
            _inputActions.Touch.TouchContact.started += OnTouchPress;
            _inputActions.Touch.TouchContact.canceled += OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed += OnTouchPosition;

            _inputActions.Touch.Scroll.performed += OnMouseScroll;

            EnhancedTouchSupport.Enable();
            ETouch.onFingerMove += OnFingerMove;
        }

        private void OnDisable()
        {
            if (_inputActions == null) return;

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

        #region ZOOM CAMERA 

        private void OnFingerMove(Finger finger)
        {
            if (ETouch.activeTouches.Count == 2)
            {
                var touch0 = ETouch.activeTouches[0];
                var touch1 = ETouch.activeTouches[1];

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                    touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 touch0Pos = touch0.screenPosition;
                    Vector2 touch1Pos = touch1.screenPosition;

                    Vector2 touch0PrevPos = touch0Pos - touch0.delta;
                    Vector2 touch1PrevPos = touch1Pos - touch1.delta;

                    float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                    float currentMagnitude = (touch0Pos - touch1Pos).magnitude;

                    float difference = prevMagnitude - currentMagnitude; 
                    OnZoomInput?.Invoke(difference * 0.01f); 
                }
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

        #region TOUCH & GAMEPLAY LOGIC

        private void OnTouchPress(InputAction.CallbackContext ctx)
        {
            if (IsPointerOverUI()) return;
            
            _isDragging = true;
            OnTouchStart?.Invoke(ReadTouchPosition());
        }

        private void OnTouchCancel(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OnTouchEnd?.Invoke(ReadTouchPosition());
            }
        }

        private void OnTouchPosition(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                OnTouchMove?.Invoke(ctx.ReadValue<Vector2>());
            }
        }

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
            
            float distanceToPlane = Mathf.Abs(MainCamera.transform.position.z);
            Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, distanceToPlane);
            
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPosWithZ);
            worldPos.z = 0; 
            
            return worldPos;
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            Vector2 touchPos = ReadTouchPosition();

            _pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = touchPos
            };
            _raycastResults.Clear();

            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            return _raycastResults.Count > 0;
        }
        
        public int GetTouchCount()
        {
            // Trả về số lượng ngón tay từ EnhancedTouch
            return ETouch.activeTouches.Count;
        }

        #endregion
    }
}