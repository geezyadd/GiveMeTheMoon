using System;
using Features.GameCoreModule.Contracts;
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
        private bool _isGrabBlocked;

        internal event Action<Grabbable> Stopped;

        public bool CanBeGrabbed => _isHeld == false && _isGrabBlocked == false;

        public override void OnStopServer() =>
            Stopped?.Invoke(this);

        public override void OnStopClient() =>
            Stopped?.Invoke(this);

        // The physics of an item nobody holds, also for one the flying deck has carried and lets go of at a station: what
        // the item had before it was held or carried would be the hold's or the deck's kinematic pin.
        public void ApplyLoosePhysics() {
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

        // Something other than a hand owns the item for a while (a bug carries the parcel off): no hand takes it and the
        // flying deck lets go of it, as of an item in a hand. The owner moves it and gives it its physics back.
        public void SetGrabBlocked(bool isBlocked) {
            _isGrabBlocked = isBlocked;
            if (isBlocked)
                SetHovered(false);
        }

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
                _rb.StopIfDynamic();
                _rb.isKinematic = true;
                _rb.detectCollisions = false;
            }

            FollowHolder();
        }

        internal void ApplyRelease() {
            _isHeld = false;
            _holdPoint = null;
            ApplyLoosePhysics();
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
