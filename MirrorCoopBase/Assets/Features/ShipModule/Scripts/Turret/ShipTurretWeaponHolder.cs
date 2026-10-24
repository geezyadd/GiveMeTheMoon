using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.ShipModule.Scripts.Turret {
    // The player seated in a turret as the weapon's holder: the trigger is the seated player's (read live on the
    // owner, synced on the others) and the aim is the turret's muzzle along its barrels.
    public sealed class ShipTurretWeaponHolder : IWeaponHolder, IWeaponAim {
        private readonly Transform _muzzle;

        // Shots on a peer only show; the server takes the shooter from its own record of the holding, never from here.
        public PlayerKey Holder => default;
        public bool IsTriggerHeld { get; private set; }
        public IWeaponAim Aim => this;
        public Vector3 Origin => _muzzle.position;
        public Vector3 Direction => _muzzle.forward;

        public ShipTurretWeaponHolder(Transform muzzle) =>
            _muzzle = muzzle;

        public void SetTriggerHeld(bool isTriggerHeld) =>
            IsTriggerHeld = isTriggerHeld;
    }
}
