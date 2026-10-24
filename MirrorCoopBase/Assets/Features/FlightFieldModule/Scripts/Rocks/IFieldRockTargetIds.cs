using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // A field rock as a weapon target id and back: the same number on every peer, inside the rocks' own id range.
    public interface IFieldRockTargetIds {
        public ulong GetTargetId(FieldRockId id);
        public bool TryGetRockId(ulong targetId, out FieldRockId id);
    }
}
