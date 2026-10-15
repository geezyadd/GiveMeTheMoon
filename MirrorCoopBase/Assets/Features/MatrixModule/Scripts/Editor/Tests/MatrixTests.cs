using System.Collections.Generic;
using Features.MatrixModule.Scripts.Exceptions;
using NUnit.Framework;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Editor.Tests {
    public sealed class MatrixTests {
        private const int FREE = 0;
        private const int BLOCKED = 1;
        private const int FILL = 7;
        private const int SIZE = 3;

        private readonly MatrixFactory.MatrixFactory _matrixFactory = new();

        [Test]
        public void WhenMatrixIsCreated_ThenItHasTheGivenSizeAndFill() {
            IMatrix<int> matrix = _matrixFactory.Create(4, 2, FILL);

            Assert.AreEqual(4, matrix.GetColumnCount());
            Assert.AreEqual(2, matrix.GetRowCount());
            CollectionAssert.AreEqual(new[] { FILL, FILL, FILL, FILL, FILL, FILL, FILL, FILL }, matrix);
        }

        [Test]
        public void WhenMatrixIsCreatedFromSavedRows_ThenItReadsThoseRows() {
            List<MatrixSavableContainer<int>> rows = new() {
                new MatrixSavableContainer<int> { Data = new List<int> { 1, 2 } },
                new MatrixSavableContainer<int> { Data = new List<int> { 3, 4 } }
            };

            IMatrix<int> matrix = _matrixFactory.Create(rows);

            Assert.AreEqual(2, matrix.GetValue(1, 0));
            Assert.AreEqual(3, matrix.GetValue(0, 1));
        }

        [Test]
        public void WhenFillIsNotCloneableReference_ThenCreationThrows() =>
            Assert.Throws<NotValidMatrixEntityTypeException>(() => _matrixFactory.Create(SIZE, SIZE, new object()));

        [Test]
        public void WhenCellIsInTheCorner_ThenItHasTwoSideNeighbors() {
            IMatrix<int> matrix = _matrixFactory.Create(SIZE, SIZE, FREE);

            List<Vector2Int> neighbors = matrix.GetNeighbors(new Vector2Int(0, 0));

            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 1), new Vector2Int(1, 0) }, neighbors);
        }

        [Test]
        public void WhenDiagonalMoveIsAllowed_ThenInnerCellHasEightNeighbors() {
            IMatrix<int> matrix = _matrixFactory.Create(SIZE, SIZE, FREE);

            List<Vector2Int> neighbors = matrix.GetNeighbors(new Vector2Int(1, 1), true, IsFree);

            Assert.AreEqual(8, neighbors.Count);
        }

        [Test]
        public void WhenNeighborIsBlocked_ThenPredicateSkipsIt() {
            IMatrix<int> matrix = _matrixFactory.Create(SIZE, SIZE, FREE);
            matrix.SetValue(1, 0, BLOCKED);

            List<Vector2Int> neighbors = matrix.GetNeighbors(new Vector2Int(1, 1), false, IsFree);

            CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 2), new Vector2Int(0, 1), new Vector2Int(2, 1) }, neighbors);
        }

        [Test]
        public void WhenSideCellIsBlocked_ThenDiagonalPastItIsNotANeighbor() {
            IMatrix<int> matrix = _matrixFactory.Create(SIZE, SIZE, FREE);
            matrix.SetValue(1, 0, BLOCKED);

            List<Vector2Int> neighbors = matrix.GetNeighbors(new Vector2Int(0, 0), true, IsFree);

            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 1) }, neighbors);
        }

        private static bool IsFree(int cell) =>
            cell == FREE;
    }
}
