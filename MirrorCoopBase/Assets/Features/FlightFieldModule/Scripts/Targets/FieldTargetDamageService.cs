using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: a weapon hit on a target balloon of the current field. The balloon is checked against the field data, not
    // the scene: it exists, is not popped, and the reported point lies on it. The hit's damage adds to what it took;
    // at its kind's hit points it pops through the one pop entry, as a Weapon pop by the shooter with the weapon's id.
    // The damage taken is forgotten with the field.
    public sealed class FieldTargetDamageService : IFieldTargetDamageService, IFieldTargetReset {
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldTargetPopper _fieldTargetPopper;
        private readonly FieldTargetCatalog _fieldTargetCatalog;
        private readonly FieldTargetDamageModel _fieldTargetDamageModel;
        private readonly FieldTargetConfiguration _fieldTargetConfiguration;

        public FieldTargetDamageService(IFieldTargetQuery fieldTargetQuery, IFieldTargetPopper fieldTargetPopper, FieldTargetCatalog fieldTargetCatalog,
                                        FieldTargetDamageModel fieldTargetDamageModel, FieldTargetConfiguration fieldTargetConfiguration) {
            _fieldTargetQuery = fieldTargetQuery;
            _fieldTargetPopper = fieldTargetPopper;
            _fieldTargetCatalog = fieldTargetCatalog;
            _fieldTargetDamageModel = fieldTargetDamageModel;
            _fieldTargetConfiguration = fieldTargetConfiguration;
        }

        public FieldTargetDamageResult ServerApplyHit(FieldTargetId id, in WeaponHit hit) {
            if (!_fieldTargetQuery.TryGetTarget(id, out FieldTargetDescription target))
                return FieldTargetDamageResult.NotFound;

            if (!IsOnTarget(target, hit))
                return FieldTargetDamageResult.TooFar;

            float damageTaken = _fieldTargetDamageModel.GetDamageTaken(id) + hit.Damage;
            if (damageTaken < _fieldTargetCatalog.GetRequired(target.KindId).HitPoints) {
                _fieldTargetDamageModel.SetDamageTaken(id, damageTaken);
                return FieldTargetDamageResult.Damaged;
            }

            _fieldTargetDamageModel.Forget(id);
            _fieldTargetPopper.ServerPop(id, FieldTargetPopCause.Weapon, hit.Shooter, hit.WeaponId);
            return FieldTargetDamageResult.Popped;
        }

        public void ServerReset() =>
            _fieldTargetDamageModel.Clear();

        private bool IsOnTarget(in FieldTargetDescription target, in WeaponHit hit) {
            float reach = target.Radius + _fieldTargetConfiguration.HitPointToleranceMeters;
            return (hit.Point - target.Center).sqrMagnitude <= reach * reach;
        }
    }
}
