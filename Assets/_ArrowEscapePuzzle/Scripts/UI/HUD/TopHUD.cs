using ArrowGame.Data.Events;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.HUD
{
    public class TopHUD :  MonoBehaviour
    {
        [Header("--- Coins ---")]
        [SerializeField] private TextMeshPro txtCoin;
        [SerializeField] private Transform coinIcon; 

        [Header("--- Hearts (Lives) ---")]
        [SerializeField] private Image[] heartImages; 
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;
        
        [Header("--- Level ---")]
        [SerializeField] private TextMeshProUGUI txtLevel;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            // EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.CoinChanged, OnCoinChanged);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.HeartChanged, OnHeartChanged);
            // EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.CoinChanged, OnCoinChanged);
        }

        private void OnCoinChanged(int currentCoin)
        {
            txtCoin.text = currentCoin.ToString();

            if (coinIcon != null)
            {
                coinIcon.DOKill();
                coinIcon.localScale = Vector3.one;
                coinIcon.DOPunchScale(Vector3.one * 0.3f, 0.2f, 5, 1f).SetUpdate(true);
            }
        }

        private void OnHeartChanged(int currentHearts)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (i < currentHearts)
                {
                    heartImages[i].sprite = fullHeartSprite;
                }
                else
                {
                    heartImages[i].sprite = emptyHeartSprite;
                }
            }
        }
    }
}
