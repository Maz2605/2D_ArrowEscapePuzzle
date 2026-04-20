using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.HUD
{
    public class TopHUD: BaseHUD
    {
        [Header("--- Coins ---")]
        [SerializeField] private TextMeshPro txtCoin;
        [SerializeField] private Transform coinIcon;

        [Header("--- Hearts ---")]
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;

        [Header("--- Heart Animation Config ---")]
        [SerializeField] private float animDuration = 0.25f;
        [SerializeField] private Vector3 heartLosePunchScale = new Vector3(1.2f, 1.2f, 1f);
        [SerializeField] private Vector3 heartGainPunchScale = new Vector3(0.3f, 0.3f, 0f);

        [Header("--- Level ---")]
        [SerializeField] private TextMeshProUGUI txtLevel;

        private int lastHeartsCount = -1;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.RequestLoadLevel, OnLoadLevel);
            if (DataManager.Instance != null)
                txtLevel.SetText("Level {0}", DataManager.Instance.GetCurrentLevel());
        }

        private void OnLoadLevel()
        {
            if (DataManager.Instance != null)
                txtLevel.SetText("Level {0}", DataManager.Instance.GetCurrentLevel());
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.RequestLoadLevel, OnLoadLevel); 
        }

        private void OnCoinChanged(int currentCoin)
        {
            txtCoin.text = currentCoin.ToString();
            if (coinIcon != null)
            {
                coinIcon.DOKill();
                coinIcon.localScale = Vector3.one;
                coinIcon.DOPunchScale(Vector3.one * 0.3f, 0.2f, 5, 1f)
                    .SetUpdate(true)
                    .SetLink(coinIcon.gameObject, LinkBehaviour.KillOnDisable); 
            }
        }

        private void OnHeartChanged(int currentHearts)
        {
            if (lastHeartsCount == -1)
            {
                InitializeHeartsUI(currentHearts);
                lastHeartsCount = currentHearts;
                return;
            }

            if (currentHearts == lastHeartsCount) return;

            if (currentHearts < lastHeartsCount)
            {
                for (int i = currentHearts; i < lastHeartsCount; i++)
                {
                    if (i >= 0 && i < heartImages.Length) AnimateLoseHeart(heartImages[i]);
                }
            }
            else
            {
                for (int i = lastHeartsCount; i < currentHearts; i++)
                {
                    if (i >= 0 && i < heartImages.Length) AnimateGainHeart(heartImages[i]);
                }
            }

            lastHeartsCount = currentHearts;
        }

        private void InitializeHeartsUI(int currentHearts)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                var img = heartImages[i];
                img.rectTransform.DOKill();
                img.DOKill();

                img.rectTransform.localScale = Vector3.one;
                img.color = Color.white;
                
                bool hasHeart = i < currentHearts;
                img.sprite = hasHeart ? fullHeartSprite : emptyHeartSprite;
                img.enabled = hasHeart || emptyHeartSprite != null;
            }
        }

        private void AnimateLoseHeart(Image heartImage)
        {
            RectTransform rt = heartImage.rectTransform;
            rt.DOKill();
            heartImage.DOKill();

            heartImage.sprite = fullHeartSprite;
            rt.localScale = Vector3.one;
            heartImage.color = Color.white;
            heartImage.enabled = true;

            Sequence loseSeq = DOTween.Sequence().SetUpdate(true).SetLink(heartImage.gameObject, LinkBehaviour.KillOnDisable);
            loseSeq.Append(rt.DOPunchScale(heartLosePunchScale, animDuration, 2, 0.5f));
            loseSeq.Join(heartImage.DOFade(0f, animDuration));

            loseSeq.OnComplete(() =>
            {
                rt.localScale = Vector3.one;
                heartImage.color = Color.white;
                
                if (emptyHeartSprite != null)
                {
                    heartImage.sprite = emptyHeartSprite;
                }
                else
                {
                    heartImage.enabled = false;
                }
            });
        }

        private void AnimateGainHeart(Image heartImage)
        {
            RectTransform rt = heartImage.rectTransform;
            rt.DOKill();
            heartImage.DOKill();

            heartImage.enabled = true;
            heartImage.sprite = fullHeartSprite;
            heartImage.color = Color.white;
            
            rt.localScale = Vector3.zero;
            rt.DOPunchScale(heartGainPunchScale, animDuration, 5, 1f)
                .SetUpdate(true)
                .SetLink(heartImage.gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => rt.localScale = Vector3.one);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            transform.DOKill();
        }
    }
}