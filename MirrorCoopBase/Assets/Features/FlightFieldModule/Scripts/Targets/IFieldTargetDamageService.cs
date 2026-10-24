using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    public interface IFieldTargetDamageService {
        public FieldTargetDamageResult ServerApplyHit(FieldTargetId id, in WeaponHit hit);
    }
}
