using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Bending an agent's course round the rock spheres of the field data ahead of it, and the last-resort push out of one.
    public interface IFieldAgentRockAvoidance {
        public float CalculateLookAheadMeters(Vector3 velocity, FieldAgentProfile profile);
        public Vector3 CalculateAvoidance(Vector3 position, Vector3 velocity, Vector3 course, IReadOnlyList<FieldRockDescription> rocks, FieldAgentProfile profile, float sideSign);
        public bool PushOutOfRocks(ref Vector3 position, ref Vector3 velocity, IReadOnlyList<FieldRockDescription> rocks, FieldAgentProfile profile);
    }
}
