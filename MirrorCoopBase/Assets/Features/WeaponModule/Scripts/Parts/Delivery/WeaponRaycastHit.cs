using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // What a weapon ray struck: a surface, the id of the weapon target on it when there is one, and what shows the
    // strike on that surface when it shows one (null otherwise).
    public readonly struct WeaponRaycastHit {
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public bool HasTarget { get; }
        public ulong TargetId { get; }
        public IWeaponImpactFeedback ImpactFeedback { get; }

        public WeaponRaycastHit(Vector3 point, Vector3 normal, bool hasTarget, ulong targetId, IWeaponImpactFeedback impactFeedback = null) {
            Point = point;
            Normal = normal;
            HasTarget = hasTarget;
            TargetId = targetId;
            ImpactFeedback = impactFeedback;
        }
    }
}
