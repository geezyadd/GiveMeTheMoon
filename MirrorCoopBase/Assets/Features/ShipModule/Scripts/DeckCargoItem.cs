using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // One item the flying deck carries, in ship space. It falls onto the deck until it rests (DeckCargoFall).
    internal sealed class DeckCargoItem {
        internal DeckCargoItem(Grabbable grabbable, uint netId, Vector3 localPosition, Quaternion localRotation) {
            Grabbable = grabbable;
            Body = grabbable.transform;
            Rigidbody = grabbable.GetComponent<Rigidbody>();
            NetworkBody = grabbable.GetComponent<NetworkRigidbodyUnreliable>();
            Colliders = grabbable.GetComponentsInChildren<Collider>();
            NetId = netId;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            // Captured before the first pin, so a detach gives the item back the smoothing it had before the deck took it.
            Interpolation = Rigidbody != null ? Rigidbody.interpolation : RigidbodyInterpolation.None;
        }

        public Grabbable Grabbable { get; }
        public Transform Body { get; }
        public Rigidbody Rigidbody { get; }
        public NetworkRigidbodyUnreliable NetworkBody { get; }
        public Collider[] Colliders { get; }
        public uint NetId { get; }
        public Vector3 LocalPosition { get; private set; }
        public Quaternion LocalRotation { get; }
        public RigidbodyInterpolation Interpolation { get; }
        public bool IsResting { get; private set; }
        public float FallSpeed { get; private set; }

        public bool IsGone => Body == null;
        public bool IsHeld => Grabbable != null && Grabbable.CanBeGrabbed == false;

        public void Fall(float drop, float fallSpeed) {
            LocalPosition -= Vector3.up * drop;
            FallSpeed = fallSpeed;
        }

        public void Rest(Vector3 localPosition) {
            LocalPosition = localPosition;
            FallSpeed = 0f;
            IsResting = true;
        }
    }
}
