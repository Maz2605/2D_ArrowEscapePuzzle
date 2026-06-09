using System.Collections.Generic;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class BoardOutcomeProcessor
    {
        private readonly BoardState _state;

        public BoardOutcomeProcessor(BoardState state)
        {
            _state = state;
        }

        public void RemoveArrowInternal(string targetId, ArrowEndpoint endpoint, LogicGameEventID eventToPost)
        {
            if (string.IsNullOrEmpty(targetId) || !_state.ArrowGroups.TryGetValue(targetId, out List<ArrowData> group)) return;

            EventManager<LogicGameEventID>.Post(eventToPost, group);

            endpoint ??= _state.GetPrimaryEndpoint(targetId);
            ApplyArrowRemovalOutcome(targetId, endpoint, eventToPost);

            foreach (ArrowData arrow in group) arrow.ResetData();

            _state.RemoveArrow(targetId);
            _state.RemainingArrows = Mathf.Max(0, _state.RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, _state.RemainingArrows);

            if (_state.IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

        public void RemoveActivatedArrows(ArrowActivationResult activationResult, LogicGameEventID eventToPost)
        {
            if (activationResult == null || activationResult.Entries == null || activationResult.Entries.Count == 0)
            {
                return;
            }

            EventManager<LogicGameEventID>.Post(eventToPost, activationResult);

            int removedCount = 0;
            HashSet<string> processedArrowIds = new HashSet<string>();
            for (int i = 0; i < activationResult.Entries.Count; i++)
            {
                ArrowActivationEntry entry = activationResult.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.ArrowId)) continue;
                if (!processedArrowIds.Add(entry.ArrowId)) continue;
                if (!_state.ArrowGroups.TryGetValue(entry.ArrowId, out List<ArrowData> group)) continue;

                ApplyArrowRemovalOutcome(entry.ArrowId, entry.Endpoint, eventToPost);

                foreach (ArrowData arrow in group)
                {
                    arrow.ResetData();
                }

                _state.RemoveArrow(entry.ArrowId);
                removedCount++;
            }

            if (removedCount <= 0) return;

            _state.RemainingArrows = Mathf.Max(0, _state.RemainingArrows - removedCount);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, _state.RemainingArrows);

            if (_state.IsBoardEmpty())
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
            }
        }

        private void ApplyArrowRemovalOutcome(string arrowId, ArrowEndpoint endpoint, LogicGameEventID eventToPost)
        {
            ArrowModel model = _state.GetArrowModel(arrowId);
            if (model == null) return;
            if (eventToPost == LogicGameEventID.ArrowEscaped || eventToPost == LogicGameEventID.ArrowForceRemove)
            {
                Vector2Int impactDir = endpoint != null ? endpoint.ExitDirection.ToVector2Int() : Vector2Int.zero;
                DecrementCounterBlocks(impactDir);
            }
        }

        private void DecrementCounterBlocks(Vector2Int impactDir)
        {
            List<SpecialCellSaveData> counterBlocks = _state.GetUniqueSpecialCells(BoardSpecialType.CounterBlock);
            List<SpecialCellSaveData> toRemove = new List<SpecialCellSaveData>();

            for (int i = 0; i < counterBlocks.Count; i++)
            {
                SpecialCellSaveData counterBlock = counterBlocks[i];
                counterBlock.Counter--;
                EventManager<LogicGameEventID>.Post<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, (counterBlock, impactDir));

                if (counterBlock.Counter <= 0)
                {
                    toRemove.Add(counterBlock);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                RemoveSpecialCellFootprint(toRemove[i]);
                EventManager<LogicGameEventID>.Post(LogicGameEventID.SpecialCellDestroyed, toRemove[i].Position);
            }
        }

        private void RemoveSpecialCellFootprint(SpecialCellSaveData specialCell)
        {
            foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
            {
                _state.RemoveSpecialCellPosition(occupiedPos);
            }
        }
    }
}
