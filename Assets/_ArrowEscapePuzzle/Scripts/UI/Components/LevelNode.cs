using System;
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Components
{
    public enum LevelNodeState
    {
        Locked,
        Current,
        Passed
    }

    public class LevelNode : MonoBehaviour, IPoolable 
    {
        [Header("--- References ---")]
        [SerializeField] private Button btnNode;
        [SerializeField] private TextMeshProUGUI txtLevelIndex;
        [SerializeField] private StarBarWidget starBarWidget;
        [SerializeField] private GameObject objLock;
        
        [Header("--- Visual ---")]
        [Tooltip("Kéo cái Empty Object chứa toàn bộ hình ảnh vào đây để Scale không bị lỗi Layout")]
        [SerializeField] private Transform visualRoot; 

        private int _levelIndex;
        private Action<int> _onClickCallback;

        private void Awake()
        {
            if (btnNode == null) btnNode = GetComponent<Button>();
            if (visualRoot == null) visualRoot = transform; 
            
            btnNode.onClick.AddListener(OnNodeClicked);
        }

        public void SetupNode(int levelIndex, LevelNodeState state, int starsAchieved, Action<int> onClick)
        {
            _levelIndex = levelIndex;
            _onClickCallback = onClick;

            if (txtLevelIndex != null) txtLevelIndex.text = levelIndex.ToString();

            if (objLock != null) objLock.SetActive(false);
            if (starBarWidget != null) starBarWidget.gameObject.SetActive(false);

            switch (state)
            {
                case LevelNodeState.Locked:
                    btnNode.interactable = false;
                    if (objLock != null) objLock.SetActive(true);
                    break;

                case LevelNodeState.Current:
                    btnNode.interactable = true;
                    if (starBarWidget != null)
                    {
                        starBarWidget.gameObject.SetActive(true);
                        starBarWidget.DisplayStars(0);
                    }
                    break;

                case LevelNodeState.Passed:
                    btnNode.interactable = true;
                    if (starBarWidget != null)
                    {
                        starBarWidget.gameObject.SetActive(true);
                        starBarWidget.DisplayStars(starsAchieved);
                    }
                    break;
            }
        }

        private void OnNodeClicked()
        {
            visualRoot.DOScale(0.9f, 0.1f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutQuad);
            
            _onClickCallback?.Invoke(_levelIndex);
        }


        public void OnSpawn()
        {
            if (visualRoot != null) visualRoot.localScale = Vector3.one;
            transform.localScale = Vector3.one;
            
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        public void OnDespawn()
        {
            if (visualRoot != null) DOTween.Kill(visualRoot);
            DOTween.Kill(transform);

            _onClickCallback = null; 
        }
    }
}