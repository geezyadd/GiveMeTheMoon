using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // An instant ray along the aim, turned by a random angle inside the spread cone.
    public sealed class HitscanDelivery : IShotDelivery {
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;
        // Above this |y| the aim is close to vertical and the world up would give a degenerate cross product.
        private const float NEAR_VERTICAL_AIM_Y = 0.9f;

        private readonly float _spreadDegrees;
        private readonly IWeaponRaycast _weaponRaycast;
        private readonly IWeaponRandom _weaponRandom;

        public HitscanDelivery(float spreadDegrees, IWeaponRaycast weaponRaycast, IWeaponRandom weaponRandom) {
            _spreadDegrees = spreadDegrees;
            _weaponRaycast = weaponRaycast;
            _weaponRandom = weaponRandom;
        }

        public void Deliver(in WeaponShot shot, IShotOutcomeReceiver receiver) {
            Vector3 direction = CreateSpreadDirection(shot.Direction.normalized);
            WeaponShotOutcome outcome = _weaponRaycast.TryRaycast(shot.Origin, direction, shot.RangeMeters, out WeaponRaycastHit hit)
                ? WeaponShotOutcome.CreateImpact(shot, hit)
                : WeaponShotOutcome.CreateMiss(shot, shot.Origin + direction * shot.RangeMeters);
            receiver.ReceiveOutcome(outcome);
        }

        // The square root spreads the shots evenly over the cone's cross-section instead of crowding its middle.
        private Vector3 CreateSpreadDirection(Vector3 aim) {
            float angle = _spreadDegrees * Mathf.Deg2Rad * Mathf.Sqrt(_weaponRandom.NextUnitValue());
            float azimuth = FULL_TURN_RADIANS * _weaponRandom.NextUnitValue();
            Vector3 side = Vector3.Cross(aim, PickHelperAxis(aim)).normalized;
            Vector3 up = Vector3.Cross(side, aim);
            Vector3 offset = side * Mathf.Cos(azimuth) + up * Mathf.Sin(azimuth);
            return (aim * Mathf.Cos(angle) + offset * Mathf.Sin(angle)).normalized;
        }

        private static Vector3 PickHelperAxis(Vector3 aim) =>
            Mathf.Abs(aim.y) < NEAR_VERTICAL_AIM_Y ? Vector3.up : Vector3.right;
    }
}
