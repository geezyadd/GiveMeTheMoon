using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipFlightControl : IShipFlightControl
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IShipFlightControlDebug
#endif
    {
        private const float DIRECTION_EPSILON = 0.0001f;
        private const float SIMULATION_STEP = 1f / 60f;

        private readonly ShipFlight _flight = new ShipFlight();
        // Read on use: the settings can be injected after the parts are created.
        private readonly System.Func<ShipFlightSettings> _flightSettings;
        private readonly IShipModules _modules;
        private readonly IShipSeats _seats;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _debugSteerActive;
        private float _debugSteer;
#endif
        private bool _controlsLocked;

        public ShipFlightMode Mode => _flight.Mode;
        public bool IsTakeoffComplete => _flight.IsTakeoffComplete;
        public bool HasLanded => _flight.HasLanded;
        internal bool IsActive => _flight.IsActive;
        internal Vector3 Position => _flight.Position;
        internal Quaternion Rotation => _flight.Rotation;
        internal Vector3 Velocity => _flight.Velocity;

        internal ShipFlightControl(System.Func<ShipFlightSettings> flightSettings, IShipModules modules, IShipSeats seats) {
            _flightSettings = flightSettings;
            _modules = modules;
            _seats = seats;
        }

        internal void BeginTakeoff(
            Vector3 from,
            Vector3 hover,
            Quaternion heading,
            Quaternion routeHeading,
            float takeoffSeconds,
            float dodgeRangeScale,
            ShipFlightMode mode) {
            _flight.BeginTakeoff(
                from,
                hover,
                heading,
                routeHeading,
                _flightSettings(),
                takeoffSeconds,
                dodgeRangeScale,
                mode);
            _controlsLocked = false;
        }

        internal void BeginCruise() {
            _controlsLocked = false;
            _flight.BeginCruise();
        }

        internal void BeginLanding(Vector3 padPoint, float landingSeconds, Vector3 faceDirection, Quaternion currentRotation) {
            LockControls();
            Vector3 flat = faceDirection;
            flat.y = 0f;
            Quaternion heading = flat.sqrMagnitude > DIRECTION_EPSILON
                ? Quaternion.LookRotation(flat.normalized, Vector3.up)
                : currentRotation;
            _flight.BeginLanding(padPoint, landingSeconds, heading);
        }

        public void LockControls() {
            _controlsLocked = true;
            _flight.SetManualHeading(false);
            _flight.SetSteer(0f);
        }

        internal void UnlockControls() =>
            _controlsLocked = false;

        internal void Stop() =>
            _flight.Stop();

        internal void ShiftWorld(Vector3 delta) =>
            _flight.ShiftWorld(delta);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetDebugSteer(bool active, float steer) {
            _debugSteerActive = active;
            _debugSteer = Mathf.Clamp(steer, -1f, 1f);
        }

        public void DebugFace(Vector3 worldForward) =>
            _flight.DebugFace(worldForward);
#endif

        internal void ServerStep(float dt) {
            bool canSteer = _controlsLocked == false && _flight.AllowsSteer;
            bool debugSteering = TryReadDebugSteer(out float debugSteer);
            bool manual = canSteer && (_modules.HasControlModule() || debugSteering);
            _flight.SetManualHeading(manual);
            float steer = debugSteering
                ? debugSteer
                : canSteer && _seats.HasHelmPilot() ? _seats.ReadHelmSteer() : 0f;
            _flight.SetSteer(steer);
            PushTravelSpeed();
            SimulateFlight(dt);
        }

        private bool TryReadDebugSteer(out float steer) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            steer = _debugSteer;
            return _debugSteerActive;
#else
            steer = 0f;
            return false;
#endif
        }

        private void PushTravelSpeed() {
            if (_flight.Mode != ShipFlightMode.TravelInSpace)
                return;

            float stat = Mathf.Max(ShipTransit.MinSpeed, _modules.GetStatFull(ShipStatType.FlightSpeed));
            _flight.SetTravelSpeed(_flightSettings().CruiseSpeed * stat);
        }

        private void SimulateFlight(float dt) {
            float left = dt;
            while (left > 0f) {
                float slice = Mathf.Min(SIMULATION_STEP, left);
                _flight.Simulate(slice);
                left -= slice;
            }
        }
    }
}
