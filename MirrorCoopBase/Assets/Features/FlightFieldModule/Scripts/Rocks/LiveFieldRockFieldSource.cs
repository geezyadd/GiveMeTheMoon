using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Every peer: the field as the network published it in the live field model. Its number turns to the new field
    // only after the last batch, so a field is read whole.
    public sealed class LiveFieldRockFieldSource : IFieldRockFieldSource {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly PublishedFieldRockReader _publishedFieldRockReader;

        public int Number => _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
        public FieldFrame Frame => new(_liveFieldModel.Origin, _liveFieldModel.Forward, _liveFieldModel.CellSize, _liveFieldModel.LengthCells, _liveFieldModel.WidthCells);

        public LiveFieldRockFieldSource(IReadOnlyLiveFieldModel liveFieldModel, PublishedFieldRockReader publishedFieldRockReader) {
            _liveFieldModel = liveFieldModel;
            _publishedFieldRockReader = publishedFieldRockReader;
        }

        public void Read(FieldRockFieldData fieldData) =>
            _publishedFieldRockReader.Read(fieldData, ReadState(), _liveFieldModel.Placements, _liveFieldModel.DebrisRuns);

        private LiveFieldState ReadState() =>
            new() {
                Number = _liveFieldModel.Number,
                Seed = _liveFieldModel.Seed,
                Origin = _liveFieldModel.Origin,
                Forward = _liveFieldModel.Forward,
                CellSize = _liveFieldModel.CellSize,
                LengthCells = _liveFieldModel.LengthCells,
                WidthCells = _liveFieldModel.WidthCells,
                ProfileId = _liveFieldModel.ProfileId
            };
    }
}
