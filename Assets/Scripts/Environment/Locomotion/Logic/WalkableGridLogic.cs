using System;
using System.Collections.Generic;

namespace HighStakes.Environment
{
    /// <summary>A block of grid cells: its lower corner (<see cref="X"/>, <see cref="Z"/>) and a size in cells.</summary>
    public struct GridRect : IEquatable<GridRect>
    {
        public int X, Z, Width, Height;

        public GridRect(int x, int z, int width, int height)
        {
            X = x;
            Z = z;
            Width = width;
            Height = height;
        }

        public int Area => Width * Height;

        public bool Contains(int x, int z) => x >= X && x < X + Width && z >= Z && z < Z + Height;

        public bool Equals(GridRect other) => X == other.X && Z == other.Z && Width == other.Width && Height == other.Height;

        public override bool Equals(object obj) => obj is GridRect other && Equals(other);

        public override int GetHashCode() => (X * 397 ^ Z) * 397 ^ (Width * 397 ^ Height);

        public override string ToString() => $"({X}, {Z}) {Width}x{Height}";
    }

    /// <summary>
    /// Turns a grid of "the player fits here" cells into a handful of rectangles. <see cref="TeleportSurfaces"/> walks
    /// an area's floor on a half-metre grid and then builds one teleport target per rectangle instead of one per cell,
    /// so a room costs a few interactables rather than hundreds. No Unity types, so EditMode tests cover it.
    /// </summary>
    public static class WalkableGridLogic
    {
        /// <summary>The cell a world coordinate falls in, measured from the grid's lower corner.</summary>
        public static int CellOf(float world, float origin, float cellSize) =>
            (int)Math.Floor((world - origin) / cellSize);

        /// <summary>The middle of a cell, in the same coordinates <see cref="CellOf"/> takes.</summary>
        public static float CellCentre(int index, float origin, float cellSize) =>
            origin + (index + 0.5f) * cellSize;

        public static int Count(bool[,] walkable)
        {
            if (walkable == null) return 0;
            int count = 0;
            for (int x = 0; x < walkable.GetLength(0); x++)
                for (int z = 0; z < walkable.GetLength(1); z++)
                    if (walkable[x, z]) count++;
            return count;
        }

        /// <summary>
        /// Covers every walkable cell exactly once with axis-aligned rectangles, and no other cell. A greedy sweep:
        /// take the first free cell, run it as wide as the row allows, then as deep as rows of that full width allow.
        /// Not the theoretical minimum number of rectangles, but on room-shaped floors it gives a few per room.
        /// </summary>
        public static List<GridRect> Rectangles(bool[,] walkable)
        {
            var rects = new List<GridRect>();
            if (walkable == null) return rects;

            int width = walkable.GetLength(0), depth = walkable.GetLength(1);
            var taken = new bool[width, depth];
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!walkable[x, z] || taken[x, z]) continue;

                    int w = 1;
                    while (x + w < width && Free(walkable, taken, x + w, z)) w++;
                    int h = 1;
                    while (z + h < depth && RowFree(walkable, taken, x, z + h, w)) h++;

                    for (int dz = 0; dz < h; dz++)
                        for (int dx = 0; dx < w; dx++)
                            taken[x + dx, z + dz] = true;
                    rects.Add(new GridRect(x, z, w, h));
                }
            }
            return rects;
        }

        static bool Free(bool[,] walkable, bool[,] taken, int x, int z) => walkable[x, z] && !taken[x, z];

        static bool RowFree(bool[,] walkable, bool[,] taken, int x, int z, int width)
        {
            for (int dx = 0; dx < width; dx++)
                if (!Free(walkable, taken, x + dx, z)) return false;
            return true;
        }
    }
}
