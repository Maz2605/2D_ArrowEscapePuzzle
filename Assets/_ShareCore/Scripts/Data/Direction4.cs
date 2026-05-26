using UnityEngine;

namespace ShareCore.Data
{
    public enum Direction4
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class Direction4Extensions
    {
        public static Vector2Int ToVector2Int(this Direction4 direction) => direction switch
        {
            Direction4.Up => Vector2Int.up,
            Direction4.Down => Vector2Int.down,
            Direction4.Left => Vector2Int.left,
            Direction4.Right => Vector2Int.right,
            _ => Vector2Int.up
        };

        public static Vector3 ToVector3(this Direction4 direction)
        {
            Vector2Int vector = direction.ToVector2Int();
            return new Vector3(vector.x, vector.y, 0f);
        }

        public static Direction4 FromVector(Vector2Int direction)
        {
            if (direction == Vector2Int.down) return Direction4.Down;
            if (direction == Vector2Int.left) return Direction4.Left;
            if (direction == Vector2Int.right) return Direction4.Right;
            return Direction4.Up;
        }

        public static Direction4 Opposite(this Direction4 direction) => direction switch
        {
            Direction4.Up => Direction4.Down,
            Direction4.Down => Direction4.Up,
            Direction4.Left => Direction4.Right,
            Direction4.Right => Direction4.Left,
            _ => Direction4.Up
        };

        public static string ToGlyph(this Direction4 direction) => direction switch
        {
            Direction4.Up => "\u2191",
            Direction4.Down => "\u2193",
            Direction4.Left => "\u2190",
            Direction4.Right => "\u2192",
            _ => "\u2191"
        };
    }
}
