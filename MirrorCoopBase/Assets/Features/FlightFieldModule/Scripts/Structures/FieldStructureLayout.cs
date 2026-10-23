using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // Where a placed structure stands. Field space is the frame's own: x along Right, z along Forward, origin at the
    // corner of cell (0, 0); the structure stands at the middle of its footprint box, turned by its quarter turns.
    public sealed class FieldStructureLayout : IFieldStructureLayout {
        private const float QUARTER_TURN_DEGREES = 90f;

        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldStructureLayout(IFieldFootprintReader fieldFootprintReader) =>
            _fieldFootprintReader = fieldFootprintReader;

        public Pose GetFieldPose(FieldStructureDefinition structure, Vector2Int anchor, FieldRotation rotation, float cellSize) =>
            GetFieldPose(_fieldFootprintReader.Read(structure, rotation), anchor, rotation, cellSize);

        public Pose GetFieldPose(FieldFootprint footprint, Vector2Int anchor, FieldRotation rotation, float cellSize) {
            Vector3 position = new((anchor.y + footprint.Height / 2f) * cellSize, 0f, (anchor.x + footprint.Width / 2f) * cellSize);
            return new Pose(position, Quaternion.Euler(0f, (int)rotation * QUARTER_TURN_DEGREES, 0f));
        }

        public Pose ToWorld(Pose fieldPose, Vector3 origin, Vector3 forward) {
            Quaternion frameRotation = Quaternion.LookRotation(forward, Vector3.up);
            return new Pose(origin + frameRotation * fieldPose.position, frameRotation * fieldPose.rotation);
        }
    }
}
