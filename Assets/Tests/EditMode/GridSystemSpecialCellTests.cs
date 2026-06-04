using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using NUnit.Framework;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class GridSystemSpecialCellTests
    {
        [Test]
        public void StraightEscapeWithoutSpecialCells_KeepsLegacyBehavior()
        {
            GridSystem grid = CreateGrid(5, 5,
                CreateArrow("A", false, new Vector2Int(0, 2), new Vector2Int(1, 2)));

            ArrowData head = grid.GetHeadOfGroup("A");
            EscapeTraceResult trace = grid.TraceEscapeRoute(head);

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Right));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(3));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(4, 2)
            }));
        }

        [Test]
        public void PortalTraversal_RequiresMatchingTravelDirectionAndUsesDestinationPortalDirection()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(2, 4), new Vector2Int(2, 3)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "X"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "X")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Left));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(5));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2),
                new Vector2Int(4, 4),
                new Vector2Int(3, 4),
                new Vector2Int(2, 4),
                new Vector2Int(1, 4),
                new Vector2Int(0, 4)
            }));
            Assert.That(trace.RouteWaypoints[1].IsTeleportExit, Is.True);
            Assert.That(trace.RouteWaypoints[1].StepCost, Is.EqualTo(0f));
            Assert.That(trace.PortalJumps.Count, Is.EqualTo(1));
            Assert.That(trace.PortalJumps[0].EntryWaypointIndex, Is.EqualTo(0));
            Assert.That(trace.PortalJumps[0].ExitWaypointIndex, Is.EqualTo(1));
            Assert.That(trace.PortalJumps[0].EntryPosition, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(trace.PortalJumps[0].ExitPosition, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(trace.PortalJumps[0].EntryTravelDirection, Is.EqualTo(Direction4.Down));
            Assert.That(trace.PortalJumps[0].ExitTravelDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void MultiplePortalTraversal_RecordsEachPortalJump()
        {
            GridSystem grid = CreateGrid(7, 6,
                CreateArrow("A", false, new Vector2Int(2, 4), new Vector2Int(2, 3)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "A"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Right, "A"),
                    new SpecialCellSaveData(new Vector2Int(5, 4), BoardSpecialType.Portal, Direction4.Left, "B"),
                    new SpecialCellSaveData(new Vector2Int(5, 1), BoardSpecialType.Portal, Direction4.Left, "B")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Left));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(7));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2),
                new Vector2Int(4, 4),
                new Vector2Int(5, 4),
                new Vector2Int(5, 1),
                new Vector2Int(4, 1),
                new Vector2Int(3, 1),
                new Vector2Int(2, 1),
                new Vector2Int(1, 1),
                new Vector2Int(0, 1)
            }));

            Assert.That(trace.PortalJumps.Count, Is.EqualTo(2));
            Assert.That(trace.PortalJumps[0].EntryWaypointIndex, Is.EqualTo(0));
            Assert.That(trace.PortalJumps[0].ExitWaypointIndex, Is.EqualTo(1));
            Assert.That(trace.PortalJumps[0].EntryPosition, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(trace.PortalJumps[0].ExitPosition, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(trace.PortalJumps[0].EntryTravelDirection, Is.EqualTo(Direction4.Down));
            Assert.That(trace.PortalJumps[0].ExitTravelDirection, Is.EqualTo(Direction4.Right));
            Assert.That(trace.PortalJumps[1].EntryWaypointIndex, Is.EqualTo(2));
            Assert.That(trace.PortalJumps[1].ExitWaypointIndex, Is.EqualTo(3));
            Assert.That(trace.PortalJumps[1].EntryPosition, Is.EqualTo(new Vector2Int(5, 4)));
            Assert.That(trace.PortalJumps[1].ExitPosition, Is.EqualTo(new Vector2Int(5, 1)));
            Assert.That(trace.PortalJumps[1].EntryTravelDirection, Is.EqualTo(Direction4.Right));
            Assert.That(trace.PortalJumps[1].ExitTravelDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void PortalEntryFromSideDirection_CanTraverse()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(0, 1), new Vector2Int(1, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Portal, Direction4.Up, "X"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "X")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Left));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(5));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(4, 4),
                new Vector2Int(3, 4),
                new Vector2Int(2, 4),
                new Vector2Int(1, 4),
                new Vector2Int(0, 4)
            }));
            Assert.That(trace.PortalJumps.Count, Is.EqualTo(1));
            Assert.That(trace.PortalJumps[0].EntryTravelDirection, Is.EqualTo(Direction4.Right));
            Assert.That(trace.PortalJumps[0].ExitTravelDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void PortalEntryFromOtherSideDirection_CanTraverse()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(4, 1), new Vector2Int(3, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Portal, Direction4.Up, "X"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "X")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Left));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(4, 4),
                new Vector2Int(3, 4),
                new Vector2Int(2, 4),
                new Vector2Int(1, 4),
                new Vector2Int(0, 4)
            }));
            Assert.That(trace.PortalJumps.Count, Is.EqualTo(1));
            Assert.That(trace.PortalJumps[0].EntryTravelDirection, Is.EqualTo(Direction4.Left));
            Assert.That(trace.PortalJumps[0].ExitTravelDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void PortalEntryFromExitFace_IsBlockedAtPortal()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(2, 0), new Vector2Int(2, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "X"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "X")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.False);
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.PortalDirectionMismatch));
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(1));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2)
            }));
        }

        [Test]
        public void RedirectCell_ForcesConfiguredDirection()
        {
            GridSystem grid = CreateGrid(5, 5,
                CreateArrow("A", false, new Vector2Int(0, 1), new Vector2Int(1, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Redirect, Direction4.Up)
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4)
            }));
        }

        [Test]
        public void PortalThenRedirect_CanChainBeforeEscaping()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(2, 4), new Vector2Int(2, 3)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "A"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "A"),
                    new SpecialCellSaveData(new Vector2Int(3, 4), BoardSpecialType.Redirect, Direction4.Down)
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Down));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2),
                new Vector2Int(4, 4),
                new Vector2Int(3, 4),
                new Vector2Int(3, 3),
                new Vector2Int(3, 2),
                new Vector2Int(3, 1),
                new Vector2Int(3, 0)
            }));
        }

        [Test]
        public void PortalReentryAgainstRequiredTravelDirection_IsBlockedBeforeLooping()
        {
            GridSystem grid = CreateGrid(6, 4,
                CreateArrow("A", false, new Vector2Int(0, 1), new Vector2Int(1, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Portal, Direction4.Right, "Loop"),
                    new SpecialCellSaveData(new Vector2Int(4, 1), BoardSpecialType.Portal, Direction4.Left, "Loop")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.False);
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.PortalDirectionMismatch));
        }

        [Test]
        public void SameDirectionPortalPair_AllowsBidirectionalTravelWhenEachPortalIsEnteredCorrectly()
        {
            List<SpecialCellSaveData> portals = new List<SpecialCellSaveData>
            {
                new SpecialCellSaveData(new Vector2Int(1, 2), BoardSpecialType.Portal, Direction4.Up, "UP"),
                new SpecialCellSaveData(new Vector2Int(4, 2), BoardSpecialType.Portal, Direction4.Up, "UP")
            };

            GridSystem gridFromA = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(1, 4), new Vector2Int(1, 3)),
                portals);
            GridSystem gridFromB = CreateGrid(6, 6,
                CreateArrow("B", false, new Vector2Int(4, 4), new Vector2Int(4, 3)),
                portals);

            EscapeTraceResult traceFromA = gridFromA.TraceEscapeRoute(gridFromA.GetHeadOfGroup("A"));
            EscapeTraceResult traceFromB = gridFromB.TraceEscapeRoute(gridFromB.GetHeadOfGroup("B"));

            Assert.That(traceFromA.CanEscape, Is.True);
            Assert.That(traceFromA.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(traceFromA.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(4, 2),
                new Vector2Int(4, 3),
                new Vector2Int(4, 4),
                new Vector2Int(4, 5)
            }));

            Assert.That(traceFromB.CanEscape, Is.True);
            Assert.That(traceFromB.FinalDirection, Is.EqualTo(Direction4.Up));
            Assert.That(traceFromB.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(4, 2),
                new Vector2Int(1, 2),
                new Vector2Int(1, 3),
                new Vector2Int(1, 4),
                new Vector2Int(1, 5)
            }));
        }

        [Test]
        public void PortalWithoutValidPair_ReturnsInvalidPortal()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(2, 4), new Vector2Int(2, 3)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 2), BoardSpecialType.Portal, Direction4.Up, "Solo")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.False);
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.InvalidPortal));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2)
            }));
        }

        [Test]
        public void PortalExitIntoOwnBodyBeforeTailVacates_IsBlocked()
        {
            GridSystem grid = CreateGrid(5, 5,
                CreateArrow("A", false,
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(3, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, 2)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(3, 2), BoardSpecialType.Portal, Direction4.Left, "Spiral"),
                    new SpecialCellSaveData(new Vector2Int(0, 3), BoardSpecialType.Portal, Direction4.Down, "Spiral")
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.False);
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.OtherArrow));
            Assert.That(trace.BlockerId, Is.EqualTo("A"));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(1));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(3, 2),
                new Vector2Int(0, 3)
            }));
        }

        [Test]
        public void ObstacleAfterRedirect_IsBlockedByOtherArrow()
        {
            GridSystem grid = CreateGrid(6, 6,
                CreateArrow("A", false, new Vector2Int(0, 1), new Vector2Int(1, 1)),
                CreateArrow("B", false, new Vector2Int(2, 3), new Vector2Int(2, 4)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Redirect, Direction4.Up)
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.False);
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.OtherArrow));
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(2));
        }

        [Test]
        public void NullSpecialCellList_DoesNotBreakTrace()
        {
            LevelSaveData data = new LevelSaveData("Legacy", 5, 5, LevelDifficulty.Normal)
            {
                Arrows = new List<ArrowSaveData>
                {
                    CreateArrow("A", false, new Vector2Int(0, 2), new Vector2Int(1, 2))
                },
                SpecialCells = null
            };

            GridSystem grid = new GridSystem(data);
            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.DistanceBeforeStop, Is.EqualTo(3));
        }

        private static GridSystem CreateGrid(int width, int height, ArrowSaveData arrow,
            List<SpecialCellSaveData> specialCells = null)
        {
            return CreateGrid(width, height, new[] { arrow }, specialCells);
        }

        private static GridSystem CreateGrid(int width, int height, ArrowSaveData arrowA, ArrowSaveData arrowB,
            List<SpecialCellSaveData> specialCells = null)
        {
            return CreateGrid(width, height, new[] { arrowA, arrowB }, specialCells);
        }

        private static GridSystem CreateGrid(int width, int height, IEnumerable<ArrowSaveData> arrows,
            List<SpecialCellSaveData> specialCells = null)
        {
            LevelSaveData data = new LevelSaveData("Test", width, height, LevelDifficulty.Normal)
            {
                Arrows = new List<ArrowSaveData>(arrows),
                SpecialCells = specialCells
            };

            return new GridSystem(data);
        }

        private static ArrowSaveData CreateArrow(string id, bool isHeadFirst, params Vector2Int[] path)
        {
            return new ArrowSaveData(id, new List<Vector2Int>(path), isHeadFirst);
        }
    }
}
