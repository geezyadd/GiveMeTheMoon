using System;
using NUnit.Framework;

namespace Features.MatrixModule.Scripts.Editor.Tests {
    public sealed class MatrixExpandTests {
        private const int OLD = 1;
        private const int FILL = 2;
        private const int SIZE = 2;

        private readonly MatrixFactory.MatrixFactory _matrixFactory = new();

        private IMatrix<int> _matrix;

        [SetUp]
        public void SetUp() =>
            _matrix = _matrixFactory.Create(SIZE, SIZE, OLD);

        [Test]
        public void WhenMatrixExpandsRight_ThenNewColumnIsFilledAfterOldCells() {
            _matrix.ExpandMatrix(MatrixOperationDirection.Right, 1, FILL);

            CollectionAssert.AreEqual(new[] { OLD, OLD, FILL, OLD, OLD, FILL }, _matrix);
        }

        [Test]
        public void WhenMatrixExpandsLeft_ThenOldCellsMoveRight() {
            _matrix.ExpandMatrix(MatrixOperationDirection.Left, 1, FILL);

            CollectionAssert.AreEqual(new[] { FILL, OLD, OLD, FILL, OLD, OLD }, _matrix);
        }

        [Test]
        public void WhenMatrixExpandsToCenter_ThenItThrowsAndKeepsItsSize() {
            Assert.Throws<ArgumentOutOfRangeException>(() => _matrix.ExpandMatrix(MatrixOperationDirection.Center, 1, FILL));

            Assert.AreEqual(SIZE, _matrix.GetColumnCount());
            Assert.AreEqual(SIZE, _matrix.GetRowCount());
        }
    }
}
