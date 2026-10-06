using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunDirector : ShipRunBridge {
        [SerializeField] private ShipBase _ship;
        [SerializeField] private ShipLandingPad _startPad;

        [Inject]
        private IShipRunService _run;

        [Inject]
        private ShipStationCatalog _stations;

        [Inject]
        private IShipRadarBinding _radar;

        private GameObject _localWreck;
        private GameObject _previousLocalWreck;
        private bool _clientStarted;

        private void LateUpdate() {
            if (isServer && _run != null && (_ship == null || _ship.IsFlying == false))
                _run.ServerTick();
        }

        public override void OnStartServer() {
            base.OnStartServer();
            if (_run != null)
                _run.Bind(this, _ship, _startPad);
        }

        public override void OnStopServer() {
            if (_run != null)
                _run.Unbind(this);
        }

        // The server binds the ship in ShipRunService.Bind; a client's HUD radar needs the same ship.
        public override void OnStartClient() {
            base.OnStartClient();
            _clientStarted = true;
            _radar.BindShip(_ship);
        }

        public override void OnStopClient() {
            _clientStarted = false;
            _radar.UnbindShip();
            base.OnStopClient();
        }

        // A scene unload can destroy the director without OnStopClient; the bridge must still clear the run model, or
        // the lobby keeps the last run's frozen flight timer.
        private void OnDestroy() {
            if (_clientStarted)
                OnStopClient();
        }

        internal void ServerPlaceWreck(Vector3 position, Quaternion rotation) {
            PlaceWreck(position, rotation);
            RpcPlaceWreck(position, rotation);
        }

        // The server has already moved the pads; pads have no transform sync, so clients move their copies here, in
        // the same frame the ship's shift arrives. Loose items are moved on every peer instead of teleported: Mirror's
        // teleport RPC lands before the client's rigidbody interpolation and the item slides in from the old frame.
        internal void ServerShiftWorld(Vector3 delta, NetworkIdentity currentPad, NetworkIdentity previousPad) {
            ShiftWreck(delta);
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (TryGetLooseItem(identity, out Rigidbody body))
                    ShiftBody(body, delta);
            }

            RpcShiftWorld(delta, currentPad, previousPad);
        }

        [ClientRpc]
        private void RpcShiftWorld(Vector3 delta, NetworkIdentity currentPad, NetworkIdentity previousPad) {
            if (isServer)
                return;

            ShiftWreck(delta);
            ShiftClientPad(currentPad, delta);
            ShiftClientPad(previousPad, delta);
            foreach (NetworkIdentity identity in NetworkClient.spawned.Values)
                ShiftClientItem(identity, delta);
        }

        private static void ShiftClientPad(NetworkIdentity pad, Vector3 delta) {
            if (pad != null)
                pad.transform.position += delta;
        }

        // A client's snapshot buffer can already hold the item's post-shift pose (the shift frame's unreliable batch may
        // be read before this RPC), so the buffer is not shifted: it restarts from the newest snapshot, placed at the
        // shifted pose, and the item's interpolation goes on from there in the new frame.
        private static void ShiftClientItem(NetworkIdentity identity, Vector3 delta) {
            if (TryGetLooseItem(identity, out Rigidbody body) == false)
                return;

            ShiftBody(body, delta);
            if (identity.TryGetComponent(out NetworkTransformBase sync))
                RestartSnapshots(sync.clientSnapshots, body.position);
        }

        // Held items are skipped: their holder moves them. Deck cargo is shifted with the rest; the deck re-poses it
        // from the (shifted) ship every frame anyway.
        private static bool TryGetLooseItem(NetworkIdentity identity, out Rigidbody body) {
            body = null;
            if (identity == null || identity.TryGetComponent(out Grabbable grabbable) == false)
                return false;

            return grabbable.CanBeGrabbed && identity.TryGetComponent(out body);
        }

        private static void ShiftBody(Rigidbody body, Vector3 delta) {
            Transform item = body.transform;
            item.position += delta;
            body.position = item.position;
        }

        private static void RestartSnapshots(SortedList<double, TransformSnapshot> snapshots, Vector3 position) {
            if (snapshots.Count == 0)
                return;

            int newest = snapshots.Count - 1;
            double key = snapshots.Keys[newest];
            TransformSnapshot snapshot = snapshots.Values[newest];
            snapshot.position = position;
            snapshots.Clear();
            snapshots.Add(key, snapshot);
        }

        private void ShiftWreck(Vector3 delta) {
            if (_localWreck != null)
                _localWreck.transform.position += delta;

            if (_previousLocalWreck != null)
                _previousLocalWreck.transform.position += delta;
        }

        [ClientRpc]
        private void RpcPlaceWreck(Vector3 position, Quaternion rotation) {
            if (isServer)
                return;

            PlaceWreck(position, rotation);
        }

        private void PlaceWreck(Vector3 position, Quaternion rotation) {
            if (_previousLocalWreck != null)
                Destroy(_previousLocalWreck);

            _previousLocalWreck = _localWreck;
            GameObject prefab = _stations.WreckPrefab;
            if (prefab == null)
                return;

            _localWreck = Instantiate(prefab, position, rotation);
        }
    }
}
