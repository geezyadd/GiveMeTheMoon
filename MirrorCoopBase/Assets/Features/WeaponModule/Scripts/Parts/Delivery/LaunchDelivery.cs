using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // A shot that leaves the muzzle as a projectile: it only picks the launch direction inside the spread cone; the
    // projectile itself decides later where it lands (WeaponDefinition.Projectile).
    public sealed class LaunchDelivery : IShotDelivery {
        private readonly float _spreadDegrees;
        private readonly IWeaponRandom _weaponRandom;

        public LaunchDelivery(float spreadDegrees, IWeaponRandom weaponRandom) {
            _spreadDegrees = spreadDegrees;
            _weaponRandom = weaponRandom;
        }

        public void Deliver(in WeaponShot shot, IShotOutcomeReceiver receiver) =>
            receiver.ReceiveOutcome(WeaponShotOutcome.CreateLaunch(shot, WeaponSpread.Turn(shot.Direction.normalized, _spreadDegrees, _weaponRandom)));
    }
}
