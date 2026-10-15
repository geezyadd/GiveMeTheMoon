using System.Linq;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A built structure stands at the middle of its footprint box in field space (x along Right, z along Forward) and
    // turns with its quarter turns; the world pose follows the frame origin and forward.
    public sealed class FieldStructureLayoutTests {
        private const float CELL_SIZE = 40f;
        private const float TOLERANCE = 0.001f;
        private const int SQUARE_SIDE = 2;
        private const float HALF_TURN_DEGREES = 180f;

        private const int FRAME_SIDE_CELLS = 10;

        private static readonly Vector2Int _anchor = new(3, 5);
        private static readonly Vector3 _frameOrigin = new(100f, 50f, -20f);

        private FlightFieldEditorComposition _composition;
        private FieldStructureLayout _layout;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _layout = new FieldStructureLayout(_composition.FootprintReader);
        }

        [Test]
        public void WhenASquareStructureIsLaidOut_ThenItStandsAtTheMiddleOfItsCells() {
            FieldStructureDefinition square = FindSquare();

            Pose pose = _layout.GetFieldPose(square, _anchor, FieldRotation.None, CELL_SIZE);

            Vector3 expected = new((_anchor.y + SQUARE_SIDE / 2f) * CELL_SIZE, 0f, (_anchor.x + SQUARE_SIDE / 2f) * CELL_SIZE);
            Assert.Less((pose.position - expected).magnitude, TOLERANCE);
        }

        [Test]
        public void WhenAStructureIsTurned_ThenItsYawFollowsTheQuarterTurns() {
            Pose pose = _layout.GetFieldPose(FindSquare(), _anchor, FieldRotation.Half, CELL_SIZE);

            Assert.AreEqual(HALF_TURN_DEGREES, pose.rotation.eulerAngles.y, TOLERANCE);
        }

        [Test]
        public void WhenAFieldPoseIsMovedToTheWorld_ThenItMatchesTheFrameCellCenters() {
            Vector3 forward = Vector3.right;
            FieldFrame frame = new(_frameOrigin, forward, CELL_SIZE, FRAME_SIDE_CELLS);
            FieldStructureDefinition single = _composition.Catalog.Structures.First(structure => _composition.FootprintReader.Read(structure, FieldRotation.None).Cells.Count == 1);

            Pose world = _layout.ToWorld(_layout.GetFieldPose(single, _anchor, FieldRotation.None, CELL_SIZE), _frameOrigin, forward);

            Assert.Less((world.position - frame.CellToWorld(_anchor)).magnitude, TOLERANCE);
        }

        private FieldStructureDefinition FindSquare() =>
            _composition.Catalog.Structures.First(IsSquareOfSide);

        private bool IsSquareOfSide(FieldStructureDefinition structure) {
            FieldFootprint footprint = _composition.FootprintReader.Read(structure, FieldRotation.None);
            return footprint.Width == SQUARE_SIDE && footprint.Height == SQUARE_SIDE && footprint.Cells.Count == SQUARE_SIDE * SQUARE_SIDE;
        }
    }
}
