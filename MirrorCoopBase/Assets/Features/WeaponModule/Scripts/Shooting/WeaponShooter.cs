using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Features.WeaponModule.Scripts.Parts.Feel;
using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Shooting {
    // One weapon firing on one peer: the fire mode decides when shots are born, the delivery where they land, and the
    // outcomes go to the receiver this peer needs. It never asks what holds the weapon.
    public sealed class WeaponShooter {
        private readonly WeaponDefinition _weapon;
        private readonly IWeaponFireMode _fireMode;
        private readonly IShotDelivery _delivery;
        private readonly IWeaponFeel _feel;
        private readonly IShotOutcomeReceiver _outcomeReceiver;

        public bool IsFiring => _fireMode.IsFiring;
        public float Readiness => _fireMode.Readiness;

        public WeaponShooter(
            WeaponDefinition weapon,
            IWeaponFireMode fireMode,
            IShotDelivery delivery,
            IWeaponFeel feel,
            IShotOutcomeReceiver outcomeReceiver) {
            _weapon = weapon;
            _fireMode = fireMode;
            _delivery = delivery;
            _feel = feel;
            _outcomeReceiver = outcomeReceiver;
        }

        public void Advance(IWeaponHolder holder, float deltaTime) {
            int shots = _fireMode.Advance(holder.IsTriggerHeld, deltaTime);
            _feel.PresentFiring(_fireMode.IsFiring, _fireMode.Readiness);
            for (int i = 0; i < shots; i++)
                Fire(holder);
        }

        private void Fire(IWeaponHolder holder) =>
            _delivery.Deliver(
                new WeaponShot(_weapon.Id, holder.Aim.Origin, holder.Aim.Direction, _weapon.RangeMeters),
                _outcomeReceiver);
    }
}
