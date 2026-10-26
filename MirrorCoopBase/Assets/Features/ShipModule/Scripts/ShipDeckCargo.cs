using System;
using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The flying deck is moved by code, not by physics, so it carries the items lying on it itself: in flight they are
    // pinned in ship space and fall onto the deck there (DeckCargoFall). At a station the ship stands still and every
    // item is a physics body again, so it lies, topples and can be pushed off the deck as anywhere else.
    internal sealed class ShipDeckCargo : IShipDeckCargo {
        private const float SCAN_INTERVAL = 0.2f;
        private const float REST_HEIGHT = 2.2f;
        private const float BELOW_DECK_TOLERANCE = 0.2f;
        // A body slower than this lies where physics put it: the deck takes it as resting, it does not fall again.
        private const float RESTING_SPEED = 0.2f;

        private static readonly Collider[] _overlapScratch = new Collider[24];

        private readonly Transform _ship;
        private readonly ShipBase _deck;
        private readonly ShipPoseSync _poseSync;
        private readonly Func<ShipFlightSettings> _flightSettings;
        private readonly DeckCargoFall _fall;
        private readonly List<DeckCargoItem> _attached = new List<DeckCargoItem>(8);
        private readonly List<DeckCargoEntry> _pending = new List<DeckCargoEntry>(4);
        private readonly List<Grabbable> _released = new List<Grabbable>(4);
        private readonly Dictionary<int, Grabbable> _known = new Dictionary<int, Grabbable>(16);
        private float _nextScan;

        public int AttachedCount => _attached.Count;

        internal ShipDeckCargo(Transform ship, ShipBase deck, ShipPoseSync poseSync, Func<ShipFlightSettings> flightSettings) {
            _ship = ship;
            _deck = deck;
            _poseSync = poseSync;
            _flightSettings = flightSettings;
            _fall = new DeckCargoFall(ship, deck.DeckGeometry);
        }

        internal void ServerReleaseGrabbed() {
            for (int i = _attached.Count - 1; i >= 0; i--) {
                DeckCargoItem item = _attached[i];
                if (item.IsGone || item.IsHeld)
                    DetachAt(i, true);
            }

            ServerCatchReleased();
        }

        // Any item let go in flight, also one carried onto the ship at the station: caught in this frame's
        // ServerReleaseGrabbed, before the flight step moves the deck away from under it.
        internal void OnItemReleased(Grabbable grabbable) {
            if (NetworkServer.active && _deck.IsFlying)
                _released.Add(grabbable);
        }

        // The ship takes off: everything lying on the deck flies with it from the first step, not from the next scan.
        internal void ServerBoardDeckItems() {
            _nextScan = Time.time + SCAN_INTERVAL;
            ServerScan();
        }

        internal void Follow(bool server) {
            ResolvePending();
            if (server && _deck.IsFlying && Time.time >= _nextScan) {
                _nextScan = Time.time + SCAN_INTERVAL;
                ServerScan();
            }

            for (int i = _attached.Count - 1; i >= 0; i--) {
                DeckCargoItem item = _attached[i];
                if (item.IsGone || item.IsHeld) {
                    DetachAt(i, server);
                    continue;
                }

                Pin(item);
                FallOntoDeck(item, server);
            }

            // After the landing's pin (and its origin shift) the ship stands still: physics takes the items back.
            if (server && _deck.IsFlying == false)
                DetachAll(true);
        }

        public void DetachAll(bool tellClients) {
            for (int i = _attached.Count - 1; i >= 0; i--)
                DetachAt(i, tellClients);

            _pending.Clear();
            _released.Clear();
        }

        public void ClientAttach(DeckCargoEntry entry) {
            if (TryFindAttached(entry.NetId, out DeckCargoItem attached)) {
                if (entry.IsResting)
                    attached.Rest(entry.LocalPosition);

                return;
            }

            RemovePending(entry.NetId);
            _pending.Add(entry);
            ResolvePending();
        }

        public void ClientDetach(uint netId) {
            RemovePending(netId);
            for (int i = _attached.Count - 1; i >= 0; i--) {
                if (_attached[i].NetId == netId)
                    DetachAt(i, false);
            }
        }

        public bool TryMeasure(out Vector3 localPosition, out float drift) {
            localPosition = Vector3.zero;
            drift = 0f;
            if (_attached.Count == 0)
                return false;

            DeckCargoItem item = _attached[0];
            if (item.IsGone)
                return false;

            localPosition = _ship.InverseTransformPoint(item.Body.position);
            Vector3 delta = localPosition - item.LocalPosition;
            delta.y = 0f;
            drift = delta.magnitude;
            return true;
        }

        // An item goes onto the deck in the frame it leaves the hand: until the next scan it would hang in the world
        // while the flying ship moves on. It falls from the hand onto the deck, lying level.
        private void ServerCatchReleased() {
            for (int i = 0; i < _released.Count; i++) {
                Grabbable grabbable = _released[i];
                if (IsCatchable(grabbable))
                    Attach(grabbable, Level(LocalRotationOf(grabbable)), false);
            }

            _released.Clear();
        }

        private bool IsCatchable(Grabbable grabbable) =>
            grabbable != null
            && grabbable.CanBeGrabbed
            && _deck.IsFlying
            && Contains(grabbable) == false
            && IsOverDeck(grabbable, _flightSettings().CargoCatchMaxHeight);

        private void ServerScan() {
            int hits = OverlapDeck();
            for (int i = 0; i < hits; i++) {
                Collider hit = _overlapScratch[i];
                if (hit == null)
                    continue;

                int id = hit.GetInstanceID();
                if (_known.TryGetValue(id, out Grabbable grabbable) == false) {
                    grabbable = hit.GetComponentInParent<Grabbable>();
                    _known[id] = grabbable;
                }

                if (grabbable == null || grabbable.CanBeGrabbed == false)
                    continue;

                if (Contains(grabbable) || IsOverDeck(grabbable, REST_HEIGHT) == false)
                    continue;

                Attach(grabbable, LocalRotationOf(grabbable), IsBodyResting(grabbable));
            }
        }

        private bool IsOverDeck(Grabbable grabbable, float maxHeight) {
            Vector3 local = _ship.InverseTransformPoint(grabbable.transform.position);
            // The whole deck, no edge inset: anything lying on it flies with the ship.
            if (_deck.DeckGeometry.ContainsDeckWalk(local, 0f) == false)
                return false;

            if (_deck.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            return local.y >= surfaceY - BELOW_DECK_TOLERANCE && local.y <= surfaceY + maxHeight;
        }

        private static bool IsBodyResting(Grabbable grabbable) {
            if (grabbable.TryGetComponent(out Rigidbody body) == false || body.isKinematic)
                return true;

            return body.IsSleeping() || body.linearVelocity.sqrMagnitude <= RESTING_SPEED * RESTING_SPEED;
        }

        private Quaternion LocalRotationOf(Grabbable grabbable) =>
            Quaternion.Inverse(_ship.rotation) * grabbable.transform.rotation;

        // An item let go from the hand keeps only its heading on the deck: tilted the way the hand held it, it would rest
        // on one corner with the rest of it in the air.
        private static Quaternion Level(Quaternion localRotation) {
            Vector3 forward = localRotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < Vector3.kEpsilon)
                forward = localRotation * Vector3.up;

            forward.y = 0f;
            return forward.sqrMagnitude < Vector3.kEpsilon ? Quaternion.identity : Quaternion.LookRotation(forward, Vector3.up);
        }

        private int OverlapDeck() {
            BoxCollider volume = _deck.RideVolume;
            if (volume == null)
                return 0;

            Vector3 center = volume.transform.TransformPoint(volume.center);
            Vector3 halfExtents = Vector3.Scale(volume.size, volume.transform.lossyScale) * 0.5f;
            return Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _overlapScratch,
                volume.transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore);
        }

        private void Attach(Grabbable grabbable, Quaternion localRotation, bool isResting) {
            Vector3 localPosition = _ship.InverseTransformPoint(grabbable.transform.position);
            NetworkIdentity identity = grabbable.GetComponent<NetworkIdentity>();
            uint netId = identity != null ? identity.netId : 0u;
            DeckCargoItem item = new DeckCargoItem(grabbable, netId, localPosition, localRotation);
            if (isResting)
                item.Rest(localPosition);

            _attached.Add(item);
            Pin(item);
            if (_poseSync != null && netId != 0u)
                _poseSync.ServerAttachDeckItem(EntryOf(item));
        }

        private void FallOntoDeck(DeckCargoItem item, bool server) {
            if (item.IsResting)
                return;

            _fall.Step(item, Time.deltaTime);
            Pin(item);
            // Clients let it fall the same way; the resting place from the server corrects them and serves late joiners.
            if (server && item.IsResting && _poseSync != null && item.NetId != 0u)
                _poseSync.ServerSettleDeckItem(EntryOf(item));
        }

        private static DeckCargoEntry EntryOf(DeckCargoItem item) =>
            new DeckCargoEntry {
                NetId = item.NetId,
                LocalPosition = item.LocalPosition,
                LocalRotation = item.LocalRotation,
                IsResting = item.IsResting
            };

        private void DetachAt(int index, bool tellClients) {
            DeckCargoItem item = _attached[index];
            _attached.RemoveAt(index);
            if (tellClients && _poseSync != null && item.NetId != 0u)
                _poseSync.ServerDetachDeckItem(item.NetId);

            if (item.IsGone)
                return;

            Vector3 worldPosition = item.Body.position;
            Quaternion worldRotation = item.Body.rotation;
            item.Body.SetParent(null, true);
            item.Body.SetPositionAndRotation(worldPosition, worldRotation);
            // A grabbed item is already in the hand: Grabbable owns its kinematic hold and silenced network body.
            if (item.IsHeld == false)
                RestorePhysics(item);
        }

        // Not the physics the item had when the deck took it: a client can take it while its own peer still shows it in the
        // hand, kinematic with a silenced network body, and the item would stay frozen there on that client for good.
        private static void RestorePhysics(DeckCargoItem item) {
            item.Grabbable.ApplyLoosePhysics();
            if (item.Rigidbody == null)
                return;

            item.Rigidbody.interpolation = item.Interpolation;
            // Pinned for a whole flight, it would stay asleep where the deck left it until something hits it.
            if (item.Rigidbody.isKinematic == false)
                item.Rigidbody.WakeUp();
        }

        private void Pin(DeckCargoItem item) {
            Vector3 world = _ship.TransformPoint(item.LocalPosition);
            Quaternion rotation = _ship.rotation * item.LocalRotation;
            if (item.Body.parent != null)
                item.Body.SetParent(null, true);

            item.Body.SetPositionAndRotation(world, rotation);
            if (item.Rigidbody != null) {
                item.Rigidbody.StopIfDynamic();
                item.Rigidbody.isKinematic = true;
                item.Rigidbody.interpolation = RigidbodyInterpolation.None;
                item.Rigidbody.position = world;
                item.Rigidbody.rotation = rotation;
            }

            if (item.NetworkBody != null)
                item.NetworkBody.enabled = false;
        }

        private void ResolvePending() {
            for (int i = _pending.Count - 1; i >= 0; i--) {
                DeckCargoEntry pending = _pending[i];
                if (NetworkClient.spawned.TryGetValue(pending.NetId, out NetworkIdentity identity) == false)
                    continue;

                _pending.RemoveAt(i);
                Grabbable grabbable = identity.GetComponent<Grabbable>();
                if (grabbable == null || Contains(grabbable))
                    continue;

                DeckCargoItem item = new DeckCargoItem(grabbable, pending.NetId, pending.LocalPosition, pending.LocalRotation);
                if (pending.IsResting)
                    item.Rest(pending.LocalPosition);

                _attached.Add(item);
                Pin(item);
            }
        }

        private void RemovePending(uint netId) {
            for (int i = _pending.Count - 1; i >= 0; i--) {
                if (_pending[i].NetId == netId)
                    _pending.RemoveAt(i);
            }
        }

        private bool TryFindAttached(uint netId, out DeckCargoItem attached) {
            attached = null;
            for (int i = 0; i < _attached.Count; i++) {
                if (_attached[i].NetId != netId)
                    continue;

                attached = _attached[i];
                return true;
            }

            return false;
        }

        private bool Contains(Grabbable grabbable) {
            for (int i = 0; i < _attached.Count; i++) {
                if (_attached[i].Grabbable == grabbable)
                    return true;
            }

            return false;
        }
    }
}
