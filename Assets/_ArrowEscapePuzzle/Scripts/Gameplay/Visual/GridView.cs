using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening; 

namespace ArrowGame.Gameplay.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float cellSize = 1.1f;
        [SerializeField] private Vector2 gridOffset;

        [Header("References")]
        [SerializeField] private ArrowLineView arrowLinePrefab; 
        [SerializeField] private Transform container;
        [SerializeField] private GameObject emptyDotPrefab; 

        private GridSystem _logic;
        private Dictionary<string, ArrowLineView> _activeLines;

        private void Awake()
        {
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
        }
        
        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.RemoveListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
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
            foreach (Transform child in container) Destroy(child.gameObject);
            _activeLines.Clear();

            foreach (var kvp in _logic.ArrowGroups)
            {
                string id = kvp.Key;
                List<ArrowData> sortedPath = kvp.Value;

                ArrowLineView lineView = Instantiate(arrowLinePrefab, container);
                lineView.transform.localPosition = Vector3.zero; 
                
                lineView.Setup(sortedPath, cellSize);
                
                _activeLines.Add(id, lineView);
            }
        }

        private GameObject SpawnSingleDot(int x, int y)
        {
            if (emptyDotPrefab == null) return null;
            
            GameObject dot = Instantiate(emptyDotPrefab, container);
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

            // Gọi Animation bay đi của mũi tên
            if (_activeLines.TryGetValue(targetID, out ArrowLineView lineView))
            {
                lineView.PlayEscapeAnimation();
                _activeLines.Remove(targetID); 
            }

            // --- TẠO SEQUENCE POPUP DOT ---
            
            int count = escapedGroup.Count;
            // Công thức move bên ArrowLineView: 0.7f + (Count * 0.08f)
            float totalMoveDuration = 0.7f + (count * 0.08f); 
            float timePerNode = totalMoveDuration / count; 

            Sequence popSeq = DOTween.Sequence();

            for (int i = 0; i < count; i++)
            {
                ArrowData arrow = escapedGroup[i];
                
                // Spawn trước nhưng giấu đi (Scale = 0)
                GameObject dot = SpawnSingleDot(arrow.X, arrow.Y);
                if (dot == null) continue;
                dot.transform.localScale = Vector3.zero;
                
                float delayTime = (i * timePerNode) + 0.45f; 

                popSeq.Insert(delayTime, dot.transform.DOScale(Vector3.one, 0.4f)
                    .SetEase(Ease.OutBack, 1.5f) 
                    .SetLink(dot)); 
            }
        }

        private void HandleArrowBlocked(ArrowData headData)
        {
            if (_activeLines.TryGetValue(headData.ID, out ArrowLineView lineView))
            {
                int emptySpaces = _logic.GetEmptyCellsBeforeBlock(headData);
                float realBumpDistance = (emptySpaces * cellSize) + 0.45f;
                lineView.PlayBlockedAnimation(realBumpDistance);
            }
        }

        private void CenterGrid()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            container.localPosition = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0) + (Vector3)gridOffset;
        }
        
        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            var arrowData = _logic.GetArrow(gridPos.x, gridPos.y); 
    
            if (arrowData != null && !string.IsNullOrEmpty(arrowData.ID))
            {
                if (_activeLines.TryGetValue(arrowData.ID, out var view))
                {
                    return view;
                }
            }
            return null;
        }
    }
}