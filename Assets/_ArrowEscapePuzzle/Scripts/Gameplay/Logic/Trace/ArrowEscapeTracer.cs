using System.Collections.Generic;
using ArrowGame.Gameplay.Logic.SpecialCells;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowEscapeTracer
    {
        private readonly BoardState _state;

        public ArrowEscapeTracer(BoardState state)
        {
            _state = state;
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId)
        {
            ArrowEndpoint endpoint = _state.GetPrimaryEndpoint(arrowId);
            return TraceEscapeRoute(arrowId, endpoint);
        }

        public EscapeTraceResult TraceEscapeRoute(ArrowData headArrow)
        {
            if (headArrow == null || string.IsNullOrEmpty(headArrow.ID))
            {
                return new EscapeTraceResult(string.Empty, Direction4.Up);
            }

            return TraceEscapeRoute(headArrow.ID);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint)
        {
            return TraceEscapeRoute(arrowId, endpoint, true);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint, bool cacheLiveResult,
            string activationGroupKey = "")
        {
            Direction4 initialDirection = endpoint != null ? endpoint.ExitDirection : Direction4.Up;
            EscapeTraceResult result = new EscapeTraceResult(arrowId, initialDirection,
                endpoint?.EndpointKey ?? string.Empty, endpoint?.PathIndex ?? -1);
            result.ActivationGroupKey = activationGroupKey ?? string.Empty;

            if (string.IsNullOrEmpty(arrowId) || endpoint == null)
            {
                if (cacheLiveResult) _state.StoreTraceResult(result);
                return result;
            }

            Vector2Int direction = initialDirection.ToVector2Int();
            int checkX = endpoint.Position.x + direction.x;
            int checkY = endpoint.Position.y + direction.y;
            string startId = arrowId;
            ArrowModel movingModel = _state.GetArrowModel(arrowId);
            HashSet<string> visitedStates = new HashSet<string>();

            while (true)
            {
                if (!_state.IsValidPosition(checkX, checkY))
                {
                    result.CanEscape = true;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) _state.StoreTraceResult(result);
                    return result;
                }

                string stateKey = $"{checkX}:{checkY}:{direction.x}:{direction.y}";
                if (!visitedStates.Add(stateKey))
                {
                    result.BlockReason = EscapeBlockReason.Loop;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) _state.StoreTraceResult(result);
                    return result;
                }

                ArrowData cell = _state.GetArrow(checkX, checkY);
                if (IsBlockedByArrow(movingModel, endpoint, cell, result.DistanceBeforeStop + 1,
                        out string blockerId))
                {
                    result.BlockReason = EscapeBlockReason.OtherArrow;
                    result.BlockerId = blockerId;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) _state.StoreTraceResult(result);
                    return result;
                }

                Vector2Int currentPosition = new Vector2Int(checkX, checkY);
                result.AddWaypoint(currentPosition, 1f);

                SpecialCellSaveData specialCell = _state.GetSpecialCellAt(currentPosition.x, currentPosition.y);
                if (specialCell != null)
                {
                    ISpecialCellLogic logic = SpecialCellLogicFactory.GetLogic(specialCell.Type);
                    if (logic != null)
                    {
                        TraceContext context = new TraceContext(arrowId, movingModel, endpoint, currentPosition,
                            direction, result.RouteWaypoints.Count - 1, result, _state);
                        SpecialCellStepResult stepResult = logic.Evaluate(context, specialCell);
                        if (!stepResult.Handled)
                        {
                            result.FinalDirection = Direction4Extensions.FromVector(direction);
                            checkX += direction.x;
                            checkY += direction.y;
                            continue;
                        }

                        ApplySpecialCellStepResult(result, stepResult);

                        if (stepResult.ShouldStop)
                        {
                            if (cacheLiveResult) _state.StoreTraceResult(result);
                            return result;
                        }

                        direction = stepResult.NextDirection;
                        checkX = stepResult.NextPosition.x;
                        checkY = stepResult.NextPosition.y;
                        continue;
                    }
                }

                result.FinalDirection = Direction4Extensions.FromVector(direction);
                checkX += direction.x;
                checkY += direction.y;
            }
        }

        private bool IsBlockedByArrow(ArrowModel movingModel, ArrowEndpoint activeEndpoint,
            ArrowData candidateCell, int travelDistanceToCandidate, out string blockerId)
        {
            blockerId = string.Empty;
            if (candidateCell == null || candidateCell.ID == BoardState.EmptyId || movingModel == null)
            {
                return false;
            }

            if (candidateCell.ID != movingModel.ArrowId)
            {
                ArrowModel blockerModel = _state.GetArrowModel(candidateCell.ID);
                if (movingModel.Mechanics.CanPassThroughLinkedArrow(movingModel, blockerModel))
                {
                    return false;
                }

                blockerId = candidateCell.ID;
                return true;
            }

            if (!IsSelfBodyStillOccupying(movingModel, activeEndpoint, candidateCell, travelDistanceToCandidate))
            {
                return false;
            }

            blockerId = candidateCell.ID;
            return true;
        }

        private static bool IsSelfBodyStillOccupying(ArrowModel movingModel, ArrowEndpoint activeEndpoint,
            ArrowData candidateCell, int travelDistanceToCandidate)
        {
            if (movingModel == null || activeEndpoint == null || candidateCell == null || movingModel.Path == null)
            {
                return false;
            }

            int pathCount = movingModel.Path.Count;
            if (pathCount <= 0) return false;

            Vector2Int candidatePosition = new Vector2Int(candidateCell.X, candidateCell.Y);
            for (int i = 0; i < pathCount; i++)
            {
                if (movingModel.Path[i] != candidatePosition) continue;

                int distanceFromTail = activeEndpoint.PathIndex == 0 ? pathCount - 1 - i : i;
                return distanceFromTail >= travelDistanceToCandidate;
            }

            return false;
        }

        private static void ApplySpecialCellStepResult(EscapeTraceResult result, SpecialCellStepResult stepResult)
        {
            if (stepResult.RemoveCurrentWaypoint)
            {
                RemoveLastWaypoint(result);
            }

            if (stepResult.PortalJump != null)
            {
                int exitWaypointIndex = result.AddWaypoint(stepResult.PortalJump.ExitPosition, 0f, true);
                result.AddPortalJump(stepResult.PortalJump.EntryWaypointIndex, exitWaypointIndex,
                    stepResult.PortalJump.EntryPosition, stepResult.PortalJump.ExitPosition,
                    stepResult.PortalJump.EntryTravelDirection, stepResult.PortalJump.ExitTravelDirection);
            }

            result.FinalDirection = stepResult.FinalDirection;
            if (stepResult.ShouldStop)
            {
                result.BlockReason = stepResult.BlockReason;
            }
        }

        private static void RemoveLastWaypoint(EscapeTraceResult result)
        {
            if (result == null || result.RouteWaypoints.Count <= 0) return;

            EscapeTraceWaypoint removedWaypoint = result.RouteWaypoints[result.RouteWaypoints.Count - 1];
            if (result.VisitedCells.Count > 0)
            {
                result.VisitedCells.RemoveAt(result.VisitedCells.Count - 1);
            }

            result.RouteWaypoints.RemoveAt(result.RouteWaypoints.Count - 1);
            result.DistanceBeforeStop -= Mathf.RoundToInt(removedWaypoint.StepCost);
        }
    }
}
