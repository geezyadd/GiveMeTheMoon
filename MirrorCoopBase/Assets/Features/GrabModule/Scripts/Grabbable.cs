using System;
using Mirror;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    // An item a player can carry. Who holds it lives in the holder's PlayerHand model; HeldItemRegistry applies that
    // state here, so the item has no synced hold state of its own.
    public sealed class Grabbable : NetworkBehaviour {
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;
        [SerializeField] private Outline _outline;

        private Transform _holdPoint;
        private bool _isHeld;

        internal event Action<Grabbable> Stopped;

        public bool CanBeGrabbed => _isHeld == false;

        public override void OnStopServer() =>
            Stopped?.Invoke(this);

        public override void OnStopClient() =>
            Stopped?.Invoke(this);

        internal void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            _outline.enabled = hovered && CanBeGrabbed;
        }

        internal void ApplyHold(Transform holdPoint) {
            _isHeld = true;
            _holdPoint = holdPoint;
            SetHovered(false);
            if (_networkBody != null)
                _networkBody.enabled = false;

            if (_rb != null) {
                // Velocity can only be cleared while the body is still dynamic.
                if (_rb.isKinematic == false) {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                _rb.isKinematic = true;
                _rb.detectCollisions = false;
            }

            FollowHolder();
        }

        internal void ApplyRelease() {
            _isHeld = false;
            _holdPoint = null;
            if (_rb != null) {
                _rb.detectCollisions = true;
                // Only the server simulates a loose item: on a client the network body keeps it kinematic and moves it,
                // so a client-side gravity step would only make it twitch before the first snapshot.
                if (isServer) {
                    _rb.isKinematic = false;
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
            }

            if (_networkBody != null)
                _networkBody.enabled = true;
        }

        internal void FollowHolder() {
            if (_holdPoint == null)
                return;

            Vector3 position = _holdPoint.position;
            Quaternion rotation = _holdPoint.rotation;
            transform.SetPositionAndRotation(position, rotation);
            if (_rb != null) {
                _rb.position = position;
                _rb.rotation = rotation;
            }
        }
    }
}
