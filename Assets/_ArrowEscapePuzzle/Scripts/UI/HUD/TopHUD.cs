using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.Gameplay.Logic;
using ArrowGame.UI.Base;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.HUD
{
    public class TopHUD: BaseHUD
    {
        [Header("--- Hearts ---")]
        [SerializeField] private Image[] heartImages;
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;
        [SerializeField] private Color heartImageColor = Color.white;
        [SerializeField] private Color emptyHeartImageColor = Color.white;

        [Header("--- Heart Animation Config ---")]
        [SerializeField] private float animDuration = 0.25f;
        [SerializeField] private Vector3 heartLosePunchScale = new Vector3(1.2f, 1.2f, 1f);
        [SerializeField] private Vector3 heartGainPunchScale = new Vector3(0.3f, 0.3f, 0f);

        [Header("--- Heart Appear Animation ---")]
        [SerializeField] private float heartAppearDuration = 0.35f;
        [SerializeField] private float heartAppearStagger = 0.07f;

        [Header("--- Level ---")]
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtDifficulty;

        private int lastHeartsCount = -1;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            EventManager<LogicGameEventID>.AddListener<LevelSaveData>(LogicGameEventID.LevelLoaded, OnLevelLoaded);
            
            RefreshStartupState();
        }

        private void RefreshStartupState()
        {
            lastHeartsCount = -1;

            if (DataManager.Instance != null)
            {
                if (txtLevel != null)
                {
                    txtLevel.SetText("Level {0}", DataManager.Instance.GetActiveLevel());
                }
            }

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentLevelData != null && txtDifficulty != null)
                {
                    var levelData = GameManager.Instance.CurrentLevelData;
                    bool shouldShow = levelData.Difficulty == LevelDifficulty.Hard || levelData.Difficulty == LevelDifficulty.SuperHard;
                    txtDifficulty.gameObject.SetActive(shouldShow);
                    if (shouldShow)
                    {
                        txtDifficulty.SetText(levelData.Difficulty.ToString());
                    }
                }

                if (GameManager.Instance.HeartSystem != null)
                {
                    OnHeartChanged(GameManager.Instance.HeartSystem.CurrentHeart);
                }
            }
        }

        private void OnLevelLoaded(LevelSaveData levelData)
        {
            lastHeartsCount = -1;

            if (levelData == null) return;
            if (DataManager.Instance != null)
                txtLevel.SetText("Level {0}", DataManager.Instance.GetActiveLevel());
            if (txtDifficulty != null)
            {
                bool shouldShow = levelData.Difficulty == LevelDifficulty.Hard || levelData.Difficulty == LevelDifficulty.SuperHard;
                txtDifficulty.gameObject.SetActive(shouldShow);
                if (shouldShow)
                {
                    txtDifficulty.SetText(levelData.Difficulty.ToString());
                }
            }
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            EventManager<LogicGameEventID>.RemoveListener<LevelSaveData>(LogicGameEventID.LevelLoaded, OnLevelLoaded);
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

                bool hasHeart = i < currentHearts;
                img.sprite = hasHeart ? fullHeartSprite : emptyHeartSprite;
                img.color = hasHeart ? heartImageColor : emptyHeartImageColor;
                img.enabled = hasHeart || emptyHeartSprite != null;

                if (img.enabled)
                    AnimateAppearHeart(img, i * heartAppearStagger);
                else
                    img.rectTransform.localScale = Vector3.one;
            }
        }

        private void AnimateAppearHeart(Image heartImage, float delay)
        {
            RectTransform rt = heartImage.rectTransform;
            rt.DOKill();
            heartImage.DOKill();

            rt.localScale = Vector3.zero;
            Color targetColor = heartImage.color;
            heartImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

            Sequence seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(heartImage.gameObject, LinkBehaviour.KillOnDisable);

            if (delay > 0f) seq.AppendInterval(delay);

            seq.Append(rt.DOScale(Vector3.one, heartAppearDuration).SetEase(Ease.OutBack));
            seq.Join(heartImage.DOFade(targetColor.a, heartAppearDuration * 0.6f));
            seq.OnComplete(() => rt.localScale = Vector3.one);
        }

        private void AnimateLoseHeart(Image heartImage)
        {
            RectTransform rt = heartImage.rectTransform;
            rt.DOKill();
            heartImage.DOKill();

            heartImage.sprite = fullHeartSprite;
            rt.localScale = Vector3.one;
            heartImage.color = heartImageColor;
            heartImage.enabled = true;

            Sequence loseSeq = DOTween.Sequence().SetUpdate(true).SetLink(heartImage.gameObject, LinkBehaviour.KillOnDisable);
            loseSeq.Append(rt.DOPunchScale(heartLosePunchScale, animDuration, 2, 0.5f));
            loseSeq.Join(heartImage.DOFade(0f, animDuration));

            loseSeq.OnComplete(() =>
            {
                rt.localScale = Vector3.one;
                heartImage.color = heartImageColor;
                
                if (emptyHeartSprite != null)
                {
                    heartImage.sprite = emptyHeartSprite;
                    heartImage.color = emptyHeartImageColor;
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
            heartImage.color = heartImageColor;
            
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