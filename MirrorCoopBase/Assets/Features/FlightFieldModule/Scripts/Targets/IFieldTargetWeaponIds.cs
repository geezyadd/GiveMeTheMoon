using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // A target balloon id <-> its weapon target id.
    public interface IFieldTargetWeaponIds {
        public ulong GetTargetId(FieldTargetId id);
        public bool TryGetBalloonId(ulong targetId, out FieldTargetId id);
    }
}
