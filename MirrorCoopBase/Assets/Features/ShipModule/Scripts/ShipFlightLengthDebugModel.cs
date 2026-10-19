#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Editor / development only: a fixed flight length in cruise seconds for every loop while above 0.
    public sealed class ShipFlightLengthDebugModel : IShipFlightLengthDebug {
        public float FlightSeconds { get; private set; }

        public void DebugUseFlightSeconds(float seconds) =>
            FlightSeconds = Mathf.Max(0f, seconds);

        public void DebugClearFlightSeconds() =>
            FlightSeconds = 0f;
    }
}
#endif
