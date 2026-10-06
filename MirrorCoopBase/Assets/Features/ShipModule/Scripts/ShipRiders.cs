using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The players riding the ship: who is on the deck, binding them to it in flight and releasing them after.
    internal sealed class ShipRiders {
        private static readonly Collider[] RiderScratch = new Collider[16];

        private readonly ShipBase _ship;
        private readonly ShipDeckGeometry _deckGeometry;
        private readonly BoxCollider _rideVolume;
        private readonly ShipFlightSettings _flightSettings;
        private readonly List<ShipRider> _riders = new List<ShipRider>();
        private readonly List<ShipRider> _insideVolume = new List<ShipRider>();

        internal IReadOnlyList<ShipRider> All => _riders;

        internal ShipRiders(ShipBase ship, ShipDeckGeometry deckGeometry, BoxCollider rideVolume, ShipFlightSettings flightSettings) {
            _ship = ship;
            _deckGeometry = deckGeometry;
            _rideVolume = rideVolume;
            _flightSettings = flightSettings;
        }

        internal void DebugBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            if (_insideVolume.Contains(rider) == false)
                _insideVolume.Add(rider);

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(_ship);
        }

        internal void RegisterRider(ShipRider rider) {
            if (rider == null)
                return;

            if (_insideVolume.Contains(rider) == false)
                _insideVolume.Add(rider);

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            TryBindRider(rider);
        }

        internal void SetVolumeOverlap(ShipRider rider, bool inside) {
            if (rider == null)
                return;

            if (inside) {
                if (_insideVolume.Contains(rider) == false)
                    _insideVolume.Add(rider);

                if (_ship.IsFlying)
                    TryBindRider(rider);

                return;
            }

            _insideVolume.Remove(rider);
            // Binding turns the rider's collisions off, which raises this exit; a bound rider is released by its own path.
            if (rider.IsRiding)
                return;

            UnregisterRider(rider);
        }

        internal void UnregisterRider(ShipRider rider) {
            if (_ship.IsFlying)
                return;

            if (rider != null && NetworkServer.active)
                rider.ServerRelease();

            _riders.Remove(rider);
        }

        internal void TrackRider(ShipRider rider) {
            if (_riders.Contains(rider) == false)
                _riders.Add(rider);
        }

        internal void Remove(ShipRider rider) {
            _riders.Remove(rider);
            _insideVolume.Remove(rider);
        }

        internal void CollectRidersInVolume() {
            if (_rideVolume != null) {
                Vector3 center = _rideVolume.transform.TransformPoint(_rideVolume.center);
                Vector3 halfExtents = Vector3.Scale(_rideVolume.size, _rideVolume.transform.lossyScale) * 0.5f;
                int hits = Physics.OverlapBoxNonAlloc(
                    center,
                    halfExtents,
                    RiderScratch,
                    _rideVolume.transform.rotation,
                    ~0,
                    QueryTriggerInteraction.Collide);

                for (int i = 0; i < hits; i++) {
                    Collider hit = RiderScratch[i];
                    if (hit == null)
                        continue;

                    RegisterRider(hit.GetComponentInParent<ShipRider>());
                }
            }

            ShipRider[] riders = Object.FindObjectsByType<ShipRider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Vector3 origin = _ship.transform.position;
            const float radiusSq = 64f;
            for (int i = 0; i < riders.Length; i++) {
                ShipRider rider = riders[i];
                if (rider == null)
                    continue;

                if ((rider.transform.position - origin).sqrMagnitude > radiusSq)
                    continue;

                RegisterRider(rider);
            }
        }

        internal void CatchDeckRiders() {
            for (int i = 0; i < _insideVolume.Count; i++)
                TryBindRider(_insideVolume[i]);
        }

        // Each peer boards only its own player; the others follow their owner's synced ride (ShipRider hooks).
        internal void BindRiders() {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                ShipRider rider = _riders[i];
                if (rider == null || rider.isOwned == false)
                    continue;

                if (IsAboveDeck(rider.transform.position) == false) {
                    _riders.RemoveAt(i);
                    continue;
                }

                rider.BindToPlatform(_ship);
            }
        }

        internal void FollowRiders(float dt) {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].Follow(dt);
            }
        }

        internal void ServerReleaseRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].ServerRelease();
            }
        }

        internal bool HasOwnedHelmPilot() {
            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                if (rider != null && rider.isOwned && rider.IsHelmSeat)
                    return true;
            }

            return false;
        }

        private void TryBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            if (rider.WantsLand(_ship) == false)
                return;

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(_ship);
        }

        private bool IsAboveDeck(Vector3 worldPosition) {
            Transform ship = _ship.transform;
            Vector3 local = Quaternion.Inverse(ship.rotation) * (worldPosition - ship.position);
            if (_deckGeometry.TryClosestDeckWalk(local, out _, out float dx, out float dz) == false)
                return false;

            float edge = _flightSettings.BoardingEdgeTolerance;
            if (dx * dx + dz * dz > edge * edge)
                return false;

            if (_deckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            float height = local.y - surfaceY;
            return height >= _flightSettings.BoardingMinHeight && height <= _flightSettings.BoardingMaxHeight;
        }
    }
}
