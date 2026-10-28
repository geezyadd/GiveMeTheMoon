using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: the piece of ore a reported hit names, as the weapon hit path sees it. One instance serves every piece:
    // the hit path aims it at a piece and uses it within the same call, so a hit allocates nothing. A rope hooks it;
    // a bullet does nothing to it (ore does not break).
    public sealed class FieldOreWeaponTarget : IWeaponTarget, IWeaponPullTarget {
        private readonly IFieldOreQuery _fieldOreQuery;
        private readonly IFieldOreHooks _fieldOreHooks;

        private FieldOreId _id;

        public bool IsAlive => _fieldOreQuery.TryGetOre(_id, out _);

        public FieldOreWeaponTarget(IFieldOreQuery fieldOreQuery, IFieldOreHooks fieldOreHooks) {
            _fieldOreQuery = fieldOreQuery;
            _fieldOreHooks = fieldOreHooks;
        }

        public void AimAt(FieldOreId id) =>
            _id = id;

        public void ServerReceiveHit(in WeaponHit hit) {
        }

        public void ServerReceivePull(in WeaponHit hit, in WeaponPull pull) =>
            _fieldOreHooks.ServerHook(_id, hit.Shooter, hit.WeaponId, hit.Point, pull);
    }
}
