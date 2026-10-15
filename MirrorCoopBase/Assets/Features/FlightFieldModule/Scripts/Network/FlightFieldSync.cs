using Features.FlightFieldModule.Scripts.Network.Generated;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Network {
    // The scene's carrier of the live field model: the server reaches it through IFlightFieldSyncBindingModel.
    // Lives on the run director's object in GameScene with syncInterval 0, so a world shift of the field origin goes
    // out in the same broadcast as the ship's WorldShift.
    public sealed class FlightFieldSync : LiveFieldBridge {
        private IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;

        [Inject]
        private void InjectDependencies(IFlightFieldSyncBindingModel flightFieldSyncBindingModel) =>
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;

        public override void OnStartServer() {
            base.OnStartServer();
            _flightFieldSyncBindingModel.Bind(this);
        }

        public override void OnStopServer() =>
            _flightFieldSyncBindingModel.Clear(this);
    }
}
