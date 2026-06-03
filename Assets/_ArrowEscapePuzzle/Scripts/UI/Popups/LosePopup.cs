using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ArrowGame.UI.Popups
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LosePopup : BasePopup
    {
        [Header("--- Core UI References ---")]
        [SerializeField] private RectTransform contentPanel; 
        [SerializeField] private RectTransform heartLeft;    
        [SerializeField] private RectTransform heartRight;   
        [SerializeField] private RectTransform btnHomeRect;  
        [SerializeField] private RectTransform btnReplayRect;

        [Header("--- Level Info ---")]
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtLevelFailed;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnHome;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float popupDuration = 0.4f;
        [SerializeField] private float breakDistance = 10f;  
        [SerializeField] private float breakRotation = 5f;  

        [Header("--- Idle Animations (Premium Feel) ---")]
        [Tooltip("Độ lệch bay lên/xuống của tim vỡ")]
        [SerializeField] private float heartFloatAmount = 6f;
        [Tooltip("Thời gian của một chu kỳ bay")]
        [SerializeField] private float heartFloatDuration = 2.0f;
        [Tooltip("Góc lắc lư thêm của mảnh tim")]
        [SerializeField] private float heartRotateSwayAmount = 2f;
        [Tooltip("Thời gian của một chu kỳ lắc lư")]
        [SerializeField] private float heartRotateSwayDuration = 2.2f;
        [Tooltip("Tỷ lệ co giãn (pulsing) của nút chơi lại")]
        [SerializeField] private float ctaPulseAmount = 1.06f;
        [Tooltip("Thời gian của một chu kỳ co giãn")]
        [SerializeField] private float ctaPulseDuration = 0.8f;

        private CanvasGroup _canvasGroup;
        private Sequence _showSequence;
        private Sequence _hideSequence;
        
        private Vector2 _leftOriginPos;
        private Vector2 _rightOriginPos;
        private float _levelTextOriginY;
        private float _levelFailedTextOriginY;

        protected override void Awake()
        {
            base.Awake();
            _canvasGroup = GetComponent<CanvasGroup>();

            BindButton(btnHome, () =>
            {
                GameManager.Instance.RequestBackHome();
            });
            BindButton(btnReplay, () =>
            {
                UIManager.Instance.ShowLoading(onCovered: () =>
                {
                    UIManager.Instance.HideLoading();
                    GameManager.Instance.RequestReloadLevelWithEnergyWarning();
                });
            });

            // Lưu lại vị trí chuẩn (lúc tim đang lành lặn) trên Scene
            if (heartLeft != null) _leftOriginPos = heartLeft.anchoredPosition;
            if (heartRight != null) _rightOriginPos = heartRight.anchoredPosition;

            // Lưu vị trí gốc của các Text để chạy slide animation
            if (txtLevel != null) _levelTextOriginY = txtLevel.rectTransform.anchoredPosition.y;
            if (txtLevelFailed != null) _levelFailedTextOriginY = txtLevelFailed.rectTransform.anchoredPosition.y;

            ResetUIState();
        }

        private void ResetUIState()
        {
            if (contentPanel != null) contentPanel.localScale = Vector3.zero;
            if (btnHomeRect != null) btnHomeRect.localScale = Vector3.zero;
            if (btnReplayRect != null)
            {
                btnReplayRect.DOKill();
                btnReplayRect.localScale = Vector3.zero;
            }

            if (heartLeft != null)
            {
                heartLeft.DOKill();
                heartLeft.anchoredPosition = _leftOriginPos;
                heartLeft.localRotation = Quaternion.identity;
                heartLeft.localScale = Vector3.zero; // Bắt đầu từ 0 để pop up nguyên vẹn trước
            }
            if (heartRight != null)
            {
                heartRight.DOKill();
                heartRight.anchoredPosition = _rightOriginPos;
                heartRight.localRotation = Quaternion.identity;
                heartRight.localScale = Vector3.zero; // Bắt đầu từ 0 để pop up nguyên vẹn trước
            }

            if (txtLevel != null)
            {
                txtLevel.rectTransform.DOKill();
                txtLevel.rectTransform.localScale = Vector3.zero;
                txtLevel.rectTransform.anchoredPosition = new Vector2(txtLevel.rectTransform.anchoredPosition.x, _levelTextOriginY + 30f);
            }
            if (txtLevelFailed != null)
            {
                txtLevelFailed.rectTransform.DOKill();
                txtLevelFailed.rectTransform.localScale = Vector3.zero;
                txtLevelFailed.rectTransform.anchoredPosition = new Vector2(txtLevelFailed.rectTransform.anchoredPosition.x, _levelFailedTextOriginY + 30f);
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            if (btnHome != null) btnHome.interactable = false;
            if (btnReplay != null) btnReplay.interactable = false;
        }
        

        protected override void PlayShowAnimation()
        {
            KillAllTweens();
            ResetUIState();

            // Cập nhật level tự động từ DataManager nếu có
            if (txtLevel != null && DataManager.Instance != null)
            {
                txtLevel.text = $"LEVEL {DataManager.Instance.GetActiveLevel()}";
            }

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, 0.3f);

            _showSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            // 1. Phóng to Panel chính
            _showSequence.Append(contentPanel.DOScale(1f, popupDuration).SetEase(Ease.OutBack));

            // 2. Trái tim xuất hiện nguyên vẹn (Pop scale lên 1)
            if (heartLeft != null && heartRight != null)
            {
                _showSequence.Append(heartLeft.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                _showSequence.Join(heartRight.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

                // 3. Hiệu ứng rung động nứt vỡ (Shake)
                _showSequence.Append(heartLeft.DOShakeRotation(0.25f, new Vector3(0, 0, 8f), 15, 90, false));
                _showSequence.Join(heartRight.DOShakeRotation(0.25f, new Vector3(0, 0, 8f), 15, 90, false));

                // 4. Trái tim bị tách đôi vỡ ra (DOAnchor và DOLocalRotate với Ease.OutBack sắc nét)
                _showSequence.Append(heartLeft.DOAnchorPosX(_leftOriginPos.x - breakDistance, 0.35f).SetEase(Ease.OutBack));
                _showSequence.Join(heartLeft.DOLocalRotate(new Vector3(0, 0, breakRotation), 0.35f).SetEase(Ease.OutBack));

                _showSequence.Join(heartRight.DOAnchorPosX(_rightOriginPos.x + breakDistance, 0.35f).SetEase(Ease.OutBack));
                _showSequence.Join(heartRight.DOLocalRotate(new Vector3(0, 0, -breakRotation), 0.35f).SetEase(Ease.OutBack));
            }

            // 5. Hiệu ứng Text xuất hiện so le (Staggered Slide Down + Scale OutBack)
            float textAnimationStart = _showSequence.Duration() - 0.15f;
            if (txtLevel != null)
            {
                _showSequence.Insert(textAnimationStart, txtLevel.rectTransform.DOAnchorPosY(_levelTextOriginY, 0.4f).SetEase(Ease.OutBack));
                _showSequence.Insert(textAnimationStart, txtLevel.rectTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            }
            if (txtLevelFailed != null)
            {
                float failedTextStart = textAnimationStart + 0.12f;
                _showSequence.Insert(failedTextStart, txtLevelFailed.rectTransform.DOAnchorPosY(_levelFailedTextOriginY, 0.4f).SetEase(Ease.OutBack));
                _showSequence.Insert(failedTextStart, txtLevelFailed.rectTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            }

            // 6. Hiệu ứng các nút xuất hiện
            float buttonsStart = _showSequence.Duration() - 0.08f;
            if (btnHomeRect != null)
            {
                _showSequence.Insert(buttonsStart, btnHomeRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }
            if (btnReplayRect != null)
            {
                _showSequence.Insert(buttonsStart + 0.1f, btnReplayRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }

            _showSequence.OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                if (btnHome != null) btnHome.interactable = true;
                if (btnReplay != null) btnReplay.interactable = true;
                StartIdleAnimations();
            });
        }

        private void StartIdleAnimations()
        {
            // 1. Hiệu ứng trôi lơ lửng của tim vỡ (lệch pha sinh động)
            if (heartLeft != null)
            {
                heartLeft.DOAnchorPosY(_leftOriginPos.y + heartFloatAmount, heartFloatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject);

                heartLeft.DOLocalRotate(new Vector3(0, 0, breakRotation + heartRotateSwayAmount), heartRotateSwayDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            if (heartRight != null)
            {
                heartRight.DOAnchorPosY(_rightOriginPos.y - heartFloatAmount, heartFloatDuration * 1.15f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject);

                heartRight.DOLocalRotate(new Vector3(0, 0, -breakRotation - heartRotateSwayAmount), heartRotateSwayDuration * 1.1f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            // 2. Hiệu ứng co giãn nhẹ nhàng (breathing) cho nút Try Again
            if (btnReplayRect != null)
            {
                btnReplayRect.DOScale(Vector3.one * ctaPulseAmount, ctaPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            KillAllTweens();
            
            _hideSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _hideSequence.Join(_canvasGroup.DOFade(0f, 0.2f));
            _hideSequence.Join(contentPanel.DOScale(0.8f, 0.2f).SetEase(Ease.InQuad));

            _hideSequence.OnComplete(() =>
            {
                Hide();
                onComplete?.Invoke();
            });
        }

        private void KillAllTweens()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            
            if (contentPanel != null) contentPanel.DOKill();
            if (heartLeft != null) heartLeft.DOKill();
            if (heartRight != null) heartRight.DOKill();
            if (btnHomeRect != null) btnHomeRect.DOKill();
            if (btnReplayRect != null) btnReplayRect.DOKill();
            if (txtLevel != null) txtLevel.rectTransform.DOKill();
            if (txtLevelFailed != null) txtLevelFailed.rectTransform.DOKill();
            if (_canvasGroup != null) _canvasGroup.DOKill();
        }

        protected void OnDestroy()
        {
            if (btnReplay != null) btnReplay.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
            KillAllTweens();
        }
    }
}
