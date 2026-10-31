using Features.ShipModule.Scripts.Throttle;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    public sealed class RecordingThrottleStop : IShipThrottleStop {
        public int Stops { get; private set; }

        public void ServerStop() =>
            Stops++;
    }
}
