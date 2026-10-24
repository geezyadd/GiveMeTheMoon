using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Every peer: the balloons of the field the network published in the live field model.
    public sealed class LiveFieldTargetFieldSource : IFieldTargetFieldSource {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;

        public bool HasField => _liveFieldModel.IsAvailable && _liveFieldModel.Number != LiveFieldNumbers.NO_FIELD;
        public FieldFrame Frame => new(_liveFieldModel.Origin, _liveFieldModel.Forward, _liveFieldModel.CellSize, _liveFieldModel.LengthCells, _liveFieldModel.WidthCells);
        public int TargetCount => _liveFieldModel.Targets.Count;

        public LiveFieldTargetFieldSource(IReadOnlyLiveFieldModel liveFieldModel) =>
            _liveFieldModel = liveFieldModel;

        public FieldTargetPlacement GetTarget(int index) {
            FieldTargetEntry entry = _liveFieldModel.Targets[index];
            return new FieldTargetPlacement(entry.KindId, entry.FieldPosition);
        }
    }
}
