using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Network {
    // Writes a generated field into the live field model: the placements are the content every peer builds from, the
    // routes are kept for the debug map and later features. A long field is tens of kilobytes, above the Telepathy
    // message limit (16 KB), so it goes out in batches (FieldPublishJob), one batch per publish interval, which spans
    // several network broadcasts, so every Mirror delta carries at most one batch. The scalar state with the new
    // number goes last. A new field or Clear drops a publish still in progress.
    public sealed class FlightFieldPublisher : IFlightFieldPublisher, ITickable {
        private readonly IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        private FieldPublishJob _job;
        private float _nextBatchTime;

        public FlightFieldPublisher(IFlightFieldSyncBindingModel flightFieldSyncBindingModel, FlightFieldConfiguration flightFieldConfiguration) {
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public void Publish(FlightField field, int number) {
            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            bridge.ServerSetState(default);
            ClearLists(bridge);
            _job = new FieldPublishJob(field, number, _flightFieldConfiguration.PublishEntriesPerBatch);
            WriteNextBatch();
        }

        public void Tick() {
            if (_job != null && Time.unscaledTime >= _nextBatchTime)
                WriteNextBatch();
        }

        // The bridge is gone when the game scene stops before the run is reset; then there is nothing to clear.
        public void Clear() {
            DropJob();
            if (!_flightFieldSyncBindingModel.IsBound)
                return;

            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            bridge.ServerSetState(default);
            ClearLists(bridge);
        }

        public void ShiftOrigin(Vector3 delta) {
            _job?.ShiftOrigin(delta);
            if (!_flightFieldSyncBindingModel.IsBound)
                return;

            LiveFieldBridge bridge = _flightFieldSyncBindingModel.Bridge;
            LiveFieldState state = bridge.State;
            if (state.Number == LiveFieldNumbers.NO_FIELD)
                return;

            state.Origin += delta;
            bridge.ServerSetState(state);
        }

        // The bridge can stop with the game scene while a publish is in progress: the publish ends with it.
        private void WriteNextBatch() {
            if (!_flightFieldSyncBindingModel.IsBound) {
                DropJob();
                return;
            }

            _job.WriteNextBatch(new LiveFieldBridgePublishTarget(_flightFieldSyncBindingModel.Bridge));
            _nextBatchTime = Time.unscaledTime + _flightFieldConfiguration.PublishBatchIntervalSeconds;
            if (_job.IsDone)
                DropJob();
        }

        private void DropJob() =>
            _job = null;

        private static void ClearLists(LiveFieldBridge bridge) {
            bridge.ServerClearPlacements();
            bridge.ServerClearRoutes();
            bridge.ServerClearRouteCells();
        }
    }
}
