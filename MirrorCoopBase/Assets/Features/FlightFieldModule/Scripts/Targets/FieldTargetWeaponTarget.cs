using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: the target balloon a reported hit names, as the weapon hit path sees it. One instance serves every
    // balloon: the hit path aims it at a balloon and uses it within the same call, so a hit allocates nothing.
    public sealed class FieldTargetWeaponTarget : IWeaponTarget {
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldTargetDamageService _fieldTargetDamageService;

        private FieldTargetId _id;

        public bool IsAlive => _fieldTargetQuery.TryGetTarget(_id, out _);

        public FieldTargetWeaponTarget(IFieldTargetQuery fieldTargetQuery, IFieldTargetDamageService fieldTargetDamageService) {
            _fieldTargetQuery = fieldTargetQuery;
            _fieldTargetDamageService = fieldTargetDamageService;
        }

        public void AimAt(FieldTargetId id) =>
            _id = id;

        public void ServerReceiveHit(in WeaponHit hit) =>
            _fieldTargetDamageService.ServerApplyHit(_id, hit);
    }
}
