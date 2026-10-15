using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Network {
    public sealed class FlightFieldSyncBindingModel : IFlightFieldSyncBindingModel {
        public LiveFieldBridge Bridge { get; private set; }
        public bool IsBound => Bridge != null;

        public void Bind(LiveFieldBridge bridge) =>
            Bridge = bridge;

        // Only the bridge that is bound may clear: a stopping old scene must not unbind a new one.
        public void Clear(LiveFieldBridge bridge) {
            if (Bridge == bridge)
                Bridge = null;
        }
    }
}
