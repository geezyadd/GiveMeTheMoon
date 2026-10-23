using Features.FlightFieldModule.Scripts.Decor;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A fixed volume source in place of the live field model.
    public sealed class FieldTestVolumeSourceReader : IFieldVolumeSourceReader {
        public FieldVolumeSource Source { get; set; }

        public bool TryRead(out FieldVolumeSource source) {
            source = Source;
            return true;
        }
    }
}
