using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IFieldRockDamageService {
        public FieldRockDamageResult ServerApplyHit(FieldRockId id, in WeaponHit hit);
    }
}
