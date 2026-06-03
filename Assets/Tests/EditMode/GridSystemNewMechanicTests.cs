using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using NUnit.Framework;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class GridSystemNewMechanicTests
    {
        [Test]
        public void ResolveEndpointFromTap_ChoosesNearestEndpoint()
        {
            GridSystem grid = CreateGrid(8, 5,
                CreateTwoHeadArrow("A", "1", new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)));

            ArrowEndpoint leftEndpoint = grid.ResolveEndpointFromTap("A", new Vector2Int(2, 2));
            ArrowEndpoint rightEndpoint = grid.ResolveEndpointFromTap("A", new Vector2Int(4, 2));

            Assert.That(leftEndpoint.PathIndex, Is.EqualTo(0));
            Assert.That(leftEndpoint.ExitDirection, Is.EqualTo(Direction4.Left));
            Assert.That(rightEndpoint.PathIndex, Is.EqualTo(2));
            Assert.That(rightEndpoint.ExitDirection, Is.EqualTo(Direction4.Right));
        }

        [Test]
        public void TryMoveArrowGroup_UsesTappedEndpointForTwoHeadArrow()
        {
            GridSystem grid = CreateGrid(8, 5,
                CreateTwoHeadArrow("A", "1", new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)));

            ArrowActivationResult activation = grid.TryMoveArrowGroup("A", new Vector2Int(2, 2));

            Assert.That(activation, Is.Not.Null);
            Assert.That(activation.AllSucceeded, Is.True);
            Assert.That(activation.Entries.Count, Is.EqualTo(1));
            Assert.That(activation.Entries[0].Endpoint.PathIndex, Is.EqualTo(0));
            Assert.That(activation.Entries[0].TraceResult.FinalDirection, Is.EqualTo(Direction4.Left));
        }

        [Test]
        public void LinkedGroup_AllSucceeded_RemovesEveryArrowInTheGroup()
        {
            GridSystem grid = CreateGrid(8, 6,
                CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right, "L"),
                CreateArrow("B", new[] { new Vector2Int(1, 3), new Vector2Int(2, 3) }, Direction4.Right, "L"));

            grid.TryMoveArrow(2, 1);

            Assert.That(grid.RemainingArrows, Is.EqualTo(0));
            Assert.That(grid.GetArrowIdAt(1, 1), Is.EqualTo(string.Empty));
            Assert.That(grid.GetArrowIdAt(2, 1), Is.EqualTo(string.Empty));
            Assert.That(grid.GetArrowIdAt(1, 3), Is.EqualTo(string.Empty));
            Assert.That(grid.GetArrowIdAt(2, 3), Is.EqualTo(string.Empty));
        }

        [Test]
        public void LinkedGroup_BlockOnAnyMember_KeepsWholeGroupInPlace()
        {
            GridSystem grid = CreateGrid(8, 6,
                CreateArrow("A", new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) }, Direction4.Right, "L"),
                CreateArrow("B", new[] { new Vector2Int(1, 3), new Vector2Int(2, 3) }, Direction4.Right, "L"),
                CreateArrow("C", new[] { new Vector2Int(5, 3), new Vector2Int(6, 3) }, Direction4.Left, string.Empty));

            ArrowActivationResult preview = grid.TryMoveArrowGroup("A", new Vector2Int(2, 1));
            grid.TryMoveArrow(2, 1);

            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.AllSucceeded, Is.False);
            Assert.That(grid.RemainingArrows, Is.EqualTo(3));
            Assert.That(grid.GetArrowIdAt(1, 1), Is.EqualTo("A"));
            Assert.That(grid.GetArrowIdAt(2, 1), Is.EqualTo("A"));
            Assert.That(grid.GetArrowIdAt(1, 3), Is.EqualTo("B"));
            Assert.That(grid.GetArrowIdAt(2, 3), Is.EqualTo("B"));
        }

        [Test]
        public void MultiEndpointSharedPath_RequiresExactlyTwoEndpoints()
        {
            ArrowSaveData invalidArrow = new ArrowSaveData("A",
                new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, true,
                new List<ArrowEndpointSaveData>
                {
                    new ArrowEndpointSaveData(2, Direction4.Right, true)
                }, string.Empty, ArrowTopologyType.MultiEndpointSharedPath);

            bool isValid = ArrowModelFactory.TryCreate(invalidArrow, out _, out string error);

            Assert.That(isValid, Is.False);
            StringAssert.Contains("requires exactly 2 endpoints", error);
        }

        private static GridSystem CreateGrid(int width, int height, params ArrowSaveData[] arrows)
        {
            LevelSaveData data = new LevelSaveData("Test", width, height, LevelDifficulty.Normal)
            {
                Arrows = new List<ArrowSaveData>(arrows),
                SpecialCells = new List<SpecialCellSaveData>()
            };

            return new GridSystem(data);
        }

        private static ArrowSaveData CreateArrow(string id, IReadOnlyList<Vector2Int> path, Direction4 primaryDirection,
            string linkGroupId)
        {
            int primaryPathIndex = primaryDirection == Direction4.Left || primaryDirection == Direction4.Down ? 0 : path.Count - 1;
            List<ArrowEndpointSaveData> endpoints = new List<ArrowEndpointSaveData>
            {
                new ArrowEndpointSaveData(primaryPathIndex, primaryDirection, true)
            };

            return new ArrowSaveData(id, new List<Vector2Int>(path), primaryPathIndex == 0, endpoints,
                linkGroupId, ArrowTopologyType.SingleHeadSingleTail);
        }

        private static ArrowSaveData CreateTwoHeadArrow(string id, string linkGroupId, params Vector2Int[] path)
        {
            return new ArrowSaveData(id, new List<Vector2Int>(path), true,
                new List<ArrowEndpointSaveData>
                {
                    new ArrowEndpointSaveData(0, Direction4.Left, true),
                    new ArrowEndpointSaveData(path.Length - 1, Direction4.Right, false)
                }, linkGroupId, ArrowTopologyType.MultiEndpointSharedPath);
        }
    }
}
