using System.Collections.Generic;
using ArrowGame.Gameplay.Visual.GridComponents;
using NUnit.Framework;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class GridViewRefactorHelpersTests
    {
        [Test]
        public void CalculateStaggerBatchSize_ReturnsOneWhenItemsFitMaxDuration()
        {
            int batchSize = GridAnimator.CalculateStaggerBatchSize(5, 0.6f, 0.05f, 2f);

            Assert.That(batchSize, Is.EqualTo(1));
        }

        [Test]
        public void CalculateStaggerBatchSize_SplitsIntoBatchesWhenSingleSpawnWouldExceedMaxDuration()
        {
            int batchSize = GridAnimator.CalculateStaggerBatchSize(20, 0.6f, 0.05f, 1.5f);

            Assert.That(batchSize, Is.GreaterThan(1));
        }

        [Test]
        public void GetSpecialCellFootprintPositions_ReturnsUniquePositionsSortedTopDownThenLeftRight()
        {
            SpecialCellSaveData specialCell = new SpecialCellSaveData(
                new Vector2Int(2, 1),
                BoardSpecialType.CounterBlock,
                Direction4.Up,
                counter: 3,
                occupiedOffsets: new List<Vector2Int>
                {
                    Vector2Int.zero,
                    Vector2Int.up,
                    Vector2Int.right,
                    Vector2Int.up,
                    Vector2Int.right
                });

            List<Vector2Int> footprint = SpecialCellVisuals.GetSpecialCellFootprintPositions(specialCell);

            Assert.That(footprint, Is.EqualTo(new[]
            {
                new Vector2Int(2, 2),
                new Vector2Int(2, 1),
                new Vector2Int(3, 1)
            }));
        }
    }
}
