using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    public readonly struct CounterBlockLineSegment
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int End;

        public CounterBlockLineSegment(Vector2Int start, Vector2Int end)
        {
            Start = start;
            End = end;
        }
    }

    public static class CounterBlockUtility
    {
        public static string GetRootKey(SpecialCellSaveData specialCell)
        {
            if (specialCell == null) return string.Empty;

            if (specialCell.Type == BoardSpecialType.CounterBlock)
            {
                if (!string.IsNullOrEmpty(specialCell.Id))
                    return $"CounterBlock:{specialCell.Id}";

                return $"CounterBlock:{specialCell.Position.x}:{specialCell.Position.y}";
            }

            return $"{specialCell.Type}:{specialCell.Position.x}:{specialCell.Position.y}:{specialCell.PortalId}";
        }

        public static List<Vector2Int> CloneOffsets(List<Vector2Int> offsets)
        {
            return offsets != null ? new List<Vector2Int>(offsets) : new List<Vector2Int>();
        }

        public static SpecialCellSaveData Clone(SpecialCellSaveData specialCell, string forcedId = null)
        {
            if (specialCell == null) return null;

            return new SpecialCellSaveData(
                specialCell.Position,
                specialCell.Type,
                specialCell.ExitDirection,
                specialCell.PortalId,
                specialCell.Counter,
                CloneOffsets(specialCell.OccupiedOffsets),
                forcedId ?? specialCell.Id);
        }

        public static List<Vector2Int> GetAllOffsets(SpecialCellSaveData specialCell)
        {
            List<Vector2Int> offsets = CloneOffsets(specialCell?.OccupiedOffsets);
            if (!offsets.Contains(Vector2Int.zero))
            {
                offsets.Insert(0, Vector2Int.zero);
            }

            return offsets;
        }

        public static IEnumerable<Vector2Int> GetOccupiedPositions(SpecialCellSaveData specialCell)
        {
            if (specialCell == null) yield break;

            List<Vector2Int> offsets = GetAllOffsets(specialCell);
            for (int i = 0; i < offsets.Count; i++)
            {
                yield return specialCell.Position + offsets[i];
            }
        }

        public static List<SpecialCellSaveData> GetUniqueRoots(IEnumerable<SpecialCellSaveData> specialCells)
        {
            List<SpecialCellSaveData> uniqueRoots = new List<SpecialCellSaveData>();
            if (specialCells == null) return uniqueRoots;

            HashSet<string> seenKeys = new HashSet<string>();
            foreach (SpecialCellSaveData specialCell in specialCells)
            {
                if (specialCell == null) continue;

                string rootKey = GetRootKey(specialCell);
                if (!seenKeys.Add(rootKey)) continue;

                uniqueRoots.Add(specialCell);
            }

            return uniqueRoots;
        }

        public static List<CounterBlockLineSegment> BuildCenterSegments(SpecialCellSaveData specialCell)
        {
            List<CounterBlockLineSegment> segments = new List<CounterBlockLineSegment>();
            if (specialCell == null) return segments;

            List<Vector2Int> allPoints = GetAllOffsets(specialCell);
            HashSet<string> seenSegments = new HashSet<string>();

            for (int i = 0; i < allPoints.Count; i++)
            {
                for (int j = i + 1; j < allPoints.Count; j++)
                {
                    if (Mathf.Abs(allPoints[i].x - allPoints[j].x) + Mathf.Abs(allPoints[i].y - allPoints[j].y) != 1)
                        continue;

                    Vector2Int a = allPoints[i];
                    Vector2Int b = allPoints[j];
                    if (a.x > b.x || (a.x == b.x && a.y > b.y))
                    {
                        (a, b) = (b, a);
                    }

                    string key = $"{a.x}:{a.y}:{b.x}:{b.y}";
                    if (!seenSegments.Add(key)) continue;

                    segments.Add(new CounterBlockLineSegment(a, b));
                }
            }

            if (segments.Count == 0)
            {
                segments.Add(new CounterBlockLineSegment(Vector2Int.zero, Vector2Int.zero));
            }

            return segments;
        }
    }
}
