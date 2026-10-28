using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Features.WeaponModule.Scripts.Parts.Feel;
using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Shooting {
    // One weapon firing on one peer: the fire mode decides when shots are born, the delivery where they land, and the
    // outcomes go to the receiver this peer needs. A struck surface that shows impacts is shown here first, on every
    // peer that fires or replays the shot. It never asks what holds the weapon.
    public sealed class WeaponShooter : IShotOutcomeReceiver {
        private readonly WeaponDefinition _weapon;
        private readonly IWeaponFireMode _fireMode;
        private readonly IShotDelivery _delivery;
        private readonly IWeaponFeel _feel;
        private readonly IContinuousFireFeel _continuousFireFeel;
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
            _continuousFireFeel = feel as IContinuousFireFeel;
            _outcomeReceiver = outcomeReceiver;
        }

        public void Advance(IWeaponHolder holder, float deltaTime) {
            int shots = _fireMode.Advance(holder.IsTriggerHeld, deltaTime);
            if (_continuousFireFeel != null)
                _continuousFireFeel.PresentFiring(_fireMode.IsFiring, _fireMode.Readiness);

            for (int i = 0; i < shots; i++)
                Fire(holder);
        }

        private void Fire(IWeaponHolder holder) =>
            _delivery.Deliver(
                new WeaponShot(_weapon.Id, holder.Aim.Origin, holder.Aim.Direction, _weapon.RangeMeters),
                this);

        public void ReceiveOutcome(in WeaponShotOutcome outcome) {
            IWeaponImpactFeedback impactFeedback = outcome.ImpactFeedback;
            if (impactFeedback != null)
                impactFeedback.PresentImpact(outcome.EndPoint, outcome.Normal);

            _outcomeReceiver.ReceiveOutcome(outcome);
        }
    }
}
