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
        private ShipRunService _run;

        [Inject]
        private ShipRunModel _model;

        [Inject]
        private ShipStationCatalog _injectedStations;

        private GameObject _localWreck;
        private GameObject _previousLocalWreck;

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

        public override void OnStartClient() {
            ApplyToModel();
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

        private void OnRunStateChangedFloat(float previous, float current) {
            ApplyToModel();
        }

        private void OnRunStateChangedVector3(Vector3 previous, Vector3 current) {
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
