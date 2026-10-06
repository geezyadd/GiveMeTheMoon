using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Steps the ship's flight simulation on the server and feeds it the helm controls: heading mode, steer, speed.
    internal sealed class ShipFlightControl {
        private readonly ShipFlight _flight = new ShipFlight();
        private readonly ShipFlightSettings _flightSettings;
        private readonly ShipModules _modules;
        private readonly ShipSeats _seats;

        private bool _debugSteerActive;
        private float _debugSteer;
        private bool _controlsLocked;

        internal ShipFlightMode Mode => _flight.Mode;
        internal bool IsTakeoffComplete => _flight.IsTakeoffComplete;
        internal bool HasLanded => _flight.HasLanded;
        internal bool IsActive => _flight.IsActive;
        internal Vector3 Position => _flight.Position;
        internal Quaternion Rotation => _flight.Rotation;
        internal Vector3 Velocity => _flight.Velocity;

        internal ShipFlightControl(ShipFlightSettings flightSettings, ShipModules modules, ShipSeats seats) {
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
                _flightSettings,
                takeoffSeconds,
                dodgeRangeScale,
                mode);
            _seats.ClearSteer();
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
            Quaternion heading = flat.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(flat.normalized, Vector3.up)
                : currentRotation;
            _flight.BeginLanding(padPoint, landingSeconds, heading);
        }

        internal void LockControls() {
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

        internal void SetDebugSteer(bool active, float steer) {
            _debugSteerActive = active;
            _debugSteer = Mathf.Clamp(steer, -1f, 1f);
        }

        internal void DebugFace(Vector3 worldForward) =>
            _flight.DebugFace(worldForward);

        internal void ServerStep(float dt) {
            bool canSteer = _controlsLocked == false && _flight.AllowsSteer;
            bool manual = canSteer && (_modules.HasControlModule() || _debugSteerActive);
            _flight.SetManualHeading(manual);
            float steer = _debugSteerActive
                ? _debugSteer
                : canSteer && _seats.HasHelmPilot() ? _seats.ReadHelmSteer() : 0f;
            _flight.SetSteer(steer);
            PushTravelSpeed();
            SimulateFlight(dt);
        }

        private void PushTravelSpeed() {
            if (_flight.Mode != ShipFlightMode.TravelInSpace || _flightSettings == null)
                return;

            float stat = Mathf.Max(ShipTransit.MinSpeed, _modules.GetStatFull(ShipStatType.FlightSpeed));
            _flight.SetTravelSpeed(_flightSettings.CruiseSpeed * stat);
        }

        private void SimulateFlight(float dt) {
            const float step = 1f / 60f;
            float left = dt;
            while (left > 0f) {
                float slice = Mathf.Min(step, left);
                _flight.Simulate(slice);
                left -= slice;
            }
        }
    }
}
