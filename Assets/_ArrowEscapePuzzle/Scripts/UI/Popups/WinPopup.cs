using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using ArrowGame.Gameplay.Controllers;

namespace ArrowGame.UI.Popups
{
    [Serializable]
    public struct StarUIComponent
    {
        public RectTransform container; 
        
        public Image fullStar; 
    }

    public class WinPopup : BasePopup
    {
        [Header("--- Level Info ---")]
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private List<StarUIComponent> starComponents = new List<StarUIComponent>();

        [Header("--- Coin UI ---")]
        [SerializeField] private TextMeshProUGUI txtCoinReward;
        [SerializeField] private TextMeshProUGUI txtTotalCoin;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnNextLevel;
        [SerializeField] private Button btnHome;

        private Sequence _winSequence;

        protected override void Awake()
        {
            base.Awake();
            if (btnNextLevel != null) btnNextLevel.onClick.AddListener(OnNextClicked);
            if (btnHome != null) btnHome.onClick.AddListener(OnHomeClicked);
        }

        public void SetupAndAnimate(int levelIndex,int targetStars, int targetCoins)
        {
            int currentTotalCoin = DataManager.Instance.GetCurrentCoin(); 
            int coinBeforeReward = currentTotalCoin - targetCoins;
            int currentLevel = DataManager.Instance.GetCurrentLevel();

            if (txtLevel != null) txtLevel.text = $"LEVEL {levelIndex}";
            if (txtTotalCoin != null) 
            {
                txtTotalCoin.text = coinBeforeReward.ToString();
                txtTotalCoin.transform.localScale = Vector3.one;
            }
            if (txtCoinReward != null) 
            {
                txtCoinReward.text = "+0";
                txtCoinReward.transform.localScale = Vector3.zero; // Ẩn đi chờ animate
            }

            if (starComponents != null)
            {
                foreach (var star in starComponents)
                {
                    if (star.container != null) star.container.localScale = Vector3.one;

                    if (star.fullStar != null)
                    {
                        star.fullStar.gameObject.SetActive(false);
                        star.fullStar.transform.localScale = Vector3.zero; 
                    }
                }
            }

            _winSequence?.Kill();
            _winSequence = DOTween.Sequence()
                .SetUpdate(true) 
                .SetLink(gameObject); 

            _winSequence.AppendInterval(0.2f); 

            if (starComponents != null && starComponents.Count > 0)
            {
                for (int i = 0; i < starComponents.Count; i++)
                {
                    int index = i; 
                    StarUIComponent star = starComponents[index];

                    float starAppearDelay = 0.3f;
                    
                    if (index >= targetStars)
                    {
                        _winSequence.AppendInterval(starAppearDelay);
                        continue;
                    }

                    if (star.fullStar == null || star.container == null) continue;

                    _winSequence.AppendCallback(() => star.fullStar.gameObject.SetActive(true));
                    
                    _winSequence.Append(star.fullStar.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

                    _winSequence.Join(star.container.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f));

                    _winSequence.AppendInterval(0.1f); 
                }
            }

            _winSequence.AppendInterval(0.1f); 
            
            if (txtCoinReward != null)
            {
                int displayReward = 0;
                _winSequence.Append(txtCoinReward.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                _winSequence.Append(
                    DOTween.To(() => displayReward, x => 
                    {
                        displayReward = x;
                        txtCoinReward.text = $"+{displayReward}";
                    }, targetCoins, 0.6f).SetEase(Ease.OutExpo)
                );
            }

            if (txtTotalCoin != null)
            {
                int displayTotal = coinBeforeReward;
                _winSequence.Append(
                    DOTween.To(() => displayTotal, x => 
                    {
                        displayTotal = x;
                        txtTotalCoin.text = displayTotal.ToString();
                    }, currentTotalCoin, 0.5f).SetEase(Ease.Linear)
                );
                _winSequence.Append(txtTotalCoin.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f));
            }
        }

        private void OnNextClicked()
        {
            Hide();
            EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
        }

        private void OnHomeClicked()
        {
            Hide();
            GameManager.Instance.RequestBackHome();
        }

        protected override void PlayShowAnimation()
        {
            transform.localScale = Vector3.one * 0.8f;
            transform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
            
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            transform.DOScale(Vector3.one * 0.8f, animDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        protected void OnDestroy()
        {
            if (btnNextLevel != null) btnNextLevel.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
            
            _winSequence?.Kill();
            transform.DOKill();
        }
    }
}