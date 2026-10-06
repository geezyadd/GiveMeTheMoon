using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipFlight {
        private enum Phase {
            Idle,
            Takeoff,
            Cruise,
            Landing
        }

        private const float ARRIVE_DISTANCE = 0.4f;
        private const float ARRIVE_SPEED_SQR = 0.25f;

        private ShipFlightSettings _settings;
        private ShipFlightMode _mode = ShipFlightMode.TravelInSpace;
        private Vector3 _from;
        private Vector3 _hover;
        private Vector3 _landTo;
        private Vector3 _position;
        private Vector3 _velocity;
        private Vector3 _routeAnchor;
        private Vector3 _landVelocity;
        private Quaternion _restHeading = Quaternion.identity;
        private Quaternion _launchHeading = Quaternion.identity;
        private Quaternion _routeHeading = Quaternion.identity;
        private Quaternion _landFromHeading = Quaternion.identity;
        private Quaternion _landHeading = Quaternion.identity;
        private float _yaw;
        private float _yawVel;
        private float _bank;
        private float _bankVel;
        private float _steer;
        private float _dodge;
        private float _dodgeVel;
        private float _dodgeRangeScale = 1f;
        private float _travelSpeed;
        private float _cruiseAge;
        private float _takeoffAge;
        private float _takeoffSeconds = 9f;
        private float _landAge;
        private float _landingSeconds = 9f;
        private bool _manualHeading;
        private bool _landed;
        private Phase _phase = Phase.Idle;

        public bool IsActive => _phase != Phase.Idle;
        public bool HasLanded => _landed;
        public bool IsTakeoffComplete => _phase == Phase.Takeoff && _takeoffAge >= _takeoffSeconds;
        public bool AllowsSteer => _phase == Phase.Cruise;
        public ShipFlightMode Mode => _mode;

        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public void BeginTakeoff(
            Vector3 from,
            Vector3 hover,
            Quaternion heading,
            Quaternion routeHeading,
            ShipFlightSettings settings,
            float takeoffSeconds,
            float dodgeRangeScale,
            ShipFlightMode mode) {
            _settings = settings;
            _mode = mode == ShipFlightMode.None ? ShipFlightMode.TravelInSpace : mode;
            _from = from;
            _hover = hover;
            _landTo = hover;
            _position = from;
            _routeAnchor = hover;
            _velocity = Vector3.zero;
            _landVelocity = Vector3.zero;
            _steer = 0f;
            _dodge = 0f;
            _dodgeVel = 0f;
            _dodgeRangeScale = Mathf.Max(0.1f, dodgeRangeScale);
            _yaw = 0f;
            _yawVel = 0f;
            _bank = 0f;
            _bankVel = 0f;
            _travelSpeed = 0f;
            _cruiseAge = 0f;
            _takeoffAge = 0f;
            _takeoffSeconds = Mathf.Max(0.2f, takeoffSeconds);
            _landAge = 0f;
            _landed = false;
            _launchHeading = FlattenHeading(heading);
            _routeHeading = FlattenHeading(routeHeading);
            _restHeading = _launchHeading;
            Rotation = _restHeading;
            _phase = Phase.Takeoff;
        }

        public void BeginCruise() {
            _cruiseAge = 0f;
            _routeAnchor = _hover;
            if (_mode == ShipFlightMode.TravelInSpace)
                _restHeading = _routeHeading;

            _phase = Phase.Cruise;
        }

        public void BeginLanding(Vector3 padPoint, float landingSeconds) {
            BeginLanding(padPoint, landingSeconds, _restHeading);
        }

        public void BeginLanding(Vector3 padPoint, float landingSeconds, Quaternion restHeading) {
            _landFromHeading = FlattenHeading(_restHeading);
            _landHeading = FlattenHeading(restHeading);
            _restHeading = _landFromHeading;
            _manualHeading = false;
            _steer = 0f;
            _from = _position;
            _landTo = padPoint;
            _landVelocity = _velocity;
            _landAge = 0f;
            _landingSeconds = Mathf.Max(0.2f, landingSeconds);
            _landed = false;
            _phase = Phase.Landing;
        }

        public void SetSteer(float lateral) {
            float clamped = Mathf.Clamp(lateral, -1f, 1f);
            _steer = Mathf.Abs(clamped) < 0.08f ? 0f : clamped;
        }

        public void SetManualHeading(bool manual) {
            _manualHeading = manual;
        }

        public void SetTravelSpeed(float metersPerSecond) {
            _travelSpeed = Mathf.Max(0f, metersPerSecond);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugFace(Vector3 worldForward) {
            Vector3 flat = worldForward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                return;

            _restHeading = Quaternion.LookRotation(flat.normalized, Vector3.up);
            _routeHeading = _restHeading;
        }
#endif

        public void ShiftWorld(Vector3 delta) {
            _from += delta;
            _hover += delta;
            _landTo += delta;
            _position += delta;
            _routeAnchor += delta;
        }

        public void Stop() {
            _phase = Phase.Idle;
            _velocity = Vector3.zero;
            _landVelocity = Vector3.zero;
            _landed = false;
        }

        public void Simulate(float dt) {
            if (IsActive == false || dt <= 0f)
                return;

            if (_phase == Phase.Takeoff)
                SimulateTakeoff(dt);
            else if (_phase == Phase.Cruise)
                SimulateCruise(dt);
            else if (_phase == Phase.Landing)
                SimulateLanding(dt);
        }

        private void SimulateTakeoff(float dt) {
            _takeoffAge += dt;
            Vector3 hover = Vector3.Lerp(_from, _hover, TakeoffT);
            if (_mode == ShipFlightMode.TravelInSpace)
                _restHeading = Quaternion.Slerp(_launchHeading, _routeHeading, TakeoffT);

            ApplyAttitude(dt, true);
            CommitPose(hover + CruiseDodge(), dt);
        }

        private void SimulateCruise(float dt) {
            if (_manualHeading && Mathf.Abs(_steer) >= 0.08f) {
                float turnRate = _settings != null ? _settings.TurnRate : 80f;
                _restHeading = Quaternion.AngleAxis(_steer * turnRate * dt, Vector3.up) * _restHeading;
                _restHeading = FlattenHeading(_restHeading);
            }

            ApplyAttitude(dt, true);
            if (_mode == ShipFlightMode.TravelInSpace)
                AdvanceRoute(dt);

            CommitPose(CruiseAnchor() + CruiseDodge(), dt);
        }

        private void AdvanceRoute(float dt) {
            _cruiseAge += dt;
            float accelSeconds = _settings != null ? _settings.TravelAccelSeconds : 0f;
            float ramp = accelSeconds <= 0.01f ? 1f : Mathf.Clamp01(_cruiseAge / accelSeconds);
            Vector3 forward = _restHeading * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            _routeAnchor += forward.normalized * (_travelSpeed * ramp) * dt;
        }

        private Vector3 CruiseAnchor() {
            return _mode == ShipFlightMode.TravelInSpace ? _routeAnchor : _hover;
        }

        private void SimulateLanding(float dt) {
            _landAge += dt;
            float align = AlignT;
            _restHeading = Quaternion.Slerp(_landFromHeading, _landHeading, align);
            ApplyAttitude(dt, false);
            if (_mode == ShipFlightMode.TravelInSpace) {
                SimulateTravelLanding(dt);
                return;
            }

            float t = LandT;
            CommitPose(Vector3.Lerp(_from, _landTo, t), dt);
            if (t < 1f)
                return;

            _position = _landTo;
            _velocity = Vector3.zero;
            _restHeading = _landHeading;
            Rotation = _restHeading * Quaternion.Euler(0f, _yaw, _bank);
            _landed = true;
        }

        private void SimulateTravelLanding(float dt) {
            float smoothTime = Mathf.Clamp(_landingSeconds * 0.28f, 0.45f, 2.2f);
            Vector3 next = Vector3.SmoothDamp(_position, _landTo, ref _landVelocity, smoothTime, Mathf.Infinity, dt);
            CommitPose(next, dt);
            float remain = HorizontalDistance(_position, _landTo);
            bool arrived = remain < ARRIVE_DISTANCE && _landVelocity.sqrMagnitude < ARRIVE_SPEED_SQR;
            bool timedOut = _landAge >= _landingSeconds * 1.5f;
            if (arrived == false && timedOut == false)
                return;

            _position = _landTo;
            _velocity = Vector3.zero;
            _landVelocity = Vector3.zero;
            _restHeading = _landHeading;
            Rotation = _restHeading;
            _landed = true;
        }

        private void ApplyAttitude(float dt, bool allowSteer) {
            float held = allowSteer && _manualHeading ? Mathf.Abs(_steer) : 0f;
            float stiffness = _settings != null ? _settings.SwayStiffness : 10f;
            float restDamping = _settings != null ? _settings.SwayDamping : 0.32f;
            float damping = Mathf.Lerp(restDamping, 0.78f, Mathf.SmoothStep(0f, 1f, held));

            Spring(ref _yaw, ref _yawVel, 0f, stiffness, damping, dt);

            float dodgeRange = (_settings != null ? _settings.DodgeRange : 4f) * _dodgeRangeScale;
            float dodgeTarget = allowSteer && _manualHeading ? _steer * dodgeRange : 0f;
            Spring(ref _dodge, ref _dodgeVel, dodgeTarget, stiffness, damping, dt);

            float bankDegrees = _settings != null ? _settings.BankDegrees : 22f;
            float bankTarget = allowSteer && _manualHeading ? -_steer * bankDegrees : 0f;
            Spring(ref _bank, ref _bankVel, bankTarget, stiffness * 1.15f, damping, dt);
        }

        private Vector3 CruiseDodge() {
            return _restHeading * Vector3.right * _dodge;
        }

        private void CommitPose(Vector3 next, float dt) {
            _velocity = dt > 0.0001f ? (next - _position) / dt : Vector3.zero;
            _position = next;
            Rotation = _restHeading * Quaternion.Euler(0f, _yaw, _bank);
        }

        private float TakeoffT => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_takeoffAge / _takeoffSeconds));

        private float LandT => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_landAge / _landingSeconds));

        private float AlignT {
            get {
                float duration = Mathf.Clamp(_landingSeconds * 0.4f, 1.25f, 3f);
                duration = Mathf.Min(duration, _landingSeconds);
                return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_landAge / duration));
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static Quaternion FlattenHeading(Quaternion rotation) {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static void Spring(
            ref float value,
            ref float velocity,
            float target,
            float stiffness,
            float damping,
            float dt) {
            float accel = (target - value) * stiffness - velocity * (2f * Mathf.Sqrt(stiffness) * damping);
            velocity += accel * dt;
            value += velocity * dt;
        }
    }
}
