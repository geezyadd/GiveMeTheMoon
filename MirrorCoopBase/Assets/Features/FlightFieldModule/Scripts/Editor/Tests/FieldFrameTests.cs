using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    public sealed class FieldFrameTests {
        private const float CELL_SIZE = 40f;
        private const int LENGTH_CELLS = 30;
        private const int WIDTH_CELLS = 20;
        private const float TOLERANCE = 0.001f;

        private static readonly Vector3 _origin = new(-100f, 120f, 30f);
        private static readonly Vector3 _forward = new Vector3(1f, 0f, 1f).normalized;

        [Test]
        public void WhenCellCenterIsConvertedBack_ThenTheSameCellComesOut() {
            FieldFrame frame = new(_origin, _forward, CELL_SIZE, LENGTH_CELLS, WIDTH_CELLS);
            Vector2Int cell = new(7, 13);

            Vector3 center = frame.CellToWorld(cell);

            Assert.AreEqual(cell, frame.WorldToCell(center));
            Assert.AreEqual(frame.Height, center.y, TOLERANCE);
        }

        [Test]
        public void WhenFrameIsShifted_ThenOnlyOriginMovesAndCellsMoveWithTheWorld() {
            FieldFrame frame = new(_origin, _forward, CELL_SIZE, LENGTH_CELLS, WIDTH_CELLS);
            Vector3 shift = new(-800f, 0f, 250f);
            Vector2Int cell = new(3, 4);

            FieldFrame shifted = frame.Shift(shift);

            Assert.AreEqual(_origin + shift, shifted.Origin);
            Assert.AreEqual(frame.Forward, shifted.Forward);
            Assert.AreEqual(frame.Size, shifted.Size);
            Assert.AreEqual(cell, shifted.WorldToCell(frame.CellToWorld(cell) + shift));
        }
    }
}
