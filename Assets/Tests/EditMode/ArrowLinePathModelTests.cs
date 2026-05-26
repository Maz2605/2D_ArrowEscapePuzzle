using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Visual;
using NUnit.Framework;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Tests.EditMode
{
    public class ArrowLinePathModelTests
    {
        [Test]
        public void Build_NoPortal_CreatesSingleContinuousSegment()
        {
            ArrowLinePathModel model = new ArrowLinePathModel();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            List<EscapeTraceWaypoint> waypoints = new List<EscapeTraceWaypoint>
            {
                new EscapeTraceWaypoint(new Vector2Int(2, 0), 1f),
                new EscapeTraceWaypoint(new Vector2Int(3, 0), 1f)
            };

            model.Build(bodyPoints, waypoints, null, GridToLocalPoint, 1f);

            Assert.That(model.SegmentCount, Is.EqualTo(1));
            ArrowLinePathModel.Segment segment = model.GetSegment(0);
            Assert.That(segment.StartPointIndex, Is.EqualTo(0));
            Assert.That(segment.EndPointIndex, Is.EqualTo(3));
            Assert.That(segment.StartDistance, Is.EqualTo(0f));
            Assert.That(segment.EndDistance, Is.EqualTo(3f));
        }

        [Test]
        public void Build_OnePortal_SplitsEntryAndExitIntoSeparateSegments()
        {
            ArrowLinePathModel model = new ArrowLinePathModel();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            List<EscapeTraceWaypoint> waypoints = new List<EscapeTraceWaypoint>
            {
                new EscapeTraceWaypoint(new Vector2Int(2, 0), 1f),
                new EscapeTraceWaypoint(new Vector2Int(0, 2), 0f, true),
                new EscapeTraceWaypoint(new Vector2Int(0, 3), 1f)
            };
            List<EscapeTracePortalJump> portalJumps = new List<EscapeTracePortalJump>
            {
                new EscapeTracePortalJump(0, 1, new Vector2Int(2, 0), new Vector2Int(0, 2),
                    Direction4.Right, Direction4.Up)
            };

            model.Build(bodyPoints, waypoints, portalJumps, GridToLocalPoint, 1f);

            Assert.That(model.SegmentCount, Is.EqualTo(2));
            Assert.That(model.GetSegment(0).StartPointIndex, Is.EqualTo(0));
            Assert.That(model.GetSegment(0).EndPointIndex, Is.EqualTo(2));
            Assert.That(model.GetSegment(1).StartPointIndex, Is.EqualTo(3));
            Assert.That(model.GetSegment(1).EndPointIndex, Is.EqualTo(4));
            Assert.That(model.MovementDistances[2], Is.EqualTo(model.MovementDistances[3]));
            Assert.That(model.MovementPoints[2], Is.Not.EqualTo(model.MovementPoints[3]));
        }

        [Test]
        public void Build_MultiplePortals_CreatesSegmentPerContinuousPathPart()
        {
            ArrowLinePathModel model = new ArrowLinePathModel();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            List<EscapeTraceWaypoint> waypoints = new List<EscapeTraceWaypoint>
            {
                new EscapeTraceWaypoint(new Vector2Int(2, 0), 1f),
                new EscapeTraceWaypoint(new Vector2Int(0, 2), 0f, true),
                new EscapeTraceWaypoint(new Vector2Int(0, 3), 1f),
                new EscapeTraceWaypoint(new Vector2Int(3, 3), 0f, true),
                new EscapeTraceWaypoint(new Vector2Int(4, 3), 1f)
            };
            List<EscapeTracePortalJump> portalJumps = new List<EscapeTracePortalJump>
            {
                new EscapeTracePortalJump(0, 1, new Vector2Int(2, 0), new Vector2Int(0, 2),
                    Direction4.Right, Direction4.Up),
                new EscapeTracePortalJump(2, 3, new Vector2Int(0, 3), new Vector2Int(3, 3),
                    Direction4.Up, Direction4.Right)
            };

            model.Build(bodyPoints, waypoints, portalJumps, GridToLocalPoint, 1f);

            Assert.That(model.SegmentCount, Is.EqualTo(3));
            for (int i = 0; i < model.SegmentCount; i++)
            {
                ArrowLinePathModel.Segment segment = model.GetSegment(i);
                for (int pointIndex = segment.StartPointIndex + 1; pointIndex <= segment.EndPointIndex; pointIndex++)
                {
                    bool sameDistance = Mathf.Abs(model.MovementDistances[pointIndex] -
                                                  model.MovementDistances[pointIndex - 1]) <= ArrowLinePathModel.RenderEpsilon;
                    bool changedPosition = (model.MovementPoints[pointIndex] -
                                            model.MovementPoints[pointIndex - 1]).sqrMagnitude > 0.0001f;

                    Assert.That(sameDistance && changedPosition, Is.False);
                }
            }
        }

        [Test]
        public void BuildVisibleBodyChunks_PutsHeadSegmentFirstAndSkipsZeroLengthChunks()
        {
            ArrowLinePathModel model = new ArrowLinePathModel();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            List<EscapeTraceWaypoint> waypoints = new List<EscapeTraceWaypoint>
            {
                new EscapeTraceWaypoint(new Vector2Int(2, 0), 1f),
                new EscapeTraceWaypoint(new Vector2Int(0, 2), 0f, true),
                new EscapeTraceWaypoint(new Vector2Int(0, 3), 1f)
            };
            List<EscapeTracePortalJump> portalJumps = new List<EscapeTracePortalJump>
            {
                new EscapeTracePortalJump(0, 1, new Vector2Int(2, 0), new Vector2Int(0, 2),
                    Direction4.Right, Direction4.Up)
            };

            model.Build(bodyPoints, waypoints, portalJumps, GridToLocalPoint, 1f);
            model.BuildVisibleBodyChunks(1.5f, 2.5f);

            Assert.That(model.VisibleBodyChunkCount, Is.EqualTo(2));
            Assert.That(model.GetVisibleBodyChunk(0).SegmentIndex, Is.EqualTo(1));
            Assert.That(model.GetVisibleBodyChunk(0).ContainsHead, Is.True);
            Assert.That(model.GetVisibleBodyChunk(1).SegmentIndex, Is.EqualTo(0));

            model.BuildVisibleBodyChunks(2f, 2f);
            Assert.That(model.VisibleBodyChunkCount, Is.EqualTo(0));
        }

        private static Vector3 GridToLocalPoint(Vector2Int position)
        {
            return new Vector3(position.x, position.y, 0f);
        }
    }
}
