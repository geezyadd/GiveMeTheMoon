using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Every ray strikes the same weapon target a metre ahead, which shows its strikes on the given feedback, if any.
    public sealed class TargetEverywhereRaycast : IWeaponRaycast {
        private readonly ulong _targetId;
        private readonly IWeaponImpactFeedback _impactFeedback;

        public TargetEverywhereRaycast(ulong targetId, IWeaponImpactFeedback impactFeedback = null) {
            _targetId = targetId;
            _impactFeedback = impactFeedback;
        }

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            hit = new WeaponRaycastHit(origin + direction, -direction, hasTarget: true, _targetId, _impactFeedback);
            return true;
        }
    }
}
