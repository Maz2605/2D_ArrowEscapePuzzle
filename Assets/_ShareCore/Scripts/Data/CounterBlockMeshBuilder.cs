using System.Collections.Generic;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    public static class CounterBlockMeshBuilder
    {
        private const int CornerSegments = 4;

        public static Mesh Build(SpecialCellSaveData specialCell, float cellSize, float cornerRadius, float padding = 0f)
        {
            Mesh mesh = new Mesh
            {
                name = $"CounterBlock_{specialCell?.Id ?? "Mesh"}"
            };

            if (specialCell == null) return mesh;

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>(CounterBlockUtility.GetAllOffsets(specialCell));
            if (occupied.Count == 0) return mesh;

            float halfCell = cellSize * 0.5f;
            float maxPadding = halfCell - 0.01f;
            padding = Mathf.Clamp(padding, 0f, maxPadding);
            float radius = Mathf.Clamp(cornerRadius, cellSize * 0.04f, halfCell - padding - 0.001f);

            List<Vector3> vertices = new List<Vector3>(occupied.Count * 32);
            List<int> triangles = new List<int>(occupied.Count * 48);

            foreach (Vector2Int offset in occupied)
            {
                AddCellGeometry(occupied, offset, halfCell, radius, padding, vertices, triangles);
            }

            if (vertices.Count == 0 || triangles.Count == 0) return mesh;

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, BuildUvs(vertices));
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void AddCellGeometry(HashSet<Vector2Int> occupied, Vector2Int offset, float halfCell, float radius, float padding,
            List<Vector3> vertices, List<int> triangles)
        {
            bool hasUp = occupied.Contains(offset + Vector2Int.up);
            bool hasDown = occupied.Contains(offset + Vector2Int.down);
            bool hasLeft = occupied.Contains(offset + Vector2Int.left);
            bool hasRight = occupied.Contains(offset + Vector2Int.right);

            bool exposeUp = !hasUp;
            bool exposeDown = !hasDown;
            bool exposeLeft = !hasLeft;
            bool exposeRight = !hasRight;

            float centerX = offset.x * halfCell * 2f;
            float centerY = offset.y * halfCell * 2f;

            float x0 = centerX - halfCell + (exposeLeft ? padding : 0f);
            float x1 = centerX + halfCell - (exposeRight ? padding : 0f);
            float y0 = centerY - halfCell + (exposeDown ? padding : 0f);
            float y1 = centerY + halfCell - (exposeUp ? padding : 0f);

            float innerMinX = x0 + (exposeLeft ? radius : 0f);
            float innerMaxX = x1 - (exposeRight ? radius : 0f);
            float innerMinY = y0 + (exposeDown ? radius : 0f);
            float innerMaxY = y1 - (exposeUp ? radius : 0f);

            AddRect(vertices, triangles, innerMinX, innerMinY, innerMaxX, innerMaxY);

            if (exposeUp) AddRect(vertices, triangles, innerMinX, innerMaxY, innerMaxX, y1);
            if (exposeDown) AddRect(vertices, triangles, innerMinX, y0, innerMaxX, innerMinY);
            if (exposeLeft) AddRect(vertices, triangles, x0, innerMinY, innerMinX, innerMaxY);
            if (exposeRight) AddRect(vertices, triangles, innerMaxX, innerMinY, x1, innerMaxY);

            if (exposeUp && exposeRight)
                AddCorner(vertices, triangles, new Vector2(x1 - radius, y1 - radius), radius, 0f, 90f);
            else if (exposeUp || exposeRight)
                AddRect(vertices, triangles, innerMaxX, innerMaxY, x1, y1);

            if (exposeUp && exposeLeft)
                AddCorner(vertices, triangles, new Vector2(x0 + radius, y1 - radius), radius, 90f, 180f);
            else if (exposeUp || exposeLeft)
                AddRect(vertices, triangles, x0, innerMaxY, innerMinX, y1);

            if (exposeDown && exposeLeft)
                AddCorner(vertices, triangles, new Vector2(x0 + radius, y0 + radius), radius, 180f, 270f);
            else if (exposeDown || exposeLeft)
                AddRect(vertices, triangles, x0, y0, innerMinX, innerMinY);

            if (exposeDown && exposeRight)
                AddCorner(vertices, triangles, new Vector2(x1 - radius, y0 + radius), radius, 270f, 360f);
            else if (exposeDown || exposeRight)
                AddRect(vertices, triangles, innerMaxX, y0, x1, innerMinY);
        }

        private static void AddRect(List<Vector3> vertices, List<int> triangles, float minX, float minY, float maxX, float maxY)
        {
            if (maxX - minX <= 0.0001f || maxY - minY <= 0.0001f) return;

            int start = vertices.Count;
            vertices.Add(new Vector3(minX, minY, 0f));
            vertices.Add(new Vector3(maxX, minY, 0f));
            vertices.Add(new Vector3(maxX, maxY, 0f));
            vertices.Add(new Vector3(minX, maxY, 0f));

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddCorner(List<Vector3> vertices, List<int> triangles, Vector2 center, float radius, float startAngleDeg, float endAngleDeg)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(center.x, center.y, 0f));

            for (int i = 0; i <= CornerSegments; i++)
            {
                float t = i / (float)CornerSegments;
                float angle = Mathf.Lerp(startAngleDeg, endAngleDeg, t) * Mathf.Deg2Rad;
                vertices.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f));
            }

            for (int i = 1; i <= CornerSegments; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        private static Vector2[] BuildUvs(List<Vector3> vertices)
        {
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            for (int i = 1; i < vertices.Count; i++) bounds.Encapsulate(vertices[i]);

            float width = Mathf.Max(bounds.size.x, 0.0001f);
            float height = Mathf.Max(bounds.size.y, 0.0001f);
            Vector2[] uvs = new Vector2[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                uvs[i] = new Vector2(
                    (vertices[i].x - bounds.min.x) / width,
                    (vertices[i].y - bounds.min.y) / height);
            }

            return uvs;
        }
    }
}
