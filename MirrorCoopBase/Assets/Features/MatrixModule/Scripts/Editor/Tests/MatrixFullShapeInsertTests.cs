using System.Collections.Generic;
using NUnit.Framework;

namespace Features.MatrixModule.Scripts.Editor.Tests {
    public sealed class MatrixFullShapeInsertTests {
        private const int FREE = 0;
        private const int BLOCKED = 1;
        private const int SHAPE = 5;
        private const int MATRIX_SIZE = 4;
        private const int SHAPE_SIZE = 2;

        private readonly MatrixFactory.MatrixFactory _matrixFactory = new();

        private IMatrix<int> _matrix;
        private IMatrix<int> _shape;

        [SetUp]
        public void SetUp() {
            _matrix = _matrixFactory.Create(MATRIX_SIZE, MATRIX_SIZE, FREE);
            _shape = _matrixFactory.Create(SHAPE_SIZE, SHAPE_SIZE, SHAPE);
        }

        [Test]
        public void WhenShapeFitsOnFreeCells_ThenWholeShapeIsInserted() {
            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 1, 1, out List<int> insertedCells, IsFree);

            Assert.IsTrue(isInserted);
            Assert.AreEqual(4, insertedCells.Count);
            CollectionAssert.AreEqual(new[] {
                FREE, FREE, FREE, FREE,
                FREE, SHAPE, SHAPE, FREE,
                FREE, SHAPE, SHAPE, FREE,
                FREE, FREE, FREE, FREE
            }, _matrix);
        }

        [Test]
        public void WhenShapeTouchesTheBottomRightCorner_ThenWholeShapeIsInserted() {
            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 2, 2, out List<int> insertedCells, IsFree);

            Assert.IsTrue(isInserted);
            Assert.AreEqual(4, insertedCells.Count);
            Assert.AreEqual(SHAPE, _matrix.GetValue(3, 3));
        }

        [Test]
        public void WhenShapeCoversWholeMatrix_ThenWholeShapeIsInserted() {
            IMatrix<int> shape = _matrixFactory.Create(MATRIX_SIZE, MATRIX_SIZE, SHAPE);

            bool isInserted = _matrix.TryInsertFullShapeMatrix(shape, 0, 0, out List<int> insertedCells, IsFree);

            Assert.IsTrue(isInserted);
            Assert.AreEqual(MATRIX_SIZE * MATRIX_SIZE, insertedCells.Count);
        }

        [Test]
        public void WhenShapeOverlapsBlockedCell_ThenNothingIsInserted() {
            _matrix.SetValue(2, 2, BLOCKED);

            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 1, 1, out List<int> insertedCells, IsFree);

            Assert.IsFalse(isInserted);
            Assert.IsEmpty(insertedCells);
            Assert.AreEqual(FREE, _matrix.GetValue(1, 1));
        }

        [Test]
        public void WhenShapeHangsOverTheRightEdge_ThenNothingIsInserted() {
            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 3, 0, out List<int> insertedCells, IsFree);

            Assert.IsFalse(isInserted);
            Assert.IsEmpty(insertedCells);
            Assert.AreEqual(FREE, _matrix.GetValue(3, 0));
        }

        [Test]
        public void WhenShapeHangsOverTheBottomEdge_ThenNothingIsInserted() {
            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 0, 3, out List<int> insertedCells, IsFree);

            Assert.IsFalse(isInserted);
            Assert.IsEmpty(insertedCells);
            Assert.AreEqual(FREE, _matrix.GetValue(0, 3));
        }

        [Test]
        public void WhenShapeStartsOutsideTheMatrix_ThenNothingIsInserted() {
            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, -1, 0, out List<int> insertedCells, IsFree);

            Assert.IsFalse(isInserted);
            Assert.IsEmpty(insertedCells);
            Assert.AreEqual(FREE, _matrix.GetValue(0, 0));
        }

        [Test]
        public void WhenShapePredicateSkipsCell_ThenThatMatrixCellKeepsItsValue() {
            _shape.SetValue(1, 1, FREE);
            _matrix.SetValue(2, 2, BLOCKED);

            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 1, 1, out List<int> insertedCells, null, IsShapeCell);

            Assert.IsTrue(isInserted);
            Assert.AreEqual(3, insertedCells.Count);
            Assert.AreEqual(BLOCKED, _matrix.GetValue(2, 2));
        }

        [Test]
        public void WhenShapePredicateSkipsCellOverBlockedCell_ThenLShapeIsInserted() {
            _shape.SetValue(1, 0, FREE);
            _matrix.SetValue(2, 1, BLOCKED);

            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 1, 1, out List<int> insertedCells, IsFree, IsShapeCell);

            Assert.IsTrue(isInserted);
            Assert.AreEqual(3, insertedCells.Count);
            Assert.AreEqual(BLOCKED, _matrix.GetValue(2, 1));
            Assert.AreEqual(SHAPE, _matrix.GetValue(1, 1));
            Assert.AreEqual(SHAPE, _matrix.GetValue(2, 2));
        }

        [Test]
        public void WhenInsertedCellOfLShapeIsBlocked_ThenNothingIsInserted() {
            _shape.SetValue(1, 0, FREE);
            _matrix.SetValue(1, 2, BLOCKED);

            bool isInserted = _matrix.TryInsertFullShapeMatrix(_shape, 1, 1, out List<int> insertedCells, IsFree, IsShapeCell);

            Assert.IsFalse(isInserted);
            Assert.IsEmpty(insertedCells);
            Assert.AreEqual(FREE, _matrix.GetValue(1, 1));
        }

        private static bool IsFree(int cell) =>
            cell == FREE;

        private static bool IsShapeCell(int cell) =>
            cell == SHAPE;
    }
}
