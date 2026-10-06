using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipDeckCargo {
        private const float SCAN_INTERVAL = 0.2f;
        private const float REST_HEIGHT = 2.2f;
        private const float BELOW_DECK_TOLERANCE = 0.2f;

        private readonly List<Attached> _attached = new List<Attached>(8);
        private readonly List<Pending> _pending = new List<Pending>(4);
        private readonly List<Grabbable> _inHand = new List<Grabbable>(4);
        private readonly Dictionary<int, Grabbable> _known = new Dictionary<int, Grabbable>(16);
        private float _nextScan;

        internal int AttachedCount => _attached.Count;

        internal void ServerReleaseGrabbed(Transform ship, ShipBase deck, ShipPoseSync poseSync) {
            for (int i = _attached.Count - 1; i >= 0; i--) {
                Attached item = _attached[i];
                if (item.Body != null && IsHeld(item))
                    _inHand.Add(item.Grabbable);

                if (item.Body == null || IsHeld(item))
                    DetachAt(i, ship, poseSync, true);
            }

            ServerCatchReleased(ship, deck, poseSync);
        }

        // An item taken from the deck goes back on it in the frame it leaves the hand: until the next scan it would
        // hang in the world while the flying ship moves on. At a station the scan picks it up as before.
        private void ServerCatchReleased(Transform ship, ShipBase deck, ShipPoseSync poseSync) {
            for (int i = _inHand.Count - 1; i >= 0; i--) {
                Grabbable grabbable = _inHand[i];
                if (grabbable != null && grabbable.CanBeGrabbed == false)
                    continue;

                _inHand.RemoveAt(i);
                if (grabbable == null || deck.IsFlying == false || Contains(grabbable))
                    continue;

                if (IsOnDeck(grabbable, ship, deck))
                    Attach(grabbable, ship, poseSync, true);
            }
        }

        internal void Follow(Transform ship, ShipBase deck, bool server, ShipPoseSync poseSync) {
            if (ship == null)
                return;

            ResolvePending(ship);
            if (server && Time.time >= _nextScan) {
                _nextScan = Time.time + SCAN_INTERVAL;
                ServerScan(ship, deck, poseSync);
            }

            for (int i = _attached.Count - 1; i >= 0; i--) {
                Attached item = _attached[i];
                if (item.Body == null || IsHeld(item)) {
                    DetachAt(i, ship, poseSync, server);
                    continue;
                }

                Pin(item, ship);
            }
        }

        internal void DetachAll(Transform ship, ShipPoseSync poseSync, bool tellClients) {
            for (int i = _attached.Count - 1; i >= 0; i--)
                DetachAt(i, ship, poseSync, tellClients);

            _pending.Clear();
            _inHand.Clear();
        }

        internal void ShiftDetached(Vector3 delta) {
            for (int i = 0; i < _attached.Count; i++) {
                Attached item = _attached[i];
                if (item.Body == null)
                    continue;

                item.Body.position += delta;
            }
        }

        internal void ClientAttach(uint netId, Vector3 localPosition, Quaternion localRotation, Transform ship) {
            _pending.Add(new Pending(netId, localPosition, localRotation));
            ResolvePending(ship);
        }

        internal void ClientDetach(uint netId, Transform ship) {
            for (int i = _pending.Count - 1; i >= 0; i--) {
                if (_pending[i].NetId == netId)
                    _pending.RemoveAt(i);
            }

            for (int i = _attached.Count - 1; i >= 0; i--) {
                if (_attached[i].NetId == netId)
                    DetachAt(i, ship, null, false);
            }
        }

        internal void DebugAttach(Grabbable grabbable, Transform ship, ShipPoseSync poseSync) {
            if (grabbable == null || Contains(grabbable))
                return;

            Attach(grabbable, ship, poseSync, true);
        }

        internal bool TryMeasure(Transform ship, out Vector3 localPosition, out float drift) {
            localPosition = Vector3.zero;
            drift = 0f;
            if (_attached.Count == 0 || ship == null)
                return false;

            Attached item = _attached[0];
            if (item.Body == null)
                return false;

            localPosition = ship.InverseTransformPoint(item.Body.position);
            Vector3 delta = localPosition - item.LocalPosition;
            delta.y = 0f;
            drift = delta.magnitude;
            return true;
        }

        private void ServerScan(Transform ship, ShipBase deck, ShipPoseSync poseSync) {
            if (deck == null)
                return;

            int hits = OverlapDeck(deck);
            for (int i = 0; i < hits; i++) {
                Collider hit = OverlapScratch[i];
                if (hit == null)
                    continue;

                int id = hit.GetInstanceID();
                if (_known.TryGetValue(id, out Grabbable grabbable) == false) {
                    grabbable = hit.GetComponentInParent<Grabbable>();
                    _known[id] = grabbable;
                }

                if (grabbable == null || grabbable.CanBeGrabbed == false)
                    continue;

                if (Contains(grabbable) || IsOnDeck(grabbable, ship, deck) == false)
                    continue;

                Attach(grabbable, ship, poseSync, true);
            }
        }

        private static bool IsOnDeck(Grabbable grabbable, Transform ship, ShipBase deck) {
            Vector3 local = ship.InverseTransformPoint(grabbable.transform.position);
            // The whole deck, no edge inset: anything lying on it flies with the ship.
            if (deck.DeckGeometry.ContainsDeckWalk(local, 0f) == false)
                return false;

            if (deck.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            return local.y >= surfaceY - BELOW_DECK_TOLERANCE && local.y <= surfaceY + REST_HEIGHT;
        }

        private static int OverlapDeck(ShipBase deck) {
            BoxCollider volume = deck.RideVolume;
            if (volume == null)
                return 0;

            Vector3 center = volume.transform.TransformPoint(volume.center);
            Vector3 halfExtents = Vector3.Scale(volume.size, volume.transform.lossyScale) * 0.5f;
            return Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                OverlapScratch,
                volume.transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore);
        }

        private void Attach(Grabbable grabbable, Transform ship, ShipPoseSync poseSync, bool tellClients) {
            Transform root = grabbable.transform;
            Vector3 localPosition = ship.InverseTransformPoint(root.position);
            Quaternion localRotation = Quaternion.Inverse(ship.rotation) * root.rotation;
            NetworkIdentity identity = grabbable.GetComponent<NetworkIdentity>();
            uint netId = identity != null ? identity.netId : 0u;
            Attached item = CreateAttached(grabbable, netId, localPosition, localRotation);
            _attached.Add(item);
            Pin(item, ship);
            if (tellClients && poseSync != null && netId != 0u)
                poseSync.ServerAttachDeckItem(netId, localPosition, localRotation);
        }

        private void DetachAt(int index, Transform ship, ShipPoseSync poseSync, bool tellClients) {
            Attached item = _attached[index];
            _attached.RemoveAt(index);
            if (tellClients && poseSync != null && item.NetId != 0u)
                poseSync.ServerDetachDeckItem(item.NetId);

            if (item.Body == null)
                return;

            Vector3 worldPosition = item.Body.position;
            Quaternion worldRotation = item.Body.rotation;
            item.Body.SetParent(null, true);
            item.Body.SetPositionAndRotation(worldPosition, worldRotation);
            // A grabbed item is already in the hand: Grabbable owns its kinematic hold and silenced network body.
            if (IsHeld(item) == false)
                RestorePhysics(item);
        }

        private static bool IsHeld(Attached item) =>
            item.Grabbable != null && item.Grabbable.CanBeGrabbed == false;

        private static void RestorePhysics(Attached item) {
            if (item.Rigidbody != null) {
                item.Rigidbody.isKinematic = item.WasKinematic;
                item.Rigidbody.interpolation = item.Interpolation;
                if (item.WasKinematic == false) {
                    item.Rigidbody.linearVelocity = Vector3.zero;
                    item.Rigidbody.angularVelocity = Vector3.zero;
                }
            }

            if (item.NetworkBody != null)
                item.NetworkBody.enabled = item.NetworkBodyWasEnabled;
        }

        // Captured before the first Pin, so a detach gives the item back the physics it had before the deck took it.
        private static Attached CreateAttached(Grabbable grabbable, uint netId, Vector3 localPosition, Quaternion localRotation) {
            Rigidbody body = grabbable.GetComponent<Rigidbody>();
            NetworkRigidbodyUnreliable networkBody = grabbable.GetComponent<NetworkRigidbodyUnreliable>();
            return new Attached(
                grabbable,
                body,
                networkBody,
                netId,
                localPosition,
                localRotation,
                body != null && body.isKinematic,
                body != null ? body.interpolation : RigidbodyInterpolation.None,
                networkBody != null && networkBody.enabled);
        }

        private void Pin(Attached item, Transform ship) {
            Vector3 world = ship.TransformPoint(item.LocalPosition);
            Quaternion rotation = ship.rotation * item.LocalRotation;
            if (item.Body.parent != null)
                item.Body.SetParent(null, true);

            item.Body.SetPositionAndRotation(world, rotation);
            if (item.Rigidbody != null) {
                item.Rigidbody.isKinematic = true;
                item.Rigidbody.interpolation = RigidbodyInterpolation.None;
                item.Rigidbody.position = world;
                item.Rigidbody.rotation = rotation;
                item.Rigidbody.linearVelocity = Vector3.zero;
                item.Rigidbody.angularVelocity = Vector3.zero;
            }

            if (item.NetworkBody != null)
                item.NetworkBody.enabled = false;
        }

        private void ResolvePending(Transform ship) {
            if (ship == null)
                return;

            for (int i = _pending.Count - 1; i >= 0; i--) {
                Pending pending = _pending[i];
                if (NetworkClient.spawned.TryGetValue(pending.NetId, out NetworkIdentity identity) == false)
                    continue;

                _pending.RemoveAt(i);
                Grabbable grabbable = identity.GetComponent<Grabbable>();
                if (grabbable == null || Contains(grabbable))
                    continue;

                Attached item = CreateAttached(grabbable, pending.NetId, pending.LocalPosition, pending.LocalRotation);
                _attached.Add(item);
                Pin(item, ship);
            }
        }

        private bool Contains(Grabbable grabbable) {
            for (int i = 0; i < _attached.Count; i++) {
                if (_attached[i].Grabbable == grabbable)
                    return true;
            }

            return false;
        }

        private static readonly Collider[] OverlapScratch = new Collider[24];

        private readonly struct Pending {
            public Pending(uint netId, Vector3 localPosition, Quaternion localRotation) {
                NetId = netId;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }

            public uint NetId { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
        }

        private readonly struct Attached {
            public Attached(
                Grabbable grabbable,
                Rigidbody rigidbody,
                NetworkRigidbodyUnreliable networkBody,
                uint netId,
                Vector3 localPosition,
                Quaternion localRotation,
                bool wasKinematic,
                RigidbodyInterpolation interpolation,
                bool networkBodyWasEnabled) {
                Grabbable = grabbable;
                Rigidbody = rigidbody;
                NetworkBody = networkBody;
                Body = grabbable != null ? grabbable.transform : null;
                NetId = netId;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                WasKinematic = wasKinematic;
                Interpolation = interpolation;
                NetworkBodyWasEnabled = networkBodyWasEnabled;
            }

            public Grabbable Grabbable { get; }
            public Transform Body { get; }
            public Rigidbody Rigidbody { get; }
            public NetworkRigidbodyUnreliable NetworkBody { get; }
            public uint NetId { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public bool WasKinematic { get; }
            public RigidbodyInterpolation Interpolation { get; }
            public bool NetworkBodyWasEnabled { get; }
        }
    }
}
