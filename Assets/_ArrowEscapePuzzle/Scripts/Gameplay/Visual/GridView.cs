using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening; 

namespace ArrowGame.Gameplay.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("--- REFERENCES ---")]
        [SerializeField] private ArrowLineView arrowLinePrefab; 
        [SerializeField] private Transform container;
        [SerializeField] private GameObject emptyDotPrefab;
        
        [Header("--- 1. GRID BASE SETTINGS ---")]
        [SerializeField] private float cellSize = 1.1f;
        [SerializeField] private Vector2 gridOffset;

        [Header("--- 2. INTRO LEVEL ANIMATION ---")]
        [SerializeField] private float introSpawnDuration = 0.6f; 
        [SerializeField] private float introSpawnDelayFactor = 0.05f; 
        [Tooltip("Thời gian Intro tối thiểu (để khớp với thời gian Camera Zoom)")]
        [SerializeField] private float minIntroDuration = 1.5f;

        [Header("--- 3. DOT APPEAR ANIMATION (Arrow Escaped) ---")]
        [SerializeField] private float dotAppearInitialDelay = 0.1f; 
        [SerializeField] private float dotAppearDuration = 0.4f;     
        [SerializeField] private float delayBetweenDots = 0.15f;     
        [SerializeField] private Ease dotAppearEase = Ease.OutBack;  
        [SerializeField] private float dotTargetScale = 1f;

        [Header("--- 4. ARROW BLOCKED ANIMATION ---")]
        [SerializeField] private float blockedBumpOffset = 0.45f;

        [Header("--- 5. WIN ANIMATION (Wave Effect) ---")]
        [SerializeField] private float winWaveDelayFactor = 0.12f; 
        [SerializeField] private float winJumpHeight = 0.5f;       
        [SerializeField] private float winScaleMax = 1.15f;        
        [SerializeField] private float winJumpUpDuration = 0.25f;  
        [SerializeField] private float winFallDownDuration = 0.35f; 
        [SerializeField] private float winCompleteDelay = 0.3f;

        private GridSystem _logic;
        private Dictionary<string, ArrowLineView> _activeLines;

        private void Awake()
        {
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
        }
        
        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.RemoveListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
            
            transform.DOKill(); 
            if (container != null) 
            {
                container.DOKill(true); 
            }
        }

        public void Initialize(GridSystem logic, LevelSaveData levelData)
        {
            _logic = logic; 
            _activeLines = new Dictionary<string, ArrowLineView>();
            
            SpawnGrid();
            CenterGrid();
        }

        private void SpawnGrid()
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                
                child.DOKill(); 
                PoolingManager.Instance.Despawn(child.gameObject);
            }
            _activeLines.Clear();

            foreach (var kvp in _logic.ArrowGroups)
            {
                string id = kvp.Key;
                List<ArrowData> sortedPath = kvp.Value;

                ArrowLineView lineView = PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero, Quaternion.identity, container);
                lineView.transform.localPosition = Vector3.zero; 
                
                lineView.Setup(sortedPath, cellSize);
                
                _activeLines.Add(id, lineView);
            }
        }

        private GameObject SpawnSingleDot(int x, int y)
        {
            if (emptyDotPrefab == null) return null;
            
            GameObject dot = PoolingManager.Instance.Spawn(emptyDotPrefab, Vector3.zero, Quaternion.identity, container);
            
            dot.transform.DOKill();
            dot.transform.localScale = Vector3.zero; 
            
            dot.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
            dot.transform.SetAsFirstSibling(); 
            return dot;
        }
        
        public Vector2Int WorldToGridPos(Vector3 worldPos)
        {
            Vector2 localPos = container.InverseTransformPoint(worldPos);
            return new Vector2Int(Mathf.RoundToInt(localPos.x / cellSize), Mathf.RoundToInt(localPos.y / cellSize));
        }

        private void HandleArrowEscaped(List<ArrowData> escapedGroup)
        {
            if (escapedGroup == null || escapedGroup.Count == 0) return;

            string targetID = escapedGroup[0].ID;

            if (_activeLines.TryGetValue(targetID, out ArrowLineView lineView))
            {
                lineView.PlayEscapeAnimation();
                _activeLines.Remove(targetID); 
            }

            int count = escapedGroup.Count;
            
            for (int i = 0; i < count; i++)
            {
                ArrowData arrow = escapedGroup[i];
                
                GameObject dot = SpawnSingleDot(arrow.X, arrow.Y);
                if (dot == null) continue;
                
                float delayTime = dotAppearInitialDelay + (i * delayBetweenDots); 

                dot.transform.DOScale(Vector3.one * dotTargetScale, dotAppearDuration)
                    .SetDelay(delayTime)
                    .SetEase(dotAppearEase)
                    .SetLink(dot, LinkBehaviour.KillOnDisable);
            }
        }

        private void HandleArrowBlocked(ArrowData headData)
        {
            if (_activeLines.TryGetValue(headData.ID, out ArrowLineView lineView))
            {
                int emptySpaces = _logic.GetEmptyCellsBeforeBlock(headData);
                float realBumpDistance = (emptySpaces * cellSize) + blockedBumpOffset;
                lineView.PlayBlockedAnimation(realBumpDistance);
            }
        }

        private void CenterGrid()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            container.localPosition = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0) + (Vector3)gridOffset;
        }

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Win: 
                    PlayWinAnimation();
                    break;
                case GameState.IntroLevel:
                    PlayIntroLevelAnimation();
                    break;
            }
        }

        private void PlayIntroLevelAnimation()
        {
            float delay = 0f;
            foreach (var kvp in _activeLines)
            {
                ArrowLineView lineView = kvp.Value;
                lineView.PlaySpawnAnimation(delay, introSpawnDuration);
                delay += introSpawnDelayFactor; 
            }

            float arrowsDuration = _activeLines.Count > 0 ? (_activeLines.Count - 1) * introSpawnDelayFactor + introSpawnDuration : 0f;
            
            float totalDuration = Mathf.Max(minIntroDuration, arrowsDuration);
            
            DOVirtual.DelayedCall(totalDuration, () => 
            {
                EventManager<VisualEventID>.Post(VisualEventID.IntroAnimationComplete);
            });
        }

        private void PlayWinAnimation()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            Vector3 centerPos = new Vector3(totalWidth / 2f, totalHeight / 2f, 0);

            Sequence masterSeq = DOTween.Sequence();

            for (int i = 0; i < container.childCount; i++)
            {
                Transform dot = container.GetChild(i);
                float dist = Vector3.Distance(dot.localPosition, centerPos);
                
                float delay = dist * winWaveDelayFactor; 
                Vector3 originalPos = dot.localPosition;

                Sequence dotSeq = DOTween.Sequence();

                dotSeq.Append(dot.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
                dotSeq.Join(dot.DOScale(winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));

                dotSeq.Append(dot.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
                dotSeq.Join(dot.DOScale(1f, winFallDownDuration).SetEase(Ease.OutBounce));

                masterSeq.Insert(delay, dotSeq);
                dotSeq.SetLink(dot.gameObject, LinkBehaviour.KillOnDisable);
            }

            masterSeq.AppendInterval(winCompleteDelay);
            masterSeq.OnComplete(() => 
            {
                EventManager<VisualEventID>.Post(VisualEventID.WinAnimationComplete);
            });
        }
        
        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            var arrowData = _logic.GetArrow(gridPos.x, gridPos.y); 
    
            if (arrowData != null && !string.IsNullOrEmpty(arrowData.ID))
            {
                if (_activeLines.TryGetValue(arrowData.ID, out var view)) return view;
            }
            return null;
        }
    }
}