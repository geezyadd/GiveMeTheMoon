using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // One rock hit on the ship as every peer gets it: the kind, the world point on the hull and how hard (0..1).
    public readonly struct ShipRockImpact {
        public ShipRockImpactKind Kind { get; }
        public Vector3 Point { get; }
        public float Strength { get; }

        public ShipRockImpact(ShipRockImpactKind kind, Vector3 point, float strength) {
            Kind = kind;
            Point = point;
            Strength = strength;
        }
    }
}
