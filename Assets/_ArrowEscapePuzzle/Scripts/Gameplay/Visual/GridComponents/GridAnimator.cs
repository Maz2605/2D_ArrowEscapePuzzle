using System;
using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual.GridComponents
{
    public class GridAnimator : MonoBehaviour
    {
        [Header("--- REFERENCES ---")]
        [SerializeField] private GameObject emptyDotPrefab;

        [Header("--- 2. INTRO LEVEL ANIMATION ---")]
        [SerializeField] private float introSpawnDuration = 0.6f;
        [SerializeField] private float introSpawnDelayFactor = 0.05f;
        [SerializeField] private float maxAllowedIntroDuration = 2.0f;

        [Header("--- 3. DOT APPEAR ANIMATION ---")]
        [SerializeField] private float dotAppearInitialDelay = 0.1f;
        [SerializeField] private float dotAppearDuration = 0.4f;
        [SerializeField] private float delayBetweenDots = 0.15f;
        [SerializeField] private Ease dotAppearEase = Ease.OutBack;
        [SerializeField] private float dotTargetScale = 1f;

        [Header("--- 5. LOSE ANIMATION ---")]
        [SerializeField] private float gridSagOffset = -0.15f;
        [SerializeField] private float gridSagDuration = 0.6f;
        [SerializeField] private float delayBeforeLoseEvent = 1.0f;

        [Header("--- 6. WIN ANIMATION ---")]
        [SerializeField] private float winWaveDelayFactor = 0.12f;
        [SerializeField] private float winJumpHeight = 0.5f;
        [SerializeField] private float winScaleMax = 1.15f;
        [SerializeField] private float winJumpUpDuration = 0.25f;
        [SerializeField] private float winFallDownDuration = 0.35f;
        [SerializeField] private float winCompleteDelay = 0.3f;

        private Sequence _winSequence;
        private Transform _container;
        private Transform _dotRoot;

        public float MaxAllowedIntroDuration => maxAllowedIntroDuration;

        public void Initialize(Transform container, Transform dotRoot)
        {
            _container = container;
            _dotRoot = dotRoot;
        }

        public void PlayGridImpactBounce()
        {
            if (_container == null) return;

            _container.DOKill(false);
            Sequence bounce = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            bounce.Append(_container.DOScale(0.975f, 0.07f).SetEase(Ease.OutCubic));
            bounce.Append(_container.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
        }

        public void PlayEmptyDotsForGroup(IReadOnlyList<ArrowData> arrowGroup, float cellSize)
        {
            if (arrowGroup == null || arrowGroup.Count == 0) return;

            List<Vector2Int> positions = new List<Vector2Int>(arrowGroup.Count);
            for (int i = 0; i < arrowGroup.Count; i++)
            {
                ArrowData arrow = arrowGroup[i];
                positions.Add(new Vector2Int(arrow.X, arrow.Y));
            }

            PlayEmptyDotsAtPositions(positions, cellSize);
        }

        public void PlayEmptyDotsAtPositions(IReadOnlyList<Vector2Int> positions, float cellSize)
        {
            if (positions == null || positions.Count == 0 || _dotRoot == null || emptyDotPrefab == null) return;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                GameObject dot = PoolingManager.Instance.Spawn(emptyDotPrefab, Vector3.zero, Quaternion.identity, _dotRoot);
                dot.transform.DOKill();
                dot.transform.localScale = Vector3.zero;
                dot.transform.localPosition = new Vector3(pos.x * cellSize, pos.y * cellSize, 0f);

                float delayTime = dotAppearInitialDelay + (i * delayBetweenDots);
                dot.transform.DOScale(Vector3.one * dotTargetScale, dotAppearDuration)
                    .SetDelay(delayTime)
                    .SetEase(dotAppearEase)
                    .SetLink(dot, LinkBehaviour.KillOnDisable);
            }
        }

        public void PlayIntroLevelAnimation(IReadOnlyDictionary<string, ArrowLineView> activeLines,
            IEnumerable<SpecialCellViewBase> specialViews)
        {
            int totalArrows = activeLines != null ? activeLines.Count : 0;
            int spawnBatchSize = totalArrows > 0
                ? CalculateStaggerBatchSize(totalArrows, introSpawnDuration, introSpawnDelayFactor, maxAllowedIntroDuration)
                : 1;
            int completedArrows = 0;
            bool introCompletePosted = false;

            float delay = 0f;
            int currentBatchCount = 0;
            bool showLine = BoosterManager.Instance.IsLineGuideActive();

            if (activeLines != null)
            {
                foreach (KeyValuePair<string, ArrowLineView> kvp in activeLines)
                {
                    kvp.Value.PlaySpawnAnimation(delay, introSpawnDuration, showLine, () =>
                    {
                        completedArrows++;
                        if (!introCompletePosted && completedArrows >= totalArrows)
                        {
                            introCompletePosted = true;
                            EventManager<VisualEventID>.Post(VisualEventID.GridIntroComplete);
                        }
                    });

                    currentBatchCount++;
                    if (currentBatchCount >= spawnBatchSize)
                    {
                        delay += introSpawnDelayFactor;
                        currentBatchCount = 0;
                    }
                }
            }

            if (specialViews != null)
            {
                foreach (SpecialCellViewBase view in specialViews)
                {
                    if (view != null) view.PlaySpawnAnimation(0f, introSpawnDuration);
                }
            }

            if (totalArrows == 0)
            {
                EventManager<VisualEventID>.Post(VisualEventID.GridIntroComplete);
            }
        }

        public void PlayWinAnimation(IReadOnlyDictionary<string, ArrowLineView> activeLines,
            IEnumerable<SpecialCellViewBase> specialViews, int gridWidth, int gridHeight, float cellSize)
        {
            float totalWidth = (gridWidth - 1) * cellSize;
            float totalHeight = (gridHeight - 1) * cellSize;
            Vector3 centerPos = new Vector3(totalWidth / 2f, totalHeight / 2f, 0f);

            _winSequence?.Kill();
            _winSequence = DOTween.Sequence().SetId(this).SetLink(gameObject);

            if (activeLines != null)
            {
                foreach (KeyValuePair<string, ArrowLineView> kvp in activeLines)
                {
                    if (kvp.Value == null) continue;

                    Transform arrowTransform = kvp.Value.transform;
                    float dist = Vector3.Distance(arrowTransform.localPosition, centerPos);
                    float delay = dist * winWaveDelayFactor;
                    Vector3 originalPos = arrowTransform.localPosition;

                    Sequence arrowSeq = DOTween.Sequence();
                    arrowSeq.Append(arrowTransform.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
                    arrowSeq.Join(arrowTransform.DOScale(winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
                    arrowSeq.Append(arrowTransform.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
                    arrowSeq.Join(arrowTransform.DOScale(1f, winFallDownDuration).SetEase(Ease.OutBounce));

                    _winSequence.Insert(delay, arrowSeq);
                    arrowSeq.SetLink(arrowTransform.gameObject, LinkBehaviour.KillOnDisable);
                }
            }

            if (_dotRoot != null)
            {
                for (int i = 0; i < _dotRoot.childCount; i++)
                {
                    Transform dot = _dotRoot.GetChild(i);
                    float dist = Vector3.Distance(dot.localPosition, centerPos);
                    float delay = dist * winWaveDelayFactor;
                    Vector3 originalPos = dot.localPosition;

                    Sequence dotSeq = DOTween.Sequence();
                    dotSeq.Append(dot.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
                    dotSeq.Join(dot.DOScale(winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
                    dotSeq.Append(dot.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
                    dotSeq.Join(dot.DOScale(1f, winFallDownDuration).SetEase(Ease.OutBounce));

                    _winSequence.Insert(delay, dotSeq);
                    dotSeq.SetLink(dot.gameObject, LinkBehaviour.KillOnDisable);
                }
            }

            if (specialViews != null)
            {
                foreach (SpecialCellViewBase view in specialViews)
                {
                    if (view == null) continue;
                    float dist = Vector3.Distance(view.transform.localPosition, centerPos);
                    float delay = dist * winWaveDelayFactor;
                    view.PlayWinAnimation(delay, winJumpHeight, winJumpUpDuration, winFallDownDuration, winScaleMax);
                }
            }

            _winSequence.AppendInterval(winCompleteDelay);
            _winSequence.OnComplete(() => { EventManager<VisualEventID>.Post(VisualEventID.WinAnimationComplete); });
        }

        public void PlayLoseAnimation(IReadOnlyDictionary<string, ArrowLineView> activeLines,
            IEnumerable<SpecialCellViewBase> specialViews)
        {
            if (activeLines != null)
            {
                foreach (KeyValuePair<string, ArrowLineView> kvp in activeLines)
                {
                    if (kvp.Value != null) kvp.Value.PlayLoseAnimation();
                }
            }

            ThemeConfigSO theme = ThemeManager.Instance.CurrentTheme;
            Color loseColor = theme != null ? theme.arrowLoseColor : new Color(0.5f, 0.5f, 0.5f, 0.5f);

            if (specialViews != null)
            {
                foreach (SpecialCellViewBase view in specialViews)
                {
                    if (view != null) view.PlayLoseAnimation(gridSagDuration, 0.8f, loseColor);
                }
            }

            if (_container == null)
            {
                EventManager<VisualEventID>.Post(VisualEventID.LoseAnimationComplete);
                return;
            }

            Vector3 originalLocalPos = _container.localPosition;
            _winSequence?.Kill();
            _container.DOKill();

            _container.DOLocalMoveY(originalLocalPos.y + gridSagOffset, gridSagDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => { _container.localPosition = originalLocalPos; })
                .SetLink(_container.gameObject);

            DOVirtual.DelayedCall(delayBeforeLoseEvent,
                    () => { EventManager<VisualEventID>.Post(VisualEventID.LoseAnimationComplete); })
                .SetId(this)
                .SetLink(gameObject);
        }

        public static int CalculateStaggerBatchSize(int totalItems, float itemDuration, float delayFactor,
            float maxAllowedDuration)
        {
            if (totalItems <= 0) return 1;

            int batchSize = 1;
            float timeIfSpawnSingle = (totalItems - 1) * delayFactor + itemDuration;

            if (timeIfSpawnSingle > maxAllowedDuration)
            {
                float availableTimeForDelays = maxAllowedDuration - itemDuration;
                int maxAllowedWaves = Mathf.FloorToInt(availableTimeForDelays / delayFactor) + 1;
                maxAllowedWaves = Mathf.Max(1, maxAllowedWaves);
                batchSize = Mathf.CeilToInt((float)totalItems / maxAllowedWaves);
            }

            return batchSize;
        }

        private void OnDisable()
        {
            _winSequence?.Kill();
            transform.DOKill();
            if (_container != null) _container.DOKill();
        }
    }
}
