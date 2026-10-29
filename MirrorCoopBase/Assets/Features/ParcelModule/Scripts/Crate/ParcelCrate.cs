using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Crate {
    // The parcel's crate, a grabbable item like a module: the parts the parcel's server code and look move. Lying on
    // board it is a plain item; under a thief, adrift or on a rope the parcel owns it (no hand takes it).
    public sealed class ParcelCrate : MonoBehaviour {
        [SerializeField] private NetworkIdentity _identity;
        [SerializeField] private Grabbable _grabbable;
        [SerializeField] private Rigidbody _body;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;
        [SerializeField] private Collider _strikeTrigger;
        [SerializeField] private LineRenderer _rope;

        public NetworkIdentity Identity => _identity;
        public Grabbable Grabbable => _grabbable;
        public Vector3 Position => _body.position;

        // Moved by code (under its thief, on a rope): kinematic and out of the physics.
        public void HoldAt(Vector3 worldPosition) {
            _body.isKinematic = true;
            _body.detectCollisions = false;
            _body.position = worldPosition;
            transform.position = worldPosition;
        }

        // Server: let go in open space, it floats on without gravity.
        public void Drift(Vector3 velocity) {
            _body.isKinematic = false;
            _body.useGravity = false;
            _body.detectCollisions = true;
            _body.linearVelocity = velocity;
            _networkBody.enabled = true;
        }

        // Server: back to a plain item with gravity, dropped at this point.
        public void Settle(Vector3 worldPosition) {
            _body.useGravity = true;
            transform.position = worldPosition;
            _body.position = worldPosition;
            _grabbable.ApplyLoosePhysics();
        }

        // Every peer with a client: the thief's look carries it, the network body would pull it back to the server's pose.
        public void SetNetworkBodySynced(bool isSynced) =>
            _networkBody.enabled = isSynced;

        // Every peer with a client, when the thief's look stops carrying it: a client gets back the loose item's physics its
        // hold took (collisions, the network body); the server's own state change already gave it its physics.
        public void ReleaseFromThiefView() {
            if (_identity.isServer)
                _networkBody.enabled = true;
            else
                _grabbable.ApplyLoosePhysics();
        }

        public void SetStrikable(bool isStrikable) =>
            _strikeTrigger.enabled = isStrikable;

        public void ShowRope(Vector3 from) {
            _rope.enabled = true;
            _rope.SetPosition(0, from);
            _rope.SetPosition(1, transform.position);
        }

        public void HideRope() =>
            _rope.enabled = false;
    }
}
