using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // A rope thrown straight along the aim, with no spread: it catches the first target within its catch radius of
    // the line, up to the weapon's range. The rope's flight out and back is the feel's; what a caught target does is
    // the weapon's hit effects on the server.
    public sealed class RopeDelivery : IShotDelivery {
        private readonly IWeaponRaycast _weaponRaycast;

        public RopeDelivery(IWeaponRaycast weaponRaycast) =>
            _weaponRaycast = weaponRaycast;

        public void Deliver(in WeaponShot shot, IShotOutcomeReceiver receiver) {
            Vector3 direction = shot.Direction.normalized;
            WeaponShotOutcome outcome = _weaponRaycast.TryRaycast(shot.Origin, direction, shot.RangeMeters, out WeaponRaycastHit hit)
                ? WeaponShotOutcome.CreateImpact(shot, hit)
                : WeaponShotOutcome.CreateMiss(shot, shot.Origin + direction * shot.RangeMeters);
            receiver.ReceiveOutcome(outcome);
        }
    }
}
