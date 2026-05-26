using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArrowGame.UI.Base;

namespace ArrowGame.UI.Controllers
{
    [Serializable]
    public class NavTab
    {
        public MainTabID tabID;
        public Button btn;
        public RectTransform rectTransform; 
        
        [Header("Juice Target")]
        [Tooltip("Kéo trực tiếp Object ICON vào đây để chỉ scale icon")]
        public RectTransform iconTarget; 

        [HideInInspector] public Sequence animSeq; 
    }

    [RequireComponent(typeof(RectTransform))]
    public class BottomBarController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform sliderBackground;
        
        [Header("--- Tabs Setup ---")]
        [SerializeField] private NavTab[] tabs;
        [SerializeField] private float animDuration = 0.25f;
        
        [Header("--- Magnify Effect (Kính lúp) ---")]
        [SerializeField] private float maxScale = 1.3f;
        [SerializeField] private float minScale = 1.0f;
        [SerializeField] private float baseEffectRadius = 100f;

        [Header("--- Slider Juice ---")]
        [SerializeField] private float baseSwipeThreshold = 80f; 
        [SerializeField] private float stretchFactor = 0.007f; 
        [SerializeField] private float maxStretch = 0.15f; 

        public event Action<MainTabID> OnTabClicked;

        private MainTabID _currentTab = (MainTabID)(-1);
        private RectTransform _containerRect;
        private Canvas _rootCanvas;
        
        private float _scaledSwipeThreshold;
        private float _scaledEffectRadius;
        private float _startDragX;
        private int _currentDragPointerId = -999; 
        
        private Tweener _sliderPosTween;
        private Tweener _sliderScaleTween; 

        public void Init()
        {
            Canvas.ForceUpdateCanvases();
            _containerRect = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<Canvas>();

            float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            _scaledSwipeThreshold = baseSwipeThreshold * scaleFactor;
            _scaledEffectRadius = baseEffectRadius * scaleFactor;

            foreach (var tab in tabs)
            {
                MainTabID id = tab.tabID;
                tab.btn.onClick.AddListener(() => {
                    if (_currentDragPointerId == -999) ChangeTab(id);
                });
            }
        }

        private float GetTabTargetX(NavTab tab)
        {
            // Lấy tọa độ của iconTarget để slider bám chuẩn tâm icon
            return _containerRect.InverseTransformPoint(tab.iconTarget.position).x;
        }

        public void ChangeTab(MainTabID tabID, bool instant = false, bool forceSnap = false)
        {
            if (_currentTab == tabID && !instant && !forceSnap) return;
            bool isNew = _currentTab != tabID;
            _currentTab = tabID;

            float duration = instant ? 0f : animDuration;
            _sliderPosTween?.Kill();
            
            NavTab targetTab = Array.Find(tabs, t => t.tabID == tabID);
            if (targetTab == null) return;

            float targetX = GetTabTargetX(targetTab);

            if (instant)
            {
                sliderBackground.anchoredPosition = new Vector2(targetX, sliderBackground.anchoredPosition.y);
            }
            else
            {
                _sliderPosTween = sliderBackground.DOAnchorPosX(targetX, duration)
                    .SetEase(Ease.OutCubic)
                    .SetLink(sliderBackground.gameObject);
            }
            
            foreach (var tab in tabs)
            {
                bool isSelected = (tab.tabID == tabID);
                tab.animSeq?.Kill();
                tab.animSeq = DOTween.Sequence().SetLink(tab.iconTarget.gameObject); 

                float s = isSelected ? maxScale : minScale;

                // Chỉ tác động Scale lên iconTarget
                tab.animSeq.Join(tab.iconTarget.DOScale(s, duration).SetEase(isSelected ? Ease.OutBack : Ease.OutQuad));
                
                if (instant) tab.animSeq.Complete();
            }

            if (isNew)
            {
                OnTabClicked?.Invoke(tabID);
                if (!instant) {
                    // uddojwc
                }
            }
        }

        #region Drag Logic

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentDragPointerId != -999) return;
            _currentDragPointerId = eventData.pointerId;

            _sliderPosTween?.Kill(); 
            _sliderScaleTween?.Kill(); 
            _sliderScaleTween = sliderBackground.DOScale(new Vector3(1.1f, 0.9f, 1f), 0.15f).SetEase(Ease.OutQuad);

            foreach (var tab in tabs) tab.animSeq?.Kill();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 startPt);
            _startDragX = startPt.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _currentDragPointerId) return; 

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 localPt))
            {
                float minX = GetTabTargetX(tabs[0]);
                float maxX = GetTabTargetX(tabs[tabs.Length - 1]);
                float clampedX = Mathf.Clamp(localPt.x, minX, maxX);
                
                sliderBackground.anchoredPosition = new Vector2(clampedX, sliderBackground.anchoredPosition.y);
                
                float dragSpeed = Mathf.Abs(eventData.delta.x);
                float stretchX = 1f + Mathf.Clamp(dragSpeed * stretchFactor, 0f, maxStretch);
                float stretchY = 1f - Mathf.Clamp(dragSpeed * (stretchFactor * 0.5f), 0f, maxStretch * 0.5f);
                sliderBackground.localScale = new Vector3(stretchX, stretchY, 1f);

                // Magnifying Effect: Chỉ tác động lên LocalScale của iconTarget
                foreach (var tab in tabs)
                {
                    float dist = Mathf.Abs(clampedX - GetTabTargetX(tab));
                    float lensFactor = 1f - Mathf.Clamp01(dist / _scaledEffectRadius);
                    float curScale = Mathf.Lerp(minScale, maxScale, lensFactor);
                    
                    tab.iconTarget.localScale = new Vector3(curScale, curScale, 1f);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _currentDragPointerId) return; 
            _currentDragPointerId = -999; 
            
            _sliderScaleTween?.Kill();
            _sliderScaleTween = sliderBackground.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutElastic);
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position, eventData.pressEventCamera, out Vector2 endPt);
            float deltaX = endPt.x - _startDragX;

            MainTabID targetID = _currentTab;
            if (Mathf.Abs(deltaX) > _scaledSwipeThreshold)
            {
                int curIdx = Array.FindIndex(tabs, t => t.tabID == _currentTab);
                int dir = deltaX > 0 ? 1 : -1; 
                targetID = tabs[Mathf.Clamp(curIdx + dir, 0, tabs.Length - 1)].tabID;
            }
            else
            {
                float minD = float.MaxValue;
                foreach (var t in tabs)
                {
                    float d = Mathf.Abs(sliderBackground.anchoredPosition.x - GetTabTargetX(t));
                    if (d < minD) { minD = d; targetID = t.tabID; }
                }
            }

            if (targetID == _currentTab) {
                // uddojwc
            }
            
            ChangeTab(targetID, false, true);
        }

        #endregion

        private void OnDestroy()
        {
            _sliderPosTween?.Kill();
            _sliderScaleTween?.Kill();
            foreach (var tab in tabs) tab.animSeq?.Kill();
        }
    }
}
