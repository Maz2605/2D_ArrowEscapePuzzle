using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class GameOverResultPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI txtFinalScore;

        [Header("Top Score References")]
        [SerializeField] private List<TextMeshProUGUI> listTopScoreTxt; 
        [SerializeField] private List<RectTransform> listTopScoreRows; 

        [Header("Buttons")]
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnHome;

        private Sequence _showSequence;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void Initialize(Action onBackHome, Action onReplay)
        {
            btnHome?.onClick.AddListener(() => onBackHome?.Invoke());
            btnReplay?.onClick.AddListener(() => onReplay?.Invoke());
        }

        public void ShowResults(int currentScore, List<int> topScores)
        {
            gameObject.SetActive(true);
            
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.transform.localScale = Vector3.one * 0.8f;
            txtFinalScore.SetText("0");

            foreach (var row in listTopScoreRows)
            {
                if (row != null)
                {
                    row.localScale = new Vector3(0, 1, 1); 
                    row.gameObject.SetActive(false); 
                }
            }
            
            _showSequence?.Kill();
            _showSequence = DOTween.Sequence();
            
            _showSequence.Append(canvasGroup.DOFade(1, 0.3f));
            _showSequence.Join(canvasGroup.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            
            _showSequence.Join(DOVirtual.Int(0, currentScore, 1.0f, (val) => txtFinalScore.SetText("{0}", val))
                .SetEase(Ease.OutQuart));
            
            float startDelay = 0.3f; 

            for (int i = 0; i < listTopScoreRows.Count; i++)
            {
                if (i >= listTopScoreTxt.Count || i >= listTopScoreRows.Count) break;

                var txt = listTopScoreTxt[i];
                var rowRect = listTopScoreRows[i];

                if (txt == null || rowRect == null) continue;

                if (i < topScores.Count)
                {
                    
                    rowRect.gameObject.SetActive(true);
                    txt.SetText("{0}", topScores[i]);

                    
                    float timeInsert = startDelay + (i * 0.15f);
                    
                    _showSequence.Insert(timeInsert, 
                        rowRect.DOScaleX(1f, 0.4f).SetEase(Ease.OutBack)
                    );
                }
                else
                {
                    rowRect.gameObject.SetActive(false);
                }
            }
        }

        public void HideResults()
        {
            _showSequence?.Kill();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0, 0.2f).OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _showSequence?.Kill();
            btnHome?.onClick.RemoveAllListeners();
            btnReplay?.onClick.RemoveAllListeners();
        }
    }
}