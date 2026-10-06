using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunDirector : NetworkBehaviour {
        [SerializeField] private ShipBase _ship;
        [SerializeField] private ShipLandingPad _startPad;
        [SerializeField] private ShipStationCatalog _stations;

        [SyncVar(hook = nameof(OnRunStateChanged))]
        private ShipRunPhase _phase;

        [SyncVar(hook = nameof(OnRunStateChangedInt))]
        private int _loopIndex;

        [SyncVar(hook = nameof(OnRunStateChangedDouble))]
        private double _cruiseEndNetworkTime;

        [SyncVar(hook = nameof(OnRunStateChangedBool))]
        private bool _launchLocked;

        [SyncVar(hook = nameof(OnRunStateChangedFloat))]
        private float _transitWorkRemaining;

        [SyncVar(hook = nameof(OnRunStateChangedFloat))]
        private float _transitSpeed = 1f;

        [SyncVar(hook = nameof(OnRunStateChangedFloat))]
        private float _transitAlignment = 1f;

        [SyncVar(hook = nameof(OnRunStateChangedFloat))]
        private float _transitSecondsRemaining;

        [SyncVar(hook = nameof(OnRunStateChangedVector3))]
        private Vector3 _transitDestination;

        [Inject]
        private IShipRunService _run;

        [Inject]
        private ShipRunModel _model;

        [Inject]
        private ShipStationCatalog _injectedStations;

        [Inject]
        private ShipRadarService _radar;

        private GameObject _localWreck;
        private GameObject _previousLocalWreck;
        private bool _clientOwnsModel;

        private void LateUpdate() {
            if (isServer && _run != null && (_ship == null || _ship.IsFlying == false))
                _run.ServerTick();
        }

        public override void OnStartServer() {
            if (_run != null)
                _run.Bind(this, _ship, _startPad);
        }

        public override void OnStopServer() {
            if (_run != null)
                _run.Unbind(this);
        }

        // The server binds the ship in ShipRunService.Bind; a client's HUD radar needs the same ship.
        public override void OnStartClient() {
            _clientOwnsModel = isServer == false;
            ApplyToModel();
            _radar.BindShip(_ship);
        }

        public override void OnStopClient() {
            _radar.UnbindShip();
            ClientResetModel();
        }

        // A scene unload can destroy the director without OnStopClient.
        private void OnDestroy() =>
            ClientResetModel();

        // The server resets the model in ShipRunService.Unbind; a remote client wrote it from the SyncVars, so it resets
        // its own copy, or the lobby keeps the last run's frozen flight timer.
        private void ClientResetModel() {
            if (_clientOwnsModel == false)
                return;

            _clientOwnsModel = false;
            _model.ResetMatch();
        }

        internal void ServerPublish() {
            if (_model == null)
                return;

            _phase = _model.Phase;
            _loopIndex = _model.LoopIndex;
            _cruiseEndNetworkTime = _model.CruiseEndNetworkTime;
            _launchLocked = _model.LaunchLocked;
            _transitWorkRemaining = _model.TransitWorkRemaining;
            _transitSpeed = _model.TransitSpeed;
            _transitAlignment = _model.TransitAlignment;
            _transitSecondsRemaining = _model.TransitSecondsRemaining;
            _transitDestination = _model.TransitDestination;
            ApplyToModel();
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
            GameObject prefab = ResolveWreckPrefab();
            if (prefab == null)
                return;

            _localWreck = Instantiate(prefab, position, rotation);
        }

        private GameObject ResolveWreckPrefab() {
            if (_stations != null && _stations.WreckPrefab != null)
                return _stations.WreckPrefab;

            return _injectedStations != null ? _injectedStations.WreckPrefab : null;
        }

        private void OnRunStateChanged(ShipRunPhase previous, ShipRunPhase current) =>
            ApplyToClientModel();

        private void OnRunStateChangedInt(int previous, int current) =>
            ApplyToClientModel();

        private void OnRunStateChangedDouble(double previous, double current) =>
            ApplyToClientModel();

        private void OnRunStateChangedBool(bool previous, bool current) =>
            ApplyToClientModel();

        private void OnRunStateChangedFloat(float previous, float current) =>
            ApplyToClientModel();

        private void OnRunStateChangedVector3(Vector3 previous, Vector3 current) =>
            ApplyToClientModel();

        // Mirror runs these hooks on the host while ServerPublish is still assigning the SyncVars one by one;
        // copying the half-updated set back would overwrite the server model (e.g. LoopIndex reset to 0).
        private void ApplyToClientModel() {
            if (isServer)
                return;

            ApplyToModel();
        }

        private void ApplyToModel() {
            if (_model == null)
                return;

            _model.Phase = _phase;
            _model.LoopIndex = _loopIndex;
            _model.CruiseEndNetworkTime = _cruiseEndNetworkTime;
            _model.LaunchLocked = _launchLocked;
            _model.TransitWorkRemaining = _transitWorkRemaining;
            _model.TransitSpeed = _transitSpeed;
            _model.TransitAlignment = _transitAlignment;
            _model.TransitSecondsRemaining = _transitSecondsRemaining;
            _model.TransitDestination = _transitDestination;
        }
    }
}
