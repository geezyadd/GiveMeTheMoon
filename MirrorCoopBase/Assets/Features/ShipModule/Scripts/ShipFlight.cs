using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipFlight {
        private enum Phase {
            Idle,
            Takeoff,
            Cruise,
            Landing
        }

        private ShipFlightSettings _settings;
        private Vector3 _from;
        private Vector3 _hover;
        private Vector3 _landTo;
        private Vector3 _position;
        private Vector3 _velocity;
        private Quaternion _restHeading = Quaternion.identity;
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

        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public void BeginTakeoff(
            Vector3 from,
            Vector3 hover,
            Quaternion heading,
            ShipFlightSettings settings,
            float takeoffSeconds,
            float dodgeRangeScale) {
            _settings = settings;
            _from = from;
            _hover = hover;
            _landTo = hover;
            _position = from;
            _velocity = Vector3.zero;
            _steer = 0f;
            _dodge = 0f;
            _dodgeVel = 0f;
            _dodgeRangeScale = Mathf.Max(0.1f, dodgeRangeScale);
            _yaw = 0f;
            _yawVel = 0f;
            _bank = 0f;
            _bankVel = 0f;
            _takeoffAge = 0f;
            _takeoffSeconds = Mathf.Max(0.2f, takeoffSeconds);
            _landAge = 0f;
            _landed = false;
            _restHeading = heading;
            Rotation = _restHeading;
            _phase = Phase.Takeoff;
        }

        public void BeginCruise() {
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

        public void Stop() {
            _phase = Phase.Idle;
            _velocity = Vector3.zero;
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
            ApplyAttitude(dt, true);
            CommitPose(hover + CruiseDodge(), dt);
        }

        private void SimulateCruise(float dt) {
            if (_manualHeading && Mathf.Abs(_steer) >= 0.08f) {
                float turnRate = _settings != null ? _settings.TurnRate : 80f;
                _restHeading = Quaternion.AngleAxis(_steer * turnRate * dt, Vector3.up) * _restHeading;
                Vector3 forward = _restHeading * Vector3.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                    _restHeading = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            ApplyAttitude(dt, true);
            CommitPose(_hover + CruiseDodge(), dt);
        }

        private void SimulateLanding(float dt) {
            _landAge += dt;
            float t = LandT;
            _restHeading = Quaternion.Slerp(_landFromHeading, _landHeading, AlignT);
            ApplyAttitude(dt, false);
            CommitPose(Vector3.Lerp(_from, _landTo, t), dt);
            if (t < 1f)
                return;

            _position = _landTo;
            _velocity = Vector3.zero;
            _restHeading = _landHeading;
            _landed = true;
        }

        private void ApplyAttitude(float dt, bool allowSteer) {
            float held = allowSteer && _manualHeading ? Mathf.Abs(_steer) : 0f;
            float stiffness = _settings != null ? _settings.SwayStiffness : 10f;
            float restDamping = _settings != null ? _settings.SwayDamping : 0.32f;
            float damping = Mathf.Lerp(restDamping, 0.78f, Mathf.SmoothStep(0f, 1f, held));

            float yawTarget = 0f;
            Spring(ref _yaw, ref _yawVel, yawTarget, stiffness, damping, dt);

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
