using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // One beacon of a field: its place in field root space (x along Right, y above the flight plane, z along Forward),
    // its colour, why it stands there and the route it marks.
    public readonly struct FieldBeacon {
        public Vector3 Position { get; }
        public FieldBeaconLane Lane { get; }
        public FieldBeaconReason Reason { get; }
        public int RouteId { get; }

        public FieldBeacon(Vector3 position, FieldBeaconLane lane, FieldBeaconReason reason, int routeId) {
            Position = position;
            Lane = lane;
            Reason = reason;
            RouteId = routeId;
        }
    }
}
