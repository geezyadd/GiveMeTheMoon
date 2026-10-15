using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    public interface IFieldStructureLayout {
        public Pose GetFieldPose(FieldStructureDefinition structure, Vector2Int anchor, FieldRotation rotation, float cellSize);
        public Pose ToWorld(Pose fieldPose, Vector3 origin, Vector3 forward);
    }
}
