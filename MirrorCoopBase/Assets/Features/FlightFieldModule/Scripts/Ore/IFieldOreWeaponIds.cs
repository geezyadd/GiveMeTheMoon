using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Ore {
    public interface IFieldOreWeaponIds {
        public ulong GetTargetId(FieldOreId id);
        public bool TryGetOreId(ulong targetId, out FieldOreId id);
    }
}
