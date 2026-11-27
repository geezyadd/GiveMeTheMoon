using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;
using Features.WeaponModule.Scripts.Parts.FireModes;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Shooting {
    // One held weapon's runtime parts on one peer: the fire mode decides when shots are born, the delivery where one
    // lands (at once for a ray, a launch direction for a projectile). It never asks what holds the weapon. Every
    // current delivery answers within Deliver.
    public sealed class WeaponShooter : IShotOutcomeReceiver {
        private readonly WeaponDefinition _weapon;
        private readonly IWeaponFireMode _fireMode;
        private readonly IShotDelivery _delivery;

        private WeaponShotOutcome _lastOutcome;

        public bool IsFiring => _fireMode.IsFiring;
        public float Readiness => _fireMode.Readiness;

        public WeaponShooter(WeaponDefinition weapon, IWeaponFireMode fireMode, IShotDelivery delivery) {
            _weapon = weapon;
            _fireMode = fireMode;
            _delivery = delivery;
        }

        // Moves the fire mode on and shows its lasting state (barrel spin); returns how many shots were born.
        public int Advance(bool isTriggerHeld, float deltaTime, IWeaponFeel feel) {
            int shots = _fireMode.Advance(isTriggerHeld, deltaTime);
            feel?.PresentFiring(_fireMode.IsFiring, _fireMode.Readiness);
            return shots;
        }

        public WeaponShotOutcome Fire(Vector3 origin, Vector3 direction, uint shooterBodyId) {
            _delivery.Deliver(new WeaponShot(_weapon.Id, origin, direction, _weapon.RangeMeters, shooterBodyId), this);
            return _lastOutcome;
        }

        public void ReceiveOutcome(in WeaponShotOutcome outcome) =>
            _lastOutcome = outcome;
    }
}
