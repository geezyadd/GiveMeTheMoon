using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The broken rocks live in the live field model (BrokenRocks), so every peer and a late joiner get them with the
    // field. A field holds few broken rocks, so a lookup walks the list (no allocation).
    public sealed class LiveFieldBrokenRocks : IFieldBrokenRocks, IFieldBrokenRockWriter {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;

        public bool IsAvailable => _flightFieldSyncBindingModel.IsBound;

        public LiveFieldBrokenRocks(IReadOnlyLiveFieldModel liveFieldModel, IFlightFieldSyncBindingModel flightFieldSyncBindingModel) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;
        }

        public bool IsBroken(FieldRockId id) {
            IReadOnlyList<FieldRockId> brokenRocks = _liveFieldModel.BrokenRocks;
            for (int i = 0; i < brokenRocks.Count; i++) {
                if (brokenRocks[i].Equals(id))
                    return true;
            }

            return false;
        }

        public void ServerAdd(FieldRockId id) =>
            _flightFieldSyncBindingModel.Bridge.ServerAddBrokenRocks(id);

        // The bridge is gone when the game scene stops before the run is reset; then there is nothing to clear.
        public void ServerClear() {
            if (_flightFieldSyncBindingModel.IsBound)
                _flightFieldSyncBindingModel.Bridge.ServerClearBrokenRocks();
        }
    }
}
