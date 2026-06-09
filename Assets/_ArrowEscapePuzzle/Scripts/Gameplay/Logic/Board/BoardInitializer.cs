using System;
using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class BoardInitializer
    {
        private readonly BoardState _state;

        public BoardInitializer(BoardState state)
        {
            _state = state;
        }

        public void Initialize(LevelSaveData levelData)
        {
            InitEmptyGrid();
            LoadArrows(levelData.Arrows);
            LoadSpecialCells(levelData.SpecialCells);
        }

        private void InitEmptyGrid()
        {
            for (int x = 0; x < _state.Width; x++)
            {
                for (int y = 0; y < _state.Height; y++)
                {
                    _state.SetEmptyCell(x, y);
                }
            }
        }

        private void LoadArrows(List<ArrowSaveData> arrows)
        {
            if (arrows == null || arrows.Count == 0) return;

            for (int i = 0; i < arrows.Count; i++)
            {
                ArrowSaveData arrowSave = arrows[i];
                if (!ArrowModelFactory.TryCreate(arrowSave, out ArrowModel arrowModel, out string error))
                {
                    Debug.LogError($"[GridSystem] Invalid arrow data. {error}");
                    continue;
                }

                if (_state.ArrowModels.ContainsKey(arrowModel.ArrowId))
                {
                    Debug.LogError($"[GridSystem] Duplicate arrow id '{arrowModel.ArrowId}'.");
                    continue;
                }

                if (!CanPlaceArrow(arrowModel, out string placementError))
                {
                    Debug.LogError($"[GridSystem] Cannot place arrow '{arrowModel.ArrowId}'. {placementError}");
                    continue;
                }

                PlaceArrow(arrowModel);
            }
        }

        private bool CanPlaceArrow(ArrowModel arrowModel, out string error)
        {
            for (int i = 0; i < arrowModel.Path.Count; i++)
            {
                Vector2Int pos = arrowModel.Path[i];
                if (!_state.IsValidPosition(pos.x, pos.y))
                {
                    error = $"Position ({pos.x}, {pos.y}) is outside the board.";
                    return false;
                }

                if (_state.GetArrow(pos.x, pos.y).ID != BoardState.EmptyId)
                {
                    error = $"Position ({pos.x}, {pos.y}) is already occupied by arrow '{_state.GetArrow(pos.x, pos.y).ID}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private void PlaceArrow(ArrowModel arrowModel)
        {
            List<CellType> legacyCellTypes = ArrowCellTypeBuilder.BuildLegacyCellTypes(arrowModel);
            List<ArrowData> group = new List<ArrowData>(arrowModel.Path.Count);

            for (int i = 0; i < arrowModel.Path.Count; i++)
            {
                Vector2Int pos = arrowModel.Path[i];
                ArrowData node = _state.GetArrow(pos.x, pos.y);
                node.SetData(arrowModel.ArrowId, legacyCellTypes[i]);
                group.Add(node);
            }

            _state.AddArrow(arrowModel, group);
        }

        private void LoadSpecialCells(List<SpecialCellSaveData> specialCells)
        {
            _state.ClearSpecialCells();
            if (specialCells == null) return;

            List<SpecialCellSaveData> uniqueSpecialCells = CounterBlockUtility.GetUniqueRoots(specialCells);
            foreach (SpecialCellSaveData specialCell in uniqueSpecialCells)
            {
                if (specialCell == null || !_state.IsValidPosition(specialCell.Position.x, specialCell.Position.y)) continue;

                Vector2Int position = specialCell.Position;
                if (_state.GetArrow(position.x, position.y).ID != BoardState.EmptyId)
                {
                    Debug.LogWarning(
                        $"[GridSystem] Skip special cell {specialCell.Type} at occupied cell ({position.x}, {position.y}).");
                    continue;
                }

                string cellId = specialCell.Id;
                if (specialCell.Type == BoardSpecialType.CounterBlock && string.IsNullOrEmpty(cellId))
                {
                    cellId = "Blocker_" + Guid.NewGuid().ToString().Substring(0, 4);
                }

                SpecialCellSaveData normalized;
                if (specialCell is MysteryBoxSaveData mysteryBox)
                {
                    // Clone Mystery Box và giữ nguyên WrappedCell bên trong (chưa đưa ra grid hoạt động)
                    normalized = new MysteryBoxSaveData(position, cellId,
                        CounterBlockUtility.Clone(mysteryBox.WrappedCell));
                }
                else
                {
                    normalized = new SpecialCellSaveData(position, specialCell.Type,
                        specialCell.ExitDirection, specialCell.PortalId, specialCell.Counter,
                        CounterBlockUtility.CloneOffsets(specialCell.OccupiedOffsets), cellId);
                }

                _state.SetSpecialCellPosition(position, normalized);

                if (normalized.OccupiedOffsets != null)
                {
                    foreach (Vector2Int offset in normalized.OccupiedOffsets)
                    {
                        if (offset == Vector2Int.zero) continue;
                        Vector2Int targetPos = position + offset;
                        if (!_state.IsValidPosition(targetPos.x, targetPos.y)) continue;

                        if (_state.GetArrow(targetPos.x, targetPos.y).ID != BoardState.EmptyId)
                        {
                            Debug.LogWarning(
                                $"[GridSystem] Skip offset ({targetPos.x}, {targetPos.y}) for special cell {specialCell.Type} - cell occupied.");
                            continue;
                        }

                        _state.SetSpecialCellPosition(targetPos, normalized);
                    }
                }

                if (normalized.Type == BoardSpecialType.Portal)
                {
                    _state.RegisterPortal(normalized);
                }
            }
        }
    }
}
