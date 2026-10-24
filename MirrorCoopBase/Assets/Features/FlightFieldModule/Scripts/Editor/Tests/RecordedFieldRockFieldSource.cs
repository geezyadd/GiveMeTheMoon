using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A peer's rock field source over what a publish job wrote, as the live field model would hold it.
    public sealed class RecordedFieldRockFieldSource : IFieldRockFieldSource {
        private readonly RecordingFieldPublishTarget _published;
        private readonly PublishedFieldRockReader _publishedFieldRockReader;

        public int Number => _published.State.Number;
        public FieldFrame Frame => new(_published.State.Origin, _published.State.Forward, _published.State.CellSize, _published.State.LengthCells, _published.State.WidthCells);

        public RecordedFieldRockFieldSource(RecordingFieldPublishTarget published, PublishedFieldRockReader publishedFieldRockReader) {
            _published = published;
            _publishedFieldRockReader = publishedFieldRockReader;
        }

        public void Read(FieldRockFieldData fieldData) =>
            _publishedFieldRockReader.Read(fieldData, _published.State, _published.Placements, _published.DebrisRuns);
    }
}
