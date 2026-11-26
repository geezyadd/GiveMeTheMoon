using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Where the crosshair points for a weapon in the hand: the view's centre ray finds the first thing the weapon can
    // strike (never the holder's own body), and the shot leaves the muzzle toward that point, or toward the far end of
    // the range when it meets nothing. The muzzle sits off the view, so a shot along the view itself would pass beside
    // a small target.
    public sealed class CrosshairAim {
        private readonly IWeaponRaycast _weaponRaycast;
        private readonly uint _holderBodyId;

        public CrosshairAim(IWeaponRaycast weaponRaycast, uint holderBodyId) {
            _weaponRaycast = weaponRaycast;
            _holderBodyId = holderBodyId;
        }

        public Vector3 GetShotDirection(Transform view, Vector3 muzzle, float rangeMeters) {
            Vector3 aimPoint = _weaponRaycast.TryRaycast(view.position, view.forward, rangeMeters, _holderBodyId, out WeaponRaycastHit hit)
                ? hit.Point
                : view.position + view.forward * rangeMeters;
            Vector3 toAimPoint = aimPoint - muzzle;
            return toAimPoint.sqrMagnitude > 0f ? toAimPoint.normalized : view.forward;
        }
    }
}
