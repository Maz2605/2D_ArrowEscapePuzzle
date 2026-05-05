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
        public void PortalTraversal_UsesExitDirectionOfDestinationPortal()
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
            Assert.That(trace.RouteWaypoints[1].IsTeleportExit, Is.True);
            Assert.That(trace.RouteWaypoints[1].StepCost, Is.EqualTo(0f));
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
                CreateArrow("A", false, new Vector2Int(0, 1), new Vector2Int(1, 1)),
                specialCells: new List<SpecialCellSaveData>
                {
                    new SpecialCellSaveData(new Vector2Int(2, 1), BoardSpecialType.Portal, Direction4.Up, "A"),
                    new SpecialCellSaveData(new Vector2Int(4, 4), BoardSpecialType.Portal, Direction4.Left, "A"),
                    new SpecialCellSaveData(new Vector2Int(3, 4), BoardSpecialType.Redirect, Direction4.Down)
                });

            EscapeTraceResult trace = grid.TraceEscapeRoute(grid.GetHeadOfGroup("A"));

            Assert.That(trace.CanEscape, Is.True);
            Assert.That(trace.FinalDirection, Is.EqualTo(Direction4.Down));
            Assert.That(trace.VisitedCells, Is.EqualTo(new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(4, 4),
                new Vector2Int(3, 4),
                new Vector2Int(3, 3),
                new Vector2Int(3, 2),
                new Vector2Int(3, 1),
                new Vector2Int(3, 0)
            }));
        }

        [Test]
        public void LoopingPortalConfiguration_IsBlocked()
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
            Assert.That(trace.BlockReason, Is.EqualTo(EscapeBlockReason.Loop));
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
