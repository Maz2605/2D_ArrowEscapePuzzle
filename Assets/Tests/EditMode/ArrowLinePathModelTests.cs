using System.Collections.Generic;
using ArrowGame.Data.Events;
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

        [Test]
        public void RebuildPath_PortalEntry_DoesNotDispatchDuplicateNormalTrigger()
        {
            ArrowLinePathPresenter presenter = new ArrowLinePathPresenter();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            EscapeTraceResult trace = new EscapeTraceResult("A", Direction4.Up);
            trace.AddWaypoint(new Vector2Int(2, 0), 1f);
            trace.AddWaypoint(new Vector2Int(0, 2), 0f, true);
            trace.AddPortalJump(0, 1, new Vector2Int(2, 0), new Vector2Int(0, 2),
                Direction4.Right, Direction4.Up);

            presenter.RebuildPath(bodyPoints, trace, GridToLocalPoint, 1f, new Vector2Int(1, 0));

            List<ArrowPathVisualTrigger> triggers = new List<ArrowPathVisualTrigger>();
            presenter.DispatchReachedTriggers(2f, trigger => triggers.Add(trigger));

            Assert.That(triggers.Count, Is.EqualTo(2));
            Assert.That(triggers[0].TriggerType, Is.EqualTo(ArrowPathVisualTriggerType.PortalEntry));
            Assert.That(triggers[1].TriggerType, Is.EqualTo(ArrowPathVisualTriggerType.PortalExit));
        }

        [Test]
        public void BuildVisibleSegmentPoints_PortalBoundaryGap_TrimsEntryAndExitEdges()
        {
            ArrowLinePathPresenter presenter = new ArrowLinePathPresenter();
            Vector3[] bodyPoints =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f)
            };
            EscapeTraceResult trace = new EscapeTraceResult("A", Direction4.Up);
            trace.AddWaypoint(new Vector2Int(2, 0), 1f);
            trace.AddWaypoint(new Vector2Int(0, 2), 0f, true);
            trace.AddWaypoint(new Vector2Int(0, 3), 1f);
            trace.AddPortalJump(0, 1, new Vector2Int(2, 0), new Vector2Int(0, 2),
                Direction4.Right, Direction4.Up);
            presenter.RebuildPath(bodyPoints, trace, GridToLocalPoint, 1f, new Vector2Int(1, 0));

            List<ArrowLinePathModel.VisibleBodyChunk> chunks = new List<ArrowLinePathModel.VisibleBodyChunk>();
            List<Vector3> rawPoints = new List<Vector3>();
            List<Vector3> finalPoints = new List<Vector3>();
            presenter.BuildVisibleBodyChunks(0.5f, 2.5f, chunks);

            ArrowLinePathModel.VisibleBodyChunk entryChunk = FindChunk(chunks, 0);
            Assert.That(presenter.BuildVisibleSegmentPoints(entryChunk, Vector3.up, rawPoints, finalPoints,
                0.02f, 0.99f, 0.2f), Is.True);
            Assert.That(Vector3.Distance(finalPoints[finalPoints.Count - 1], new Vector3(1.8f, 0f, 0f)),
                Is.LessThan(0.001f));

            ArrowLinePathModel.VisibleBodyChunk exitChunk = FindChunk(chunks, 1);
            Assert.That(presenter.BuildVisibleSegmentPoints(exitChunk, Vector3.up, rawPoints, finalPoints,
                0.02f, 0.99f, 0.2f), Is.True);
            Assert.That(Vector3.Distance(finalPoints[0], new Vector3(0f, 2.2f, 0f)), Is.LessThan(0.001f));
        }

        private static Vector3 GridToLocalPoint(Vector2Int position)
        {
            return new Vector3(position.x, position.y, 0f);
        }

        private static ArrowLinePathModel.VisibleBodyChunk FindChunk(
            IReadOnlyList<ArrowLinePathModel.VisibleBodyChunk> chunks, int segmentIndex)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].SegmentIndex == segmentIndex)
                {
                    return chunks[i];
                }
            }

            Assert.Fail($"Missing visible chunk for segment {segmentIndex}.");
            return default;
        }
    }
}
