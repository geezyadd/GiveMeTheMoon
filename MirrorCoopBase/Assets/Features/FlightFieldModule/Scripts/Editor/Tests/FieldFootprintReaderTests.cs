using System.Linq;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    public sealed class FieldFootprintReaderTests {
        private const string FOOTPRINT_ROWS_FIELD = "_footprintRows";

        private readonly FieldFootprintReader _fieldFootprintReader = new();

        private FieldStructureDefinition _structure;

        [SetUp]
        public void SetUp() =>
            _structure = ScriptableObject.CreateInstance<FieldStructureDefinition>();

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_structure);

        [Test]
        public void WhenLShapeIsRead_ThenOnlyItsMarkedCellsAreOccupied() {
            SetRows("X..", "XXX");

            FieldFootprint footprint = _fieldFootprintReader.Read(_structure, FieldRotation.None);

            Assert.AreEqual(3, footprint.Width);
            Assert.AreEqual(2, footprint.Height);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, footprint.Cells.ToArray());
        }

        [Test]
        public void WhenLShapeIsTurnedAQuarter_ThenBoxAndCellsTurnClockwise() {
            SetRows("X..", "XXX");

            FieldFootprint footprint = _fieldFootprintReader.Read(_structure, FieldRotation.Quarter);

            Assert.AreEqual(2, footprint.Width);
            Assert.AreEqual(3, footprint.Height);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, footprint.Cells.ToArray());
        }

        [Test]
        public void WhenShapeIsTurnedHalf_ThenEveryCellIsMirroredThroughTheBoxCenter() {
            SetRows("XX.", ".XX");

            FieldFootprint unturned = _fieldFootprintReader.Read(_structure, FieldRotation.None);
            FieldFootprint turnedHalf = _fieldFootprintReader.Read(_structure, FieldRotation.Half);

            CollectionAssert.AreEquivalent(TurnHalf(unturned).Cells.ToArray(), turnedHalf.Cells.ToArray());
        }

        private static FieldFootprint TurnHalf(FieldFootprint footprint) =>
            new(footprint.Width, footprint.Height,
                footprint.Cells.Select(cell => new Vector2Int(footprint.Width - 1 - cell.x, footprint.Height - 1 - cell.y)).ToList());

        private void SetRows(params string[] rows) {
            SerializedObject serializedStructure = new(_structure);
            SerializedProperty rowsProperty = serializedStructure.FindProperty(FOOTPRINT_ROWS_FIELD);
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
                rowsProperty.GetArrayElementAtIndex(i).stringValue = rows[i];

            serializedStructure.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
