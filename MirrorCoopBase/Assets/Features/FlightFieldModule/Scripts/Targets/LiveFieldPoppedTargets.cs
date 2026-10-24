using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Targets {
    // The popped target balloons live in the live field model (PoppedTargets), so every peer and a late joiner get
    // them with the field. A field holds a few dozen balloons, so a lookup walks the list (no allocation).
    public sealed class LiveFieldPoppedTargets : IFieldPoppedTargets, IFieldPoppedTargetWriter {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;

        public bool IsAvailable => _flightFieldSyncBindingModel.IsBound;

        public LiveFieldPoppedTargets(IReadOnlyLiveFieldModel liveFieldModel, IFlightFieldSyncBindingModel flightFieldSyncBindingModel) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;
        }

        public bool IsPopped(FieldTargetId id) {
            IReadOnlyList<FieldTargetId> poppedTargets = _liveFieldModel.PoppedTargets;
            for (int i = 0; i < poppedTargets.Count; i++) {
                if (poppedTargets[i].Equals(id))
                    return true;
            }

            return false;
        }

        public void ServerAdd(FieldTargetId id) =>
            _flightFieldSyncBindingModel.Bridge.ServerAddPoppedTargets(id);

        // The bridge is gone when the game scene stops before the run is reset; then there is nothing to clear.
        public void ServerClear() {
            if (_flightFieldSyncBindingModel.IsBound)
                _flightFieldSyncBindingModel.Bridge.ServerClearPoppedTargets();
        }
    }
}
