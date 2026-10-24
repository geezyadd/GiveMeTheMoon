using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The broken rocks of the current field, on every peer.
    public interface IFieldBrokenRocks {
        public bool IsBroken(FieldRockId id);
    }
}
