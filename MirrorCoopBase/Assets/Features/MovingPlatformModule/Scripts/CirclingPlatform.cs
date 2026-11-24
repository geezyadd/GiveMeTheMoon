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
        [SerializeField, Min(0f)] private float _speed = 9f;
        [Tooltip("Largest roll from side to side, degrees.")]
        [SerializeField, Min(0f)] private float _swayRollDegrees = 12f;
        [Tooltip("Largest pitch forward and back, degrees.")]
        [SerializeField, Min(0f)] private float _swayPitchDegrees = 6f;
        [Tooltip("One full rock, seconds.")]
        [SerializeField, Min(0f)] private float _swayPeriodSeconds = 3f;
        [Tooltip("How high above the platform a body in the air still rides with it, metres (a jump).")]
        [SerializeField, Min(0f)] private float _rideHeight = 6f;
        [Tooltip("How far past the platform's edges a body in the air still rides with it, metres.")]
        [SerializeField, Min(0f)] private float _rideEdgeMargin = 0.3f;

        // Stands at the start; the switch on its deck starts it.
        [SyncVar] private bool _isRunning;
        [SyncVar] private float _angleAtChange;
        [SyncVar] private double _changedAt;

        // The server time minus this peer's time, measured each frame, so the physics steps between frames get the
        // server time too.
        private double _clockOffset;
        private bool _hasClock;

        public bool IsRunning => _isRunning;
        public uint CarrierId => netId;
        public Pose PoseBefore { get; private set; }
        public Pose PoseNow { get; private set; }
        public Quaternion DrawnRotation => _body.transform.rotation;

        private float AngularSpeed => _speed / _radius;

        private void Awake() {
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

            _clockOffset = NetworkTime.time - Time.timeAsDouble;
            _hasClock = true;
        }

        private void FixedUpdate() {
            PoseBefore = PoseNow;
            if (_hasClock == false)
                return;

            double serverTime = Time.fixedTimeAsDouble + _clockOffset;
            PoseNow = PoseAt(CirclePath.AngleAt(_isRunning, _angleAtChange, _changedAt, AngularSpeed, serverTime), serverTime);
            _body.MovePosition(PoseNow.position);
            _body.MoveRotation(PoseNow.rotation);
        }

        public bool Holds(Vector3 worldPoint) {
            Vector3 local = CarrierMotion.ToLocalPoint(PoseNow, worldPoint);
            Vector3 half = _deck.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x + _rideEdgeMargin
                && Mathf.Abs(local.z) <= half.z + _rideEdgeMargin
                && local.y >= -half.y
                && local.y <= half.y + _rideHeight;
        }

        [Server]
        public void ServerToggle() {
            double now = NetworkTime.time;
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
