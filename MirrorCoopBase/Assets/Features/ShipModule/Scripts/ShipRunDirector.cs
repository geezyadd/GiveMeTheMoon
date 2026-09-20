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

        [Inject]
        private ShipRunService _run;

        [Inject]
        private ShipRunModel _model;

        [Inject]
        private ShipStationCatalog _injectedStations;

        private GameObject _localWreck;
        private GameObject _previousLocalWreck;

        private void LateUpdate() {
            if (isServer && _run != null)
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

        public override void OnStartClient() {
            ApplyToModel();
        }

        internal void ServerPublish(ShipRunPhase phase, int loopIndex, double cruiseEndNetworkTime, bool launchLocked) {
            _phase = phase;
            _loopIndex = loopIndex;
            _cruiseEndNetworkTime = cruiseEndNetworkTime;
            _launchLocked = launchLocked;
            ApplyToModel();
        }

        internal void ServerPlaceWreck(Vector3 position, Quaternion rotation) {
            PlaceWreck(position, rotation);
            RpcPlaceWreck(position, rotation);
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

        private void OnRunStateChanged(ShipRunPhase previous, ShipRunPhase current) {
            ApplyToModel();
        }

        private void OnRunStateChangedInt(int previous, int current) {
            ApplyToModel();
        }

        private void OnRunStateChangedDouble(double previous, double current) {
            ApplyToModel();
        }

        private void OnRunStateChangedBool(bool previous, bool current) {
            ApplyToModel();
        }

        private void ApplyToModel() {
            if (_model == null)
                return;

            _model.Phase = _phase;
            _model.LoopIndex = _loopIndex;
            _model.CruiseEndNetworkTime = _cruiseEndNetworkTime;
            _model.LaunchLocked = _launchLocked;
        }
    }
}
