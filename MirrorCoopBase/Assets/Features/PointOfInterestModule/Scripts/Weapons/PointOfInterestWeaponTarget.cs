using Features.PointOfInterestModule.Scripts.Server;
using Features.WeaponModule.Contracts;

namespace Features.PointOfInterestModule.Scripts.Weapons {
    // Server: the reward container a reported hit names, as the weapon hit path sees it. One instance serves every
    // point: the hit path aims it and uses it within the same call.
    public sealed class PointOfInterestWeaponTarget : IWeaponDamageTarget {
        private readonly IPointOfInterestDamageService _pointOfInterestDamageService;

        private int _pointId;

        public bool IsAlive => _pointOfInterestDamageService.IsStanding(_pointId);

        public PointOfInterestWeaponTarget(IPointOfInterestDamageService pointOfInterestDamageService) =>
            _pointOfInterestDamageService = pointOfInterestDamageService;

        public void AimAt(int pointId) =>
            _pointId = pointId;

        public void ServerReceiveHit(in WeaponHit hit) =>
            _pointOfInterestDamageService.ServerApplyHit(_pointId, hit.Point, hit.Damage);
    }
}
