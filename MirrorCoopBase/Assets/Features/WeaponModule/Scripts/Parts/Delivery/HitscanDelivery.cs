using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // An instant ray along the aim, turned by a random angle inside the spread cone.
    public sealed class HitscanDelivery : IShotDelivery {
        private readonly float _spreadDegrees;
        private readonly IWeaponRaycast _weaponRaycast;
        private readonly IWeaponRandom _weaponRandom;

        public HitscanDelivery(float spreadDegrees, IWeaponRaycast weaponRaycast, IWeaponRandom weaponRandom) {
            _spreadDegrees = spreadDegrees;
            _weaponRaycast = weaponRaycast;
            _weaponRandom = weaponRandom;
        }

        public void Deliver(in WeaponShot shot, IShotOutcomeReceiver receiver) {
            Vector3 direction = WeaponSpread.Turn(shot.Direction.normalized, _spreadDegrees, _weaponRandom);
            WeaponShotOutcome outcome = _weaponRaycast.TryRaycast(shot.Origin, direction, shot.RangeMeters, shot.ShooterBodyId, out WeaponRaycastHit hit)
                ? WeaponShotOutcome.CreateImpact(shot, hit)
                : WeaponShotOutcome.CreateMiss(shot, shot.Origin + direction * shot.RangeMeters);
            receiver.ReceiveOutcome(outcome);
        }
    }
}
