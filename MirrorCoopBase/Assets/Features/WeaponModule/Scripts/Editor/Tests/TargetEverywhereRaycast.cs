using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Every ray strikes the same weapon target a metre ahead.
    public sealed class TargetEverywhereRaycast : IWeaponRaycast {
        private readonly ulong _targetId;

        public TargetEverywhereRaycast(ulong targetId) =>
            _targetId = targetId;

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            hit = new WeaponRaycastHit(origin + direction, -direction, hasTarget: true, _targetId);
            return true;
        }
    }
}
