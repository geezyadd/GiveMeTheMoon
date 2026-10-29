using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // Server: the drifting player a reported hit names, as the weapon hit path sees it. One instance serves every
    // player: the hit path aims it and uses it within the same call. A rope hooks the player like ore, without money;
    // a bullet does nothing to it. A player never hooks itself and a hit must lie on the player now.
    public sealed class ShipOverboardWeaponTarget : IWeaponPullTarget {
        private readonly ShipOverboardConfiguration _shipOverboardConfiguration;

        private ShipOverboardRider _overboardRider;

        public bool IsAlive => _overboardRider != null && _overboardRider.IsOverboard;

        public ShipOverboardWeaponTarget(ShipOverboardConfiguration shipOverboardConfiguration) =>
            _shipOverboardConfiguration = shipOverboardConfiguration;

        public void AimAt(ShipOverboardRider overboardRider) =>
            _overboardRider = overboardRider;

        public void ServerReceivePull(in WeaponHit hit, in WeaponPull pull) {
            if (IsAlive == false || hit.Shooter.Id == _overboardRider.Key.Id || IsOnPlayer(hit.Point) == false)
                return;

            _overboardRider.ServerHook(hit.Shooter, hit.WeaponId, pull.PullSpeedMetersPerSecond, pull.MaxRopeMeters);
        }

        private bool IsOnPlayer(Vector3 hitPoint) {
            float reach = _shipOverboardConfiguration.HookReachMeters;
            return (hitPoint - _overboardRider.Position).sqrMagnitude <= reach * reach;
        }
    }
}
