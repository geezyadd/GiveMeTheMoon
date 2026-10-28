using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Where a piece of ore is at a moment, the same way on every peer: a drifting one from its field position and
    // velocity, a hooked one from its offset to this peer's ship.
    public interface IFieldOreMotion {
        public Vector3 GetWorldPosition(in FieldOreEntry entry, double now);
        // A hooked piece that has come all the way in to the ship.
        public bool IsPullComplete(in FieldOreEntry entry, double now);
        public Vector3 ToFieldPosition(Vector3 worldPosition);
        public Vector3 ToFieldDirection(Vector3 worldDirection);
        public bool TryGetShipOffset(Vector3 worldPosition, out Vector3 shipOffset);
    }
}
