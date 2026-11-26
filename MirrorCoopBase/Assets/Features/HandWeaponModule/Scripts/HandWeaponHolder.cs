using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // The player with a weapon in the hand as its aim: shots start at the item's muzzle and go along the aim, which
    // is kept in the player's own frame so the same synced value points the same way on every peer.
    public sealed class HandWeaponHolder : IWeaponAim {
        private readonly Transform _muzzle;
        private readonly Transform _body;

        public Vector3 BodyAimDirection { get; private set; } = Vector3.forward;
        public Vector3 Origin => _muzzle.position;
        public Vector3 Direction => _body.rotation * BodyAimDirection;

        public HandWeaponHolder(Transform muzzle, Transform body) {
            _muzzle = muzzle;
            _body = body;
        }

        public void SetBodyAimDirection(Vector3 bodyAimDirection) =>
            BodyAimDirection = bodyAimDirection;

        public void AimAlong(Vector3 worldDirection) =>
            BodyAimDirection = Quaternion.Inverse(_body.rotation) * worldDirection;
    }
}
