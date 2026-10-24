using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: a weapon hit on a rock of the current field. The rock is checked against the field data, not the scene:
    // it exists, is not broken, and the reported point lies on it. A breakable rock adds the hit's damage to what it
    // took; once that reaches its hit points it breaks through the one break entry, as a Weapon break by the shooter
    // with the weapon's id. The damage taken is forgotten with the field.
    public sealed class FieldRockDamageService : IFieldRockDamageService, IFieldRockReset {
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldRockBreaker _fieldRockBreaker;
        private readonly IFieldRockHitPoints _fieldRockHitPoints;
        private readonly FieldRockDamageModel _fieldRockDamageModel;
        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;

        public FieldRockDamageService(IFieldRockQuery fieldRockQuery, IFieldRockBreaker fieldRockBreaker, IFieldRockHitPoints fieldRockHitPoints,
                                      FieldRockDamageModel fieldRockDamageModel, FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) {
            _fieldRockQuery = fieldRockQuery;
            _fieldRockBreaker = fieldRockBreaker;
            _fieldRockHitPoints = fieldRockHitPoints;
            _fieldRockDamageModel = fieldRockDamageModel;
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;
        }

        public FieldRockDamageResult ServerApplyHit(FieldRockId id, in WeaponHit hit) {
            if (!_fieldRockQuery.TryGetRock(id, out FieldRockDescription rock))
                return FieldRockDamageResult.NotFound;

            if (!IsOnRock(rock, hit))
                return FieldRockDamageResult.TooFar;

            if (!rock.IsBreakable)
                return FieldRockDamageResult.Unbreakable;

            float damageTaken = _fieldRockDamageModel.GetDamageTaken(id) + hit.Damage;
            if (damageTaken < _fieldRockHitPoints.GetHitPoints(rock)) {
                _fieldRockDamageModel.SetDamageTaken(id, damageTaken);
                return FieldRockDamageResult.Damaged;
            }

            _fieldRockDamageModel.Forget(id);
            _fieldRockBreaker.ServerBreak(id, FieldRockDestroyCause.Weapon, hit.Shooter, hit.WeaponId);
            return FieldRockDamageResult.Broken;
        }

        public void ServerReset() =>
            _fieldRockDamageModel.Clear();

        private bool IsOnRock(in FieldRockDescription rock, in WeaponHit hit) {
            float reach = rock.Radius + _fieldRockDurabilityConfiguration.HitPointToleranceMeters;
            return (hit.Point - rock.Center).sqrMagnitude <= reach * reach;
        }
    }
}
