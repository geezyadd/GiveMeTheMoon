using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: the field rock a reported hit names, as the weapon hit path sees it. One instance serves every rock: the
    // hit path aims it at a rock and uses it within the same call, so a hit allocates nothing.
    public sealed class FieldRockWeaponTarget : IWeaponTarget {
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldRockDamageService _fieldRockDamageService;

        private FieldRockId _id;

        public bool IsAlive => _fieldRockQuery.TryGetRock(_id, out _);

        public FieldRockWeaponTarget(IFieldRockQuery fieldRockQuery, IFieldRockDamageService fieldRockDamageService) {
            _fieldRockQuery = fieldRockQuery;
            _fieldRockDamageService = fieldRockDamageService;
        }

        public void AimAt(FieldRockId id) =>
            _id = id;

        public void ServerReceiveHit(in WeaponHit hit) =>
            _fieldRockDamageService.ServerApplyHit(_id, hit);
    }
}
