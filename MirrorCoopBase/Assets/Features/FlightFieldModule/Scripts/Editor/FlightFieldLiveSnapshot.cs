using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The live field of the running game for the debug map: the generated field, its frame after the world shifts
    // since it was published, and the ship.
    public readonly struct FlightFieldLiveSnapshot {
        public FlightField Field { get; }
        public int Number { get; }
        public FieldFrame LiveFrame { get; }
        public bool HasShip { get; }
        public Vector3 ShipPosition { get; }

        public FlightFieldLiveSnapshot(FlightField field, int number, FieldFrame liveFrame, bool hasShip, Vector3 shipPosition) {
            Field = field;
            Number = number;
            LiveFrame = liveFrame;
            HasShip = hasShip;
            ShipPosition = shipPosition;
        }
    }
}
