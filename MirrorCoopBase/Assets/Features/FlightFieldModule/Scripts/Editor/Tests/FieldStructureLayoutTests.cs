using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A built structure stands at the middle of its footprint box in field space (x along Right, z along Forward) and
    // turns with its quarter turns; the world pose follows the frame origin and forward.
    public sealed class FieldStructureLayoutTests {
        private const float CELL_SIZE = 40f;
        private const float TOLERANCE = 0.001f;
        private const int SQUARE_SIDE = 2;
        private const float HALF_TURN_DEGREES = 180f;
        private const string FOOTPRINT_ROWS_FIELD = "_footprintRows";
        private const string SQUARE_ROW = "XX";
        private const string SINGLE_ROW = "X";

        private const int FRAME_SIDE_CELLS = 10;

        private static readonly Vector2Int _anchor = new(3, 5);
        private static readonly Vector3 _frameOrigin = new(100f, 50f, -20f);

        private FieldStructureLayout _layout;
        private FieldStructureDefinition _square;
        private FieldStructureDefinition _single;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _layout = new FieldStructureLayout(new FieldFootprintReader());
            _square = CreateStructure(SQUARE_ROW, SQUARE_ROW);
            _single = CreateStructure(SINGLE_ROW);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            Object.DestroyImmediate(_square);
            Object.DestroyImmediate(_single);
        }

        [Test]
        public void WhenASquareStructureIsLaidOut_ThenItStandsAtTheMiddleOfItsCells() {
            Pose pose = _layout.GetFieldPose(_square, _anchor, FieldRotation.None, CELL_SIZE);

            Vector3 expected = new((_anchor.y + SQUARE_SIDE / 2f) * CELL_SIZE, 0f, (_anchor.x + SQUARE_SIDE / 2f) * CELL_SIZE);
            Assert.Less((pose.position - expected).magnitude, TOLERANCE);
        }

        [Test]
        public void WhenAStructureIsTurned_ThenItsYawFollowsTheQuarterTurns() {
            Pose pose = _layout.GetFieldPose(_square, _anchor, FieldRotation.Half, CELL_SIZE);

            Assert.AreEqual(HALF_TURN_DEGREES, pose.rotation.eulerAngles.y, TOLERANCE);
        }

        [Test]
        public void WhenAFieldPoseIsMovedToTheWorld_ThenItMatchesTheFrameCellCenters() {
            Vector3 forward = Vector3.right;
            FieldFrame frame = new(_frameOrigin, forward, CELL_SIZE, FRAME_SIDE_CELLS, FRAME_SIDE_CELLS);

            Pose world = _layout.ToWorld(_layout.GetFieldPose(_single, _anchor, FieldRotation.None, CELL_SIZE), _frameOrigin, forward);

            Assert.Less((world.position - frame.CellToWorld(_anchor)).magnitude, TOLERANCE);
        }

        private static FieldStructureDefinition CreateStructure(params string[] rows) {
            FieldStructureDefinition structure = ScriptableObject.CreateInstance<FieldStructureDefinition>();
            SerializedObject serializedStructure = new(structure);
            SerializedProperty rowsProperty = serializedStructure.FindProperty(FOOTPRINT_ROWS_FIELD);
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
                rowsProperty.GetArrayElementAtIndex(i).stringValue = rows[i];

            serializedStructure.ApplyModifiedPropertiesWithoutUndo();
            return structure;
        }
    }
}
