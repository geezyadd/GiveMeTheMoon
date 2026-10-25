using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // A rock the swept hull touches: the rock, how it counts for the ship and the nearest hull point to its centre.
    public readonly struct ShipRockContact {
        public FieldRockDescription Rock { get; }
        public ShipRockImpactKind Kind { get; }
        public Vector3 HullPoint { get; }

        public ShipRockContact(FieldRockDescription rock, ShipRockImpactKind kind, Vector3 hullPoint) {
            Rock = rock;
            Kind = kind;
            HullPoint = hullPoint;
        }
    }
}
