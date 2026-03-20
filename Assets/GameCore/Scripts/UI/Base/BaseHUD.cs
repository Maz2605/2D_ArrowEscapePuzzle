using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI.Base
{
    public class BaseHUD : MonoBehaviour
    {
        [Header("Default Buttons")] 
        [SerializeField] protected Button btnHome;
        [SerializeField] protected Button btnPause;
        [SerializeField] protected Button btnReplay;
        
        [Header("Text")]
        [SerializeField] protected TextMeshProUGUI txtCurrentScore;
        [SerializeField] protected TextMeshProUGUI txtHighScore;

        [Header("Components")] 
        [SerializeField] protected CanvasGroup gamePlayPanel;
        [SerializeField] protected GameOverResultPanel resultPanel;
        
        public Action OnPauseClicked;
        public Action OnReplayClicked;
        public Action OnReplayWithoutPopupClicked;
        public Action OnHomeWithoutPopupClicked;
        public Action OnHomeClicked;
        
        protected Tween ScoreTween;
        protected Tween HighScoreTween;

        public virtual void Initialize()
        {
            BindButton(btnHome,() => OnHomeClicked?.Invoke());
            BindButton(btnPause, () => OnPauseClicked?.Invoke());
            BindButton(btnReplay, () => OnReplayClicked?.Invoke());

            if (resultPanel != null)
            {
                resultPanel.Initialize(
                    () => OnHomeWithoutPopupClicked?.Invoke(),
                    () => OnReplayWithoutPopupClicked?.Invoke()
                );
            }
        }

        protected virtual void OnEnable()
        {
            ShowPanel(gamePlayPanel, true);
        }

        protected virtual void OnDisable()
        {
            transform.DOKill();
        }

        protected virtual void OnDestroy()
        {
            btnHome?.onClick.RemoveAllListeners();
            btnReplay?.onClick.RemoveAllListeners();
            btnPause?.onClick.RemoveAllListeners();
        }
        
        protected void AnimateScoreText(TextMeshProUGUI tmp, int score, ref Tween tweenRef)
        {
            if (tmp == null) return;

            tmp.SetText("{0}", score);

            if (tweenRef != null && tweenRef.IsActive()) tweenRef.Kill();

            tmp.transform.localScale = Vector3.one;

            tweenRef = tmp.transform
                .DOPunchScale(Vector3.one * 0.3f, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetLink(tmp.gameObject);
        }
        
        protected void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() =>
            {
                btn.transform.DOKill(true);
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true) 
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }

        
        protected void ShowPanel(CanvasGroup cg, bool isShow)
        {
            if (cg == null) return;

            cg.DOKill();
            cg.transform.DOKill();

            if (isShow)
            {
                cg.alpha = 0f;
                cg.transform.localScale = Vector3.one * 0.8f; 
                cg.interactable = true;
                cg.blocksRaycasts = true;

                cg.DOFade(1f, 0.3f).SetUpdate(true);
                cg.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            else
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;

                cg.DOFade(0f, 0.2f).SetUpdate(true);
                cg.transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).SetUpdate(true);
            }
        }

        protected virtual void ShowResultPanel(int score, List<int> topScores)
        {
            if(resultPanel == null) return;
            resultPanel.ShowResults(score, topScores);
        }

        protected virtual void HideResultPanel()
        {
            if (resultPanel != null) resultPanel.HideResults();
        }
    }
}