using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Network {
    // The server's live field bridge while the game scene runs; the bridge binds and clears itself.
    public interface IFlightFieldSyncBindingModel {
        public LiveFieldBridge Bridge { get; }
        public bool IsBound { get; }
        public void Bind(LiveFieldBridge bridge);
        public void Clear(LiveFieldBridge bridge);
    }
}
