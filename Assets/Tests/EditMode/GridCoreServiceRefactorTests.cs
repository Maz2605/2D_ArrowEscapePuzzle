using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Logic.SpecialCells;
using NUnit.Framework;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class GridCoreServiceRefactorTests
    {
        [Test]
        public void BoardInitializer_LoadsArrowsAndSpecialCellsIntoBoardState()
        {
            BoardState state = CreateState(6, 6,
                new[] { CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right) },
                new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(4, 1), BoardSpecialType.Redirect, Direction4.Up, string.Empty)
                });

            Assert.That(state.RemainingArrows, Is.EqualTo(1));
            Assert.That(state.GetArrowIdAt(1, 1), Is.EqualTo("A"));
            Assert.That(state.GetArrowIdAt(2, 1), Is.EqualTo("A"));
            Assert.That(state.GetSpecialCellAt(4, 1), Is.Not.Null);
            Assert.That(state.GetSpecialCellAt(4, 1).Type, Is.EqualTo(BoardSpecialType.Redirect));
        }

        [Test]
        public void ArrowActivationPlanner_ResolvesTappedEndpointWithoutGridSystemFacade()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateTwoHeadArrow("A", new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)) },
                new List<SpecialCellSaveData>());
            ArrowEscapeTracer tracer = new ArrowEscapeTracer(state);
            ArrowActivationPlanner planner = new ArrowActivationPlanner(state, tracer);

            ArrowActivationResult activation = planner.TryMoveArrowGroup("A", new Vector2Int(2, 2));

            Assert.That(activation, Is.Not.Null);
            Assert.That(activation.AllSucceeded, Is.True);
            Assert.That(activation.Entries.Count, Is.EqualTo(1));
            Assert.That(activation.Entries[0].Endpoint.PathIndex, Is.EqualTo(0));
            Assert.That(activation.Entries[0].TraceResult.FinalDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void ArrowEscapeTracer_UsesSpecialCellLogicWithoutGridSystemFacade()
        {
            BoardState state = CreateState(6, 6,
                new[] { CreateArrow("A", new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) }, Direction4.Right) },
                new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Redirect, Direction4.Up, string.Empty)
                });
            ArrowEscapeTracer tracer = new ArrowEscapeTracer(state);

            EscapeTraceResult trace = tracer.TraceEscapeRoute("A");

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4),
                new Vector2Int(2, 5)
            }));
        }

        [Test]
        public void BoardOutcomeProcessor_RemovesActivatedGroupAndClearsTraceCache()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right) },
                new List<SpecialCellSaveData>());
            ArrowEscapeTracer tracer = new ArrowEscapeTracer(state);
            ArrowActivationPlanner planner = new ArrowActivationPlanner(state, tracer);
            BoardOutcomeProcessor outcome = new BoardOutcomeProcessor(state);
            ArrowActivationResult activation = planner.TryMoveArrowGroup("A", new Vector2Int(2, 1));

            Assert.That(state.GetCachedTraceResult("A", activation.Entries[0].TraceResult.StartEndpointKey), Is.Not.Null);

            outcome.RemoveActivatedArrows(activation, ArrowGame.Data.Events.LogicGameEventID.ArrowEscaped);

            Assert.That(state.RemainingArrows, Is.EqualTo(0));
            Assert.That(state.GetArrowIdAt(1, 1), Is.EqualTo(string.Empty));
            Assert.That(state.GetArrowIdAt(2, 1), Is.EqualTo(string.Empty));
            Assert.That(state.GetCachedTraceResult("A", activation.Entries[0].TraceResult.StartEndpointKey), Is.Null);
        }

        [Test]
        public void RedirectLogic_ReturnsContinueWithExpectedDirectionAndNextPosition()
        {
            RedirectLogic logic = new RedirectLogic();
            SpecialCellSaveData redirect = new SpecialCellSaveData(new Vector2Int(2, 1),
                BoardSpecialType.Redirect, Direction4.Up, string.Empty);
            TraceContext context = CreateTraceContext(new Vector2Int(2, 1), Direction4.Right.ToVector2Int());

            SpecialCellStepResult result = logic.Evaluate(context, redirect);

            Assert.That(result.Handled, Is.True);
            Assert.That(result.ShouldStop, Is.False);
            Assert.That(result.NextDirection, Is.EqualTo(Vector2Int.up));
            Assert.That(result.NextPosition, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(result.FinalDirection, Is.EqualTo(Direction4.Up));
        }

        [Test]
        public void PortalLogic_ReturnsStopOnDirectionMismatch()
        {
            BoardState state = CreateState(6, 6, new ArrowSaveData[0],
                new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "P"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "P")
                });
            PortalLogic logic = new PortalLogic();
            SpecialCellSaveData portal = state.GetSpecialCellAt(2, 2);
            TraceContext context = CreateTraceContext(new Vector2Int(2, 2), Direction4.Up.ToVector2Int(), state);

            SpecialCellStepResult result = logic.Evaluate(context, portal);

            Assert.That(result.ShouldStop, Is.True);
            Assert.That(result.BlockReason, Is.EqualTo(EscapeBlockReason.PortalDirectionMismatch));
            Assert.That(result.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(result.PortalJump, Is.Null);
        }

        [Test]
        public void PortalLogic_ReturnsStopOnInvalidPair()
        {
            BoardState state = CreateState(6, 6, new ArrowSaveData[0],
                new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "P")
                });
            PortalLogic logic = new PortalLogic();
            SpecialCellSaveData portal = state.GetSpecialCellAt(2, 2);
            TraceContext context = CreateTraceContext(new Vector2Int(2, 2), Direction4.Down.ToVector2Int(), state);

            SpecialCellStepResult result = logic.Evaluate(context, portal);

            Assert.That(result.ShouldStop, Is.True);
            Assert.That(result.BlockReason, Is.EqualTo(EscapeBlockReason.InvalidPortal));
            Assert.That(result.FinalDirection, Is.EqualTo(Direction4.Down));
        }

        [Test]
        public void PortalLogic_ReturnsContinueWithPortalJumpOnValidPair()
        {
            BoardState state = CreateState(6, 6, new ArrowSaveData[0],
                new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "P"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "P")
                });
            PortalLogic logic = new PortalLogic();
            SpecialCellSaveData portal = state.GetSpecialCellAt(2, 2);
            TraceContext context = CreateTraceContext(new Vector2Int(2, 2), Direction4.Down.ToVector2Int(), state);

            SpecialCellStepResult result = logic.Evaluate(context, portal);

            Assert.That(result.ShouldStop, Is.False);
            Assert.That(result.NextDirection, Is.EqualTo(Vector2Int.left));
            Assert.That(result.NextPosition, Is.EqualTo(new Vector2Int(3, 4)));
            Assert.That(result.FinalDirection, Is.EqualTo(Direction4.Left));
            Assert.That(result.PortalJump, Is.Not.Null);
            Assert.That(result.PortalJump.EntryWaypointIndex, Is.EqualTo(0));
            Assert.That(result.PortalJump.EntryPosition, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(result.PortalJump.ExitPosition, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(result.PortalJump.EntryTravelDirection, Is.EqualTo(Direction4.Down));
            Assert.That(result.PortalJump.ExitTravelDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void CounterBlockLogic_ReturnsStopAndRequestsWaypointRemoval()
        {
            CounterBlockLogic logic = new CounterBlockLogic();
            SpecialCellSaveData counterBlock = new SpecialCellSaveData(new Vector2Int(2, 1),
                BoardSpecialType.CounterBlock, Direction4.Up, string.Empty);
            TraceContext context = CreateTraceContext(new Vector2Int(2, 1), Direction4.Right.ToVector2Int());

            SpecialCellStepResult result = logic.Evaluate(context, counterBlock);

            Assert.That(result.ShouldStop, Is.True);
            Assert.That(result.BlockReason, Is.EqualTo(EscapeBlockReason.CounterBlock));
            Assert.That(result.FinalDirection, Is.EqualTo(Direction4.Right));
            Assert.That(result.RemoveCurrentWaypoint, Is.True);
            Assert.That(context.Result.RouteWaypoints.Count, Is.EqualTo(1));
        }

        [Test]
        public void TwoHeadArrowMechanic_ChoosesNearestEndpoint()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateTwoHeadArrow("A", new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)) },
                new List<SpecialCellSaveData>());
            ArrowModel model = state.GetArrowModel("A");

            ArrowEndpoint endpoint = model.Mechanics.SelectEndpoint(model, new Vector2Int(4, 2));

            Assert.That(endpoint.PathIndex, Is.EqualTo(2));
            Assert.That(endpoint.ExitDirection, Is.EqualTo(Direction4.Right));
        }

        [Test]
        public void TwoHeadArrowMechanic_TieBreaksPrimaryEndpoint()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateTwoHeadArrow("A", new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)) },
                new List<SpecialCellSaveData>());
            ArrowModel model = state.GetArrowModel("A");

            ArrowEndpoint endpoint = model.Mechanics.SelectEndpoint(model, new Vector2Int(3, 2));

            Assert.That(endpoint.PathIndex, Is.EqualTo(0));
            Assert.That(endpoint.IsPrimary, Is.True);
        }

        [Test]
        public void ArrowMechanicSet_SoloArrowReturnsOnlyTrigger()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right) },
                new List<SpecialCellSaveData>());
            ArrowModel model = state.GetArrowModel("A");

            IReadOnlyList<ArrowModel> models = model.Mechanics.ResolveActivationModels(state, model);

            Assert.That(models.Count, Is.EqualTo(1));
            Assert.That(models[0].ArrowId, Is.EqualTo("A"));
        }

        [Test]
        public void LinkedArrowMechanic_ReturnsTriggerFirst()
        {
            BoardState state = CreateState(8, 5,
                new[]
                {
                    CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right, "L"),
                    CreateArrow("B", new[] { new Vector2Int(1, 3), new Vector2Int(2, 3) }, Direction4.Right, "L")
                },
                new List<SpecialCellSaveData>());
            ArrowModel trigger = state.GetArrowModel("B");

            IReadOnlyList<ArrowModel> models = trigger.Mechanics.ResolveActivationModels(state, trigger);

            Assert.That(models.Count, Is.EqualTo(2));
            Assert.That(models[0].ArrowId, Is.EqualTo("B"));
            Assert.That(models[1].ArrowId, Is.EqualTo("A"));
        }

        [Test]
        public void LinkedArrowMechanic_AllowsSameLinkGroupPassThrough()
        {
            BoardState state = CreateState(8, 5,
                new[]
                {
                    CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right, "L"),
                    CreateArrow("B", new[] { new Vector2Int(4, 1), new Vector2Int(5, 1) }, Direction4.Right, "L")
                },
                new List<SpecialCellSaveData>());
            ArrowModel moving = state.GetArrowModel("A");
            ArrowModel blocker = state.GetArrowModel("B");

            Assert.That(moving.Mechanics.CanPassThroughLinkedArrow(moving, blocker), Is.True);
        }

        [Test]
        public void ArrowEscapeTracer_BlocksSelfBodyBeforeTailVacates()
        {
            BoardState state = CreateState(8, 5,
                new[] { CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1) }, Direction4.Right) },
                new List<SpecialCellSaveData>());
            ArrowEscapeTracer tracer = new ArrowEscapeTracer(state);
            ArrowEndpoint endpointIntoOwnBody = new ArrowEndpoint(2, new Vector2Int(3, 1), Direction4.Left, true);
            EscapeTraceResult result = tracer.TraceEscapeRoute("A", endpointIntoOwnBody);

            Assert.That(result.CanEscape, Is.False);
            Assert.That(result.BlockReason, Is.EqualTo(EscapeBlockReason.OtherArrow));
            Assert.That(result.BlockerId, Is.EqualTo("A"));
        }

        private static BoardState CreateState(int width, int height, IEnumerable<ArrowSaveData> arrows,
            List<SpecialCellSaveData> specialCells)
        {
            LevelSaveData data = new LevelSaveData("Test", width, height, LevelDifficulty.Normal)
            {
                Arrows = new List<ArrowSaveData>(arrows),
                SpecialCells = specialCells
            };
            BoardState state = new BoardState(width, height);
            new BoardInitializer(state).Initialize(data);
            return state;
        }

        private static TraceContext CreateTraceContext(Vector2Int currentPosition, Vector2Int direction,
            BoardState state = null)
        {
            state ??= CreateState(6, 6, new ArrowSaveData[0], new List<SpecialCellSaveData>());
            EscapeTraceResult traceResult = new EscapeTraceResult("A", Direction4Extensions.FromVector(direction));
            int waypointIndex = traceResult.AddWaypoint(currentPosition, 1f);
            return new TraceContext("A", null, null, currentPosition, direction, waypointIndex, traceResult, state);
        }

        private static ArrowSaveData CreateArrow(string id, IReadOnlyList<Vector2Int> path, Direction4 primaryDirection,
            string linkGroupId = "")
        {
            int primaryPathIndex = primaryDirection == Direction4.Left || primaryDirection == Direction4.Down
                ? 0
                : path.Count - 1;
            List<ArrowEndpointSaveData> endpoints = new List<ArrowEndpointSaveData>
            {
                new ArrowEndpointSaveData(primaryPathIndex, primaryDirection, true)
            };

            return new ArrowSaveData(id, new List<Vector2Int>(path), primaryPathIndex == 0, endpoints,
                linkGroupId, ArrowTopologyType.SingleHeadSingleTail);
        }

        private static ArrowSaveData CreateTwoHeadArrow(string id, params Vector2Int[] path)
        {
            return new ArrowSaveData(id, new List<Vector2Int>(path), true,
                new List<ArrowEndpointSaveData>
                {
                    new ArrowEndpointSaveData(0, Direction4.Left, true),
                    new ArrowEndpointSaveData(path.Length - 1, Direction4.Right, false)
                }, string.Empty, ArrowTopologyType.MultiEndpointSharedPath);
        }
    }
}
