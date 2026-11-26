using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.TurretModule.Scripts {
    // The turret's aim: shots leave its muzzle along its barrels.
    public sealed class ShipTurretWeaponHolder : IWeaponAim {
        private readonly Transform _muzzle;

        public Vector3 Origin => _muzzle.position;
        public Vector3 Direction => _muzzle.forward;

        public ShipTurretWeaponHolder(Transform muzzle) =>
            _muzzle = muzzle;
    }
}
