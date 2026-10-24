using UnityEngine;

namespace Features.WeaponModule.Scripts.Network {
    // One target hit as the shooter reports it. Public fields: Mirror serializes them without allocating.
    public struct WeaponHitReport {
        public ulong TargetId;
        public Vector3 Point;
        public Vector3 Normal;

        public WeaponHitReport(ulong targetId, Vector3 point, Vector3 normal) {
            TargetId = targetId;
            Point = point;
            Normal = normal;
        }
    }
}
