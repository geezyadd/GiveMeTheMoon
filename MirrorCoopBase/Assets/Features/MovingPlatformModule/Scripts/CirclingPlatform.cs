using Features.GameCoreModule.Contracts;
using Features.FloatingControllerModule;
using Mirror;
using UnityEngine;

namespace Features.MovingPlatformModule {
    // A test platform that flies around a circle (around the lobby) and turns along its way, for trying out walking and
    // jumping on a moving platform. Only three values are synced: running or not, and the angle and server time of the
    // last start or stop; every peer computes the pose from the server time (CirclePath, PlatformSway: it also rocks
    // from side to side like a deck), so it moves the same and
    // smoothly everywhere. The pose is set each physics step on a kinematic, interpolated body, before the riders move
    // (execution order), and the platform carries what stands on it (ICarrier).
    [DefaultExecutionOrder(-200)]
    public sealed class CirclingPlatform : NetworkBehaviour, ICarrier {
        [SerializeField] private Rigidbody _body;
        [Tooltip("The deck's collider: its size is the space a body rides in.")]
        [SerializeField] private BoxCollider _deck;
        [Tooltip("The middle of the circle, world position; its height is the platform's.")]
        [SerializeField] private Vector3 _center;
        [Tooltip("Radius of the circle, metres.")]
        [SerializeField, Min(0.1f)] private float _radius = 20f;
        [Tooltip("Speed along the circle, metres a second.")]
        [SerializeField, Min(0f)] private float _speed = 50f;
        [Tooltip("Largest roll from side to side, degrees.")]
        [SerializeField, Min(0f)] private float _swayRollDegrees = 12f;
        [Tooltip("Largest pitch forward and back, degrees.")]
        [SerializeField, Min(0f)] private float _swayPitchDegrees = 6f;
        [Tooltip("One full rock, seconds.")]
        [SerializeField, Min(0f)] private float _swayPeriodSeconds = 3f;
        [Tooltip("Its clock may run this share faster or slower to meet the server time again after a stall (0.05 = 5 %).")]
        [SerializeField, Range(0f, 0.5f)] private float _clockMaxRateChange = 0.05f;
        [Tooltip("How fast the clock closes a gap to the server time: the share of the gap per second, before the limit above.")]
        [SerializeField, Min(0f)] private float _clockCatchUpPerSecond = 1f;
        [Tooltip("A gap to the server time this long, seconds, is closed at once (a long stall).")]
        [SerializeField, Min(0.05f)] private float _clockJumpGap = 0.5f;
        [Tooltip("How high above the platform a body in the air still rides with it, metres (a jump).")]
        [SerializeField, Min(0f)] private float _rideHeight = 6f;
        [Tooltip("How far past the platform's edges a body in the air still rides with it, metres.")]
        [SerializeField, Min(0f)] private float _rideEdgeMargin = 0.3f;

        // Stands at the start; the switch on its deck starts it.
        [SyncVar] private bool _isRunning;
        [SyncVar] private float _angleAtChange;
        [SyncVar] private double _changedAt;

        // The server time the platform moves by: even steps, eased toward the network time (see SmoothServerClock).
        private SmoothServerClock _clock;

        public bool IsRunning => _isRunning;
        public uint CarrierId => netId;
        public Pose PoseBefore { get; private set; }
        public Pose PoseNow { get; private set; }
        public Quaternion DrawnRotation => _body.transform.rotation;

        private float AngularSpeed => _speed / _radius;

        private void Awake() {
            _clock = new SmoothServerClock(_clockMaxRateChange, _clockCatchUpPerSecond, _clockJumpGap);
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            Pose start = PoseAt(_angleAtChange, 0d);
            PoseBefore = start;
            PoseNow = start;
            _body.position = start.position;
            _body.rotation = start.rotation;
            _body.transform.SetPositionAndRotation(start.position, start.rotation);
        }

        private void Update() {
            if (NetworkClient.active == false && NetworkServer.active == false)
                return;

            if (_clock.IsStarted == false) {
                _clock.Start(NetworkTime.time);
                return;
            }

            // The clock stands at the last physics step; the frame is a little later.
            double clockNow = _clock.Time + (Time.timeAsDouble - Time.fixedTimeAsDouble);
            _clock.Compare(NetworkTime.time, clockNow);
        }

        private void FixedUpdate() {
            PoseBefore = PoseNow;
            if (_clock.IsStarted == false)
                return;

            _clock.Step(Time.fixedDeltaTime);
            double serverTime = _clock.Time;
            PoseNow = PoseAt(CirclePath.AngleAt(_isRunning, _angleAtChange, _changedAt, AngularSpeed, serverTime), serverTime);
            _body.MovePosition(PoseNow.position);
            _body.MoveRotation(PoseNow.rotation);
        }

        public bool Holds(Vector3 worldPoint) {
            // The bodies on it stand where the last step carried them, so the point is read in that step's pose: in this
            // step's, a fast platform is already a step ahead and a body at its trailing edge looked off it.
            Vector3 local = CarrierMotion.ToLocalPoint(PoseBefore, worldPoint);
            Vector3 half = _deck.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x + _rideEdgeMargin
                && Mathf.Abs(local.z) <= half.z + _rideEdgeMargin
                && local.y >= -half.y
                && local.y <= half.y + _rideHeight;
        }

        [Server]
        public void ServerToggle() {
            double now = _clock.IsStarted ? _clock.Time : NetworkTime.time;
            _angleAtChange = CirclePath.AngleAt(_isRunning, _angleAtChange, _changedAt, AngularSpeed, now);
            _changedAt = now;
            _isRunning = _isRunning == false;
        }

        // The heading along the circle, rocked by the sway (which goes on while the platform stands too).
        private Pose PoseAt(float angle, double serverTime) =>
            new(
                CirclePath.PositionAt(_center, _radius, angle),
                CirclePath.RotationAt(angle) * PlatformSway.TiltAt(serverTime, _swayRollDegrees, _swayPitchDegrees, _swayPeriodSeconds));
    }
}
