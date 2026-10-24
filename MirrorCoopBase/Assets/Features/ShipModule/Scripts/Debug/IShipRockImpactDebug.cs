#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Impacts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    public interface IShipRockImpactDebug {
        // Server: moves the flying ship a short way before the nearest rock of this kind at its height and turns it
        // to the rock, so it flies into it.
        public bool TryDebugAimAtNearestRock(ShipRockImpactKind kind, out FieldRockDescription rock);
        public void DebugMoveShipTo(Vector3 position);
    }
}
#endif
