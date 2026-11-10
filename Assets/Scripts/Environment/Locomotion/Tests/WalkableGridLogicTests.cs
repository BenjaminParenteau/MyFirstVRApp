using System.Collections.Generic;
using NUnit.Framework;

namespace HighStakes.Environment.Tests
{
    public class WalkableGridLogicTests
    {
        // "...#.." style maps, one string per row of cells along Z, '#' meaning the player fits there.
        static bool[,] Grid(params string[] rows)
        {
            int depth = rows.Length, width = rows[0].Length;
            var grid = new bool[width, depth];
            for (int z = 0; z < depth; z++)
            {
                Assert.AreEqual(width, rows[z].Length, "every row of the test map must be the same length");
                for (int x = 0; x < width; x++) grid[x, z] = rows[z][x] == '#';
            }
            return grid;
        }

        // The one guarantee the surfaces rely on: every walkable cell is covered once, nothing else is covered.
        static void AssertCoversExactly(bool[,] walkable, List<GridRect> rects)
        {
            int width = walkable.GetLength(0), depth = walkable.GetLength(1);
            var cover = new int[width, depth];
            foreach (var rect in rects)
            {
                Assert.Greater(rect.Width, 0);
                Assert.Greater(rect.Height, 0);
                for (int x = rect.X; x < rect.X + rect.Width; x++)
                    for (int z = rect.Z; z < rect.Z + rect.Height; z++)
                    {
                        Assert.IsTrue(x >= 0 && x < width && z >= 0 && z < depth, $"{rect} leaves the grid");
                        cover[x, z]++;
                    }
            }

            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    Assert.AreEqual(walkable[x, z] ? 1 : 0, cover[x, z], $"cell ({x}, {z}) covered {cover[x, z]} times");
        }

        [Test]
        public void NothingWalkableGivesNoRectangles()
        {
            var grid = Grid(
                "...",
                "...");
            Assert.IsEmpty(WalkableGridLogic.Rectangles(grid));
        }

        [Test]
        public void NullGridIsSafe()
        {
            Assert.IsEmpty(WalkableGridLogic.Rectangles(null));
            Assert.AreEqual(0, WalkableGridLogic.Count(null));
        }

        [Test]
        public void OneCell()
        {
            var grid = Grid(
                "...",
                ".#.",
                "...");
            var rects = WalkableGridLogic.Rectangles(grid);
            Assert.AreEqual(1, rects.Count);
            Assert.AreEqual(new GridRect(1, 1, 1, 1), rects[0]);
        }

        [Test]
        public void AFullRoomIsOneRectangle()
        {
            var grid = Grid(
                "####",
                "####",
                "####");
            var rects = WalkableGridLogic.Rectangles(grid);
            Assert.AreEqual(1, rects.Count);
            Assert.AreEqual(new GridRect(0, 0, 4, 3), rects[0]);
            AssertCoversExactly(grid, rects);
        }

        [Test]
        public void ARoomWithATableInItKeepsTheTableOut()
        {
            // A 5 x 5 room with a 1 x 1 table in the middle: the floor is still only a handful of rectangles.
            var grid = Grid(
                "#####",
                "#####",
                "##.##",
                "#####",
                "#####");
            var rects = WalkableGridLogic.Rectangles(grid);
            AssertCoversExactly(grid, rects);
            Assert.AreEqual(24, Total(rects));
            Assert.LessOrEqual(rects.Count, 4, "a room with one hole should not shatter into many surfaces");
        }

        [Test]
        public void AnLShapedRoomIsCoveredExactly()
        {
            var grid = Grid(
                "###..",
                "###..",
                "#####",
                "#####");
            var rects = WalkableGridLogic.Rectangles(grid);
            AssertCoversExactly(grid, rects);
            Assert.AreEqual(WalkableGridLogic.Count(grid), Total(rects));
        }

        [Test]
        public void ACorridorIntoARoomStaysAFewPieces()
        {
            // The VIP shape: a 2-cell-wide corridor opening into a wider lounge. The sweep runs the corridor's columns
            // all the way through the lounge and fills the lounge either side of them, which is three surfaces, not one
            // per cell.
            var grid = Grid(
                "..##..",
                "..##..",
                "######",
                "######",
                "######");
            var rects = WalkableGridLogic.Rectangles(grid);
            AssertCoversExactly(grid, rects);
            Assert.AreEqual(3, rects.Count);
            Assert.AreEqual(new GridRect(2, 0, 2, 5), rects[0]);
        }

        [Test]
        public void ScatteredCellsEachBecomeTheirOwnRectangle()
        {
            var grid = Grid(
                "#.#",
                ".#.",
                "#.#");
            var rects = WalkableGridLogic.Rectangles(grid);
            AssertCoversExactly(grid, rects);
            Assert.AreEqual(5, rects.Count);
        }

        [Test]
        public void TwoRoomsSeparatedByAWallStayApart()
        {
            var grid = Grid(
                "##.##",
                "##.##",
                "##.##");
            var rects = WalkableGridLogic.Rectangles(grid);
            AssertCoversExactly(grid, rects);
            Assert.AreEqual(2, rects.Count);
        }

        [Test]
        public void CellOfAndCellCentreAgree()
        {
            const float origin = -4f, size = 0.5f;
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(i, WalkableGridLogic.CellOf(WalkableGridLogic.CellCentre(i, origin, size), origin, size));
        }

        [Test]
        public void CellOfRoundsDownOnBothSidesOfTheOrigin()
        {
            const float origin = 0f, size = 0.5f;
            Assert.AreEqual(0, WalkableGridLogic.CellOf(0f, origin, size));
            Assert.AreEqual(0, WalkableGridLogic.CellOf(0.49f, origin, size));
            Assert.AreEqual(1, WalkableGridLogic.CellOf(0.5f, origin, size));
            Assert.AreEqual(-1, WalkableGridLogic.CellOf(-0.01f, origin, size));
            Assert.AreEqual(-2, WalkableGridLogic.CellOf(-0.6f, origin, size));
        }

        [Test]
        public void CountMatchesTheMap()
        {
            var grid = Grid(
                "#.#",
                "###");
            Assert.AreEqual(5, WalkableGridLogic.Count(grid));
        }

        [Test]
        public void RectangleHelpers()
        {
            var rect = new GridRect(2, 3, 4, 5);
            Assert.AreEqual(20, rect.Area);
            Assert.IsTrue(rect.Contains(2, 3));
            Assert.IsTrue(rect.Contains(5, 7));
            Assert.IsFalse(rect.Contains(6, 7));
            Assert.IsFalse(rect.Contains(1, 3));
        }

        static int Total(List<GridRect> rects)
        {
            int area = 0;
            foreach (var rect in rects) area += rect.Area;
            return area;
        }
    }
}
