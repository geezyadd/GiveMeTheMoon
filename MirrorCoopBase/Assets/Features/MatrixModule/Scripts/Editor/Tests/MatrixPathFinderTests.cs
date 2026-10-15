using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Editor.Tests {
    public sealed class MatrixPathFinderTests {
        private const int FREE = 0;
        private const int BLOCKED = 1;
        private const char BLOCKED_SYMBOL = '#';
        private const int SMALL_SIZE = 5;
        private const int OLD_ITERATION_LIMIT_SIZE = 20;
        private const int FIELD_SIZE = 40;
        private const double MAX_FIELD_PATH_MILLISECONDS = 50d;

        private readonly MatrixFactory.MatrixFactory _matrixFactory = new();

        [Test]
        public void WhenMatrixIsOpen_ThenPathIsStraightFromStartToEnd() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), new Vector2Int(4, 0), false, IsFree);

            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }, path);
        }

        [Test]
        public void WhenDiagonalMoveIsAllowed_ThenPathGoesAlongTheDiagonal() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), new Vector2Int(4, 4), true, IsFree);

            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(3, 3), new Vector2Int(4, 4) }, path);
        }

        [Test]
        public void WhenStartIsTheEnd_ThenPathIsThatCell() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(2, 2), new Vector2Int(2, 2), false, IsFree);

            CollectionAssert.AreEqual(new[] { new Vector2Int(2, 2) }, path);
        }

        [Test]
        public void WhenPathIsSearchedWithPredicate_ThenPathUsesOnlyMatchingCells() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);
            for (int x = 0; x < SMALL_SIZE; x++)
                matrix.SetValue(x, 0, BLOCKED);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 1), new Vector2Int(4, 1), false, IsFree);

            Assert.AreEqual(5, path.Count);
            AssertPathIsWalkable(matrix, path, false);
        }

        [Test]
        public void WhenWallStandsInTheWay_ThenShortestPathGoesAroundIt() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);
            for (int y = 0; y < 4; y++)
                matrix.SetValue(2, y, BLOCKED);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), new Vector2Int(4, 0), false, IsFree);

            Assert.AreEqual(13, path.Count);
            Assert.Contains(new Vector2Int(2, 4), path);
            AssertPathIsWalkable(matrix, path, false);
        }

        [Test]
        public void WhenCupFacesTheStart_ThenPathIsStillTheShortest() {
            IMatrix<int> matrix = CreateOpenMatrix(7);
            for (int x = 1; x <= 5; x++)
                matrix.SetValue(x, 4, BLOCKED);
            for (int y = 2; y <= 3; y++) {
                matrix.SetValue(1, y, BLOCKED);
                matrix.SetValue(5, y, BLOCKED);
            }

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(3, 0), new Vector2Int(3, 6), false, IsFree);

            Assert.AreEqual(13, path.Count);
            AssertPathIsWalkable(matrix, path, false);
        }

        [Test]
        public void WhenDetourLooksCloserToTheEnd_ThenShortestPathIsStillFound() {
            IMatrix<int> matrix = CreateMatrix(new[] {
                "#......",
                "##..#.#",
                "...#..#",
                "..#..#.",
                "#...#..",
                ".##....",
                "......."
            });

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 3), new Vector2Int(6, 3), false, IsFree);

            Assert.AreEqual(11, path.Count);
            AssertPathIsWalkable(matrix, path, false);
        }

        [Test]
        public void WhenWallClosesTheWay_ThenPathIsEmpty() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);
            for (int y = 0; y < SMALL_SIZE; y++)
                matrix.SetValue(2, y, BLOCKED);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), new Vector2Int(4, 0), true, IsFree);

            Assert.IsEmpty(path);
        }

        [Test]
        public void WhenEndIsOutsideTheMatrix_ThenPathIsEmpty() {
            IMatrix<int> matrix = CreateOpenMatrix(SMALL_SIZE);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), new Vector2Int(SMALL_SIZE, 0), false, IsFree);

            Assert.IsEmpty(path);
        }

        [Test]
        public void WhenPathWindsThrough20x20Maze_ThenWholePathIsFound() {
            IMatrix<int> matrix = CreateSerpentineMaze(OLD_ITERATION_LIMIT_SIZE);

            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), GetSerpentineMazeEnd(OLD_ITERATION_LIMIT_SIZE), false, IsFree);

            Assert.AreEqual(GetSerpentineMazePathLength(OLD_ITERATION_LIMIT_SIZE), path.Count);
            AssertPathIsWalkable(matrix, path, false);
        }

        [Test]
        public void WhenPathWindsThrough40x40Maze_ThenItIsFoundWithinTimeLimit() {
            IMatrix<int> matrix = CreateSerpentineMaze(FIELD_SIZE);
            Vector2Int end = GetSerpentineMazeEnd(FIELD_SIZE);
            matrix.PathFinding(new Vector2Int(0, 0), end, false, IsFree);

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), end, false, IsFree);
            stopwatch.Stop();

            TestContext.WriteLine($"40x40 maze path of {path.Count} cells: {stopwatch.Elapsed.TotalMilliseconds:F3} ms");
            Assert.AreEqual(GetSerpentineMazePathLength(FIELD_SIZE), path.Count);
            Assert.Less(stopwatch.Elapsed.TotalMilliseconds, MAX_FIELD_PATH_MILLISECONDS);
        }

        [Test]
        public void WhenNoPathExistsIn40x40Matrix_ThenEmptyPathIsReturnedWithinTimeLimit() {
            IMatrix<int> matrix = CreateOpenMatrix(FIELD_SIZE);
            for (int y = 0; y < FIELD_SIZE; y++)
                matrix.SetValue(FIELD_SIZE - 2, y, BLOCKED);
            Vector2Int end = new(FIELD_SIZE - 1, FIELD_SIZE - 1);
            matrix.PathFinding(new Vector2Int(0, 0), end, true, IsFree);

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Vector2Int> path = matrix.PathFinding(new Vector2Int(0, 0), end, true, IsFree);
            stopwatch.Stop();

            TestContext.WriteLine($"40x40 matrix without a path: {stopwatch.Elapsed.TotalMilliseconds:F3} ms");
            Assert.IsEmpty(path);
            Assert.Less(stopwatch.Elapsed.TotalMilliseconds, MAX_FIELD_PATH_MILLISECONDS);
        }

        private static bool IsFree(int cell) =>
            cell == FREE;

        private IMatrix<int> CreateOpenMatrix(int size) =>
            _matrixFactory.Create(size, size, FREE);

        private IMatrix<int> CreateMatrix(string[] rows) {
            IMatrix<int> matrix = _matrixFactory.Create(rows[0].Length, rows.Length, FREE);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    matrix.SetValue(x, y, rows[y][x] == BLOCKED_SYMBOL ? BLOCKED : FREE);

            return matrix;
        }

        // Free even columns joined by one gap in every odd wall column, alternately at the bottom and at the top:
        // the only path visits every free column from end to end.
        private IMatrix<int> CreateSerpentineMaze(int size) {
            IMatrix<int> matrix = CreateOpenMatrix(size);
            for (int x = 1; x < size; x += 2) {
                int gapY = x % 4 == 1 ? size - 1 : 0;
                for (int y = 0; y < size; y++)
                    matrix.SetValue(x, y, y == gapY ? FREE : BLOCKED);
            }

            return matrix;
        }

        private Vector2Int GetSerpentineMazeEnd(int size) =>
            new(size - 2, size / 2 % 2 == 0 ? 0 : size - 1);

        private int GetSerpentineMazePathLength(int size) =>
            size / 2 * size + size / 2 - 1;

        private void AssertPathIsWalkable(IMatrix<int> matrix, List<Vector2Int> path, bool isDiagonalMove) {
            for (int i = 0; i < path.Count; i++) {
                Assert.IsTrue(IsFree(matrix[path[i]]), $"Path cell {path[i]} is blocked.");
                if (i > 0)
                    Assert.Contains(path[i], matrix.GetNeighbors(path[i - 1], isDiagonalMove, IsFree));
            }
        }
    }
}
