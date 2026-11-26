using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Where the crosshair points for a weapon in the hand: the view's centre ray finds the first thing the weapon can
    // strike (never the holder's own body), and the weapon aims from a point that does not turn with it (the palm) at
    // that point, or at the far end of the range when it meets nothing. Aiming from the muzzle would feed back: the
    // muzzle moves as the weapon turns. Close to the hand (the barrel pressed to the floor or to a player) the aim eases
    // over to the view's own direction: fully along the view under the minimum distance, fully at the point from twice
    // it, smoothly between, so lowering the weapon never snaps it; a point beside or behind the hand fades out alike.
    public sealed class CrosshairAim {
        // The point counts fully once the way to it is at least this close to the view's own direction (cos ~66°).
        private const float MIN_FACING = 0.4f;

        private readonly IWeaponRaycast _weaponRaycast;
        private readonly uint _holderBodyId;
        private readonly float _minAimDistance;

        public CrosshairAim(IWeaponRaycast weaponRaycast, uint holderBodyId, float minAimDistance) {
            _weaponRaycast = weaponRaycast;
            _holderBodyId = holderBodyId;
            _minAimDistance = minAimDistance;
        }

        public Vector3 GetShotDirection(Transform view, Vector3 aimOrigin, float rangeMeters) {
            Vector3 aimPoint = _weaponRaycast.TryRaycast(view.position, view.forward, rangeMeters, _holderBodyId, out WeaponRaycastHit hit)
                ? hit.Point
                : view.position + view.forward * rangeMeters;
            Vector3 toAimPoint = aimPoint - aimOrigin;
            float distance = toAimPoint.magnitude;
            if (distance <= 0f)
                return view.forward;

            Vector3 toPoint = toAimPoint / distance;
            float share = PointShare(distance, Vector3.Dot(toPoint, view.forward));
            return Vector3.Slerp(view.forward, toPoint, share).normalized;
        }

        // How much the aim follows the point rather than the view: none closer than the minimum distance, all from
        // twice that on, and eased in between; a point at the side of or behind the hand fades it out the same way.
        private float PointShare(float distance, float facing) {
            float byDistance = _minAimDistance > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_minAimDistance, _minAimDistance * 2f, distance)) : 1f;
            float byFacing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, MIN_FACING, facing));
            return byDistance * byFacing;
        }
    }
}
