using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // What a weapon ray struck: a surface, and the id of the weapon target on it when there is one.
    public readonly struct WeaponRaycastHit {
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public bool HasTarget { get; }
        public ulong TargetId { get; }

        public WeaponRaycastHit(Vector3 point, Vector3 normal, bool hasTarget, ulong targetId) {
            Point = point;
            Normal = normal;
            HasTarget = hasTarget;
            TargetId = targetId;
        }
    }
}
