using Mirror;
using UnityEngine;

namespace Features.MovingPlatformModule {
    // A test platform that flies around a circle (around the lobby) and turns along its way, for trying out walking and
    // jumping on a moving platform. Only three values are synced: running or not, and the angle and server time of the
    // last start or stop; every peer computes the pose from the server time (CirclePath), so it moves the same and
    // smoothly everywhere. The pose is set each physics step on a kinematic, interpolated body.
    public sealed class CirclingPlatform : NetworkBehaviour {
        [SerializeField] private Rigidbody _body;
        [Tooltip("The middle of the circle, world position; its height is the platform's.")]
        [SerializeField] private Vector3 _center;
        [Tooltip("Radius of the circle, metres.")]
        [SerializeField, Min(0.1f)] private float _radius = 20f;
        [Tooltip("Speed along the circle, metres a second.")]
        [SerializeField, Min(0f)] private float _speed = 5f;

        [SyncVar] private bool _isRunning = true;
        [SyncVar] private float _angleAtChange;
        [SyncVar] private double _changedAt;

        // The server time minus this peer's time, measured each frame, so the physics steps between frames get the
        // server time too.
        private double _clockOffset;
        private bool _hasClock;

        public bool IsRunning => _isRunning;

        private float AngularSpeed => _speed / _radius;

        private void Awake() {
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            PlaceAt(_angleAtChange, true);
        }

        private void Update() {
            if (NetworkClient.active == false && NetworkServer.active == false)
                return;

            _clockOffset = NetworkTime.time - Time.timeAsDouble;
            _hasClock = true;
        }

        private void FixedUpdate() {
            if (_hasClock == false)
                return;

            double serverTime = Time.fixedTimeAsDouble + _clockOffset;
            PlaceAt(CirclePath.AngleAt(_isRunning, _angleAtChange, _changedAt, AngularSpeed, serverTime), false);
        }

        [Server]
        public void ServerToggle() {
            double now = NetworkTime.time;
            _angleAtChange = CirclePath.AngleAt(_isRunning, _angleAtChange, _changedAt, AngularSpeed, now);
            _changedAt = now;
            _isRunning = _isRunning == false;
        }

        private void PlaceAt(float angle, bool teleport) {
            Vector3 position = CirclePath.PositionAt(_center, _radius, angle);
            Quaternion rotation = CirclePath.RotationAt(angle);
            if (teleport) {
                _body.position = position;
                _body.rotation = rotation;
                _body.transform.SetPositionAndRotation(position, rotation);
                return;
            }

            _body.MovePosition(position);
            _body.MoveRotation(rotation);
        }
    }
}
