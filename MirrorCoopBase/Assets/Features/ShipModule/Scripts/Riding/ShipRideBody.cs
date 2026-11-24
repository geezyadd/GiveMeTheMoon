using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts.Riding {
    // The rider's body while it is bound to a ship (standing or seated): kinematic, without collisions, interpolation
    // or the float spring, its own network body silent, placed each frame at its offset in the ship's axes. Releasing
    // gives it all back as it was.
    internal sealed class ShipRideBody {
        private readonly Transform _transform;
        private readonly Rigidbody _rb;
        private readonly FloatingController _floating;
        private readonly NetworkRigidbodyUnreliable _netBody;

        private bool _wasKinematic;
        private RigidbodyInterpolation _wasInterpolation;
        private bool _netBodyWasEnabled;

        public ShipRideBody(Transform transform, Rigidbody rb, FloatingController floating, NetworkRigidbodyUnreliable netBody) {
            _transform = transform;
            _rb = rb;
            _floating = floating;
            _netBody = netBody;
        }

        // Takes the body out of the physics and puts it at its offset on the platform.
        public void Attach(Transform platform, Vector3 localOffset) {
            _wasKinematic = _rb.isKinematic;
            _wasInterpolation = _rb.interpolation;
            _rb.StopIfDynamic();
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            if (_transform.parent != null)
                _transform.SetParent(null, true);

            PlaceAt(platform, localOffset);

            if (_floating != null)
                _floating.HoverEnabled = false;
        }

        public void PlaceAt(Transform platform, Vector3 localOffset) {
            if (platform == null)
                return;

            Vector3 world = platform.TransformPoint(localOffset);
            Quaternion rotation = platform.rotation;
            _transform.SetPositionAndRotation(world, rotation);
            if (_rb == null)
                return;

            _rb.StopIfDynamic();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.None;
            _rb.position = world;
            _rb.rotation = rotation;
        }

        // Puts the body back into the physics where it stands; with carryVelocity it leaves with the ride's velocity.
        public void Detach(bool carryVelocity, Vector3 rideVelocity) {
            Vector3 worldPosition = _transform.position;
            Quaternion worldRotation = _transform.rotation;
            _transform.SetParent(null, true);
            _transform.SetPositionAndRotation(worldPosition, worldRotation);

            if (_rb != null) {
                _rb.isKinematic = _wasKinematic;
                _rb.detectCollisions = true;
                _rb.interpolation = _wasInterpolation;
                if (carryVelocity && _rb.isKinematic == false)
                    _rb.linearVelocity = rideVelocity;
            }

            if (_floating != null)
                _floating.HoverEnabled = true;
        }

        public void SilenceNetworkBody() {
            if (_netBody == null)
                return;

            _netBodyWasEnabled = _netBody.enabled;
            _netBody.enabled = false;
        }

        public void RestoreNetworkBody() {
            if (_netBody == null)
                return;

            _netBody.enabled = _netBodyWasEnabled;
        }
    }
}
