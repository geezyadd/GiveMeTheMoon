#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts.Debug {
    public interface IShipRockImpactDebug {
        // Server: moves the flying ship a short way before the nearest rock of this kind at its height and turns it
        // to the rock, so it flies into it.
        public bool TryDebugAimAtNearestRock(ShipRockImpactKind kind, out FieldRockDescription rock);
        public void DebugMoveShipTo(Vector3 position);
        // The middle of the hull boxes above the ship's pivot, meters: where a thing must stand for the ship to fly
        // into it.
        public float DebugGetHullMiddleHeight();
    }
}
#endif
