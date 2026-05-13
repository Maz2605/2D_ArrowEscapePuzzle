using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using NUnit.Framework;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class GridSystemPrimaryEndpointQueryTests
    {
        [Test]
        public void GetPrimaryExitDirection_UsesLegacyPrimaryEndpoint()
        {
            GridSystem grid = CreateGrid(5, 5,
                new ArrowSaveData("A", new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1) }, false));

            Direction4? direction = grid.GetPrimaryExitDirection("A");

            Assert.That(direction, Is.EqualTo(Direction4.Right));
        }

        [Test]
        public void GetPrimaryExitDirection_UsesExplicitEndpointData()
        {
            ArrowSaveData arrow = new ArrowSaveData("A",
                new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, true,
                new List<ArrowEndpointSaveData>
                {
                    new ArrowEndpointSaveData(2, Direction4.Right, true)
                }, string.Empty, ArrowTopologyType.SingleHeadSingleTail);

            GridSystem grid = CreateGrid(5, 5, arrow);

            Direction4? direction = grid.GetPrimaryExitDirection("A");

            Assert.That(direction, Is.EqualTo(Direction4.Right));
        }

        [Test]
        public void GetArrowIdsByPrimaryDirection_MatchesLegacyHeadTypeWrapper()
        {
            GridSystem grid = CreateGrid(6, 6,
                new[]
                {
                    new ArrowSaveData("A", new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1) }, false),
                    new ArrowSaveData("B", new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2) }, false),
                    new ArrowSaveData("C", new List<Vector2Int> { new Vector2Int(4, 4), new Vector2Int(3, 4) }, false)
                });

            List<string> directionIds = grid.GetArrowIdsByPrimaryDirection(Direction4.Right);
            List<string> legacyIds = grid.GetAllArrowIdsByType(CellType.ArrowHeadRight, string.Empty);

            CollectionAssert.AreEquivalent(legacyIds, directionIds);
            CollectionAssert.DoesNotContain(directionIds, "C");
        }

        private static GridSystem CreateGrid(int width, int height, ArrowSaveData arrow)
        {
            return CreateGrid(width, height, new[] { arrow });
        }

        private static GridSystem CreateGrid(int width, int height, IEnumerable<ArrowSaveData> arrows)
        {
            LevelSaveData data = new LevelSaveData("Test", width, height, LevelDifficulty.Normal)
            {
                Arrows = new List<ArrowSaveData>(arrows),
                SpecialCells = new List<SpecialCellSaveData>()
            };

            return new GridSystem(data);
        }
    }
}
