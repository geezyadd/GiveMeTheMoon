using System;
using Features.GameCoreModule.Contracts;
using FMODUnity;
using Mirror;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    // An item a player can carry. Who holds it lives in the holder's PlayerHand model and what a player carries
    // stowed in their inventory model; HeldItemRegistry applies that state here, so the item has no synced hold state
    // of its own. A stowed item is held out of sight: hidden, without collisions, following its carrier.
    public sealed class Grabbable : NetworkBehaviour {
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;
        [SerializeField] private Outline _outline;
        [Tooltip("The item can be kept in an inventory slot (hand weapons, tools); off for ship modules.")]
        [SerializeField] private bool _canStoreInInventory;
        [Tooltip("The FMOD event every peer plays where the item comes into a hand (picked up or taken out of a slot). Empty: no sound.")]
        [SerializeField] private EventReference _equipSound;

        private StowedItemVisibility _stowedVisibility;
        private Transform _holdPoint;
        private bool _isHeld;
        private bool _isStowed;

        internal event Action<Grabbable> Stopped;

        public bool CanBeGrabbed => _isHeld == false;
        public bool CanStoreInInventory => _canStoreInInventory;
        public bool IsStowed => _isStowed;
        public EventReference EquipSound => _equipSound;

        private void Awake() =>
            _stowedVisibility = new StowedItemVisibility(GetComponentsInChildren<Renderer>(true), GetComponentsInChildren<Collider>(true));

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

        internal void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            _outline.enabled = hovered && CanBeGrabbed;
        }

        // A thrown item: its velocity right after it left the hand, on the server that simulates it.
        [Server]
        public void ServerLaunch(Vector3 velocity) {
            if (_rb != null && _rb.isKinematic == false)
                _rb.linearVelocity = velocity;
        }

        [Server]
        public void ServerMoveTo(Vector3 position) {
            transform.position = position;
            if (_rb != null)
                _rb.position = position;
        }

        internal void ApplyHold(Transform holdPoint) {
            ShowIfStowed();
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

        // Held out of sight by a carrier; the follow keeps it with them so it comes out at the hand.
        internal void ApplyStow(Transform holdPoint) {
            ApplyHold(holdPoint);
            _isStowed = true;
            _stowedVisibility.Hide();
        }

        internal void ApplyRelease() {
            ShowIfStowed();
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

        private void ShowIfStowed() {
            if (_isStowed == false)
                return;

            _isStowed = false;
            _stowedVisibility.Show();
        }
    }
}
