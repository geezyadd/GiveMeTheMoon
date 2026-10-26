using System;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSocket : NetworkBehaviour {
        private const string UNKNOWN_MODULE_INSTALL_FORMAT = "{0}: module '{1}' is not in the {2}, it cannot be installed.";
        private const string UNKNOWN_MODULE_VIEW_FORMAT = "{0}: installed module '{1}' is not in the {2}, its slot view is not shown.";

        [SerializeField] private string _socketId;
        [SerializeField] private ShipModuleType _acceptedType = ShipModuleType.Engine;
        [SerializeField] private bool _requiredForLaunch;
        [SerializeField] private Outline _outline;
        [SerializeField] private Transform _defaultInstallPoint;
        [SerializeField] private ItemViewInstallPoint[] _viewInstallPoints;
        [SerializeField] private int _unlockLoop;
        [Tooltip("Installed here at every ship reset (run start and end, aborted flight). Empty: the socket starts empty.")]
        [SerializeField] private ModuleDefinition _startingModule;
        [SerializeField] private ShipBase _ship;

        private readonly ShipSeatRule _seatRule = new ShipSeatRule();

        private ModuleCatalog _moduleCatalog;
        private IInstantiator _instantiator;
        private IReadOnlyShipRunModel _run;
        private IReadOnlyShipSocketsModel _sockets;
        private ShipSeat _seat;
        private ShipSocketRule _rule;
        private ShipSocketState _shownState;
        private GameObject _spawnedView;
        private ISeatStation _station;
        private ISeatStationOwnerInput _stationOwnerInput;
        private PlayerKey _stationOccupant;

        public string SocketId => _socketId;
        public ShipModuleType AcceptedType => _acceptedType;
        public bool RequiredForLaunch => _requiredForLaunch;
        public bool IsOccupied => State.Occupied;
        public string InstalledModuleId => IsOccupied ? State.ModuleId : null;
        public PlayerKey Occupant => new PlayerKey(State.OccupantKey);
        public bool HasOccupant => Occupant.IsEmpty == false;
        public ShipSeat Seat => _seat;
        public ISeatStation Station => _station;
        public bool IsSittable => _seat != null;

        public bool CanAccept(ShipModuleType type) =>
            Rule.CanInstall(IsOccupied, type, RunState);

        public bool CanUninstall =>
            Rule.CanUninstall(IsOccupied, IsInstalledModuleRemovable, RunState);

        public bool IsUnlocked => Rule.IsUnlocked(RunState);

        private ShipSocketRule Rule => _rule ??= new ShipSocketRule(_acceptedType, _unlockLoop, StartingModuleId);

        private string StartingModuleId => _startingModule != null ? _startingModule.Id : null;

        private bool IsInstalledModuleRemovable =>
            TryGetInstalledModule(out ModuleDefinition module) && module.IsRemovable;

        private ShipSocketRunState RunState =>
            new ShipSocketRunState(_run.LoopIndex, _run.Phase);

        // A socket the server has not written yet is empty.
        private ShipSocketState State =>
            _sockets.States.TryGetValue(_socketId, out ShipSocketState state) ? state : default;

        [Inject]
        private void InjectDependencies(
            ModuleCatalog moduleCatalog,
            IReadOnlyShipRunModel run,
            IReadOnlyShipSocketsModel sockets,
            IInstantiator instantiator) {
            _moduleCatalog = moduleCatalog;
            _instantiator = instantiator;
            _run = run;
            _sockets = sockets;
        }

        private void Awake() =>
            Assert.IsFalse(string.IsNullOrEmpty(_socketId), name + " needs a socket id unique within the ship.");

        // The sockets bridge may start before or after this socket; when it starts later, its availability change
        // shows the full state.
        public override void OnStartClient() {
            _sockets.OnStatesChanged += HandleSocketsChanged;
            _sockets.OnAvailableChanged += HandleSocketsChanged;
            _shownState = State;
            RefreshView();
        }

        public override void OnStopClient() {
            _sockets.OnStatesChanged -= HandleSocketsChanged;
            _sockets.OnAvailableChanged -= HandleSocketsChanged;
        }

        public override void OnStartServer() =>
            RefreshView();

        internal void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            if (IsOccupied && IsSittable)
                _outline.enabled = hovered && HasOccupant == false;
            else if (IsOccupied)
                _outline.enabled = hovered && CanUninstall;
            else
                _outline.enabled = hovered && IsUnlocked;
        }

        public bool TryGetInstalledModule(out ModuleDefinition module) {
            module = null;
            return IsOccupied && _moduleCatalog.TryGet(State.ModuleId, out module);
        }

        public bool ServerTryInstall(string moduleId) {
            if (isServer == false)
                return false;

            if (_moduleCatalog.TryGet(moduleId, out ModuleDefinition module) == false)
                throw new InvalidOperationException(string.Format(UNKNOWN_MODULE_INSTALL_FORMAT, name, moduleId, nameof(ModuleCatalog)));

            if (CanAccept(module.SlotType) == false)
                return false;

            ShipSocketState state = State;
            state.ModuleId = module.Id;
            state.Occupied = true;
            ServerWriteState(state);
            _ship.Modules.ServerOnModuleInstalled(this, module);
            return true;
        }

        public bool IsHeldBy(PlayerKey rider) =>
            _seatRule.IsHeldBy(Occupant, rider);

        internal bool CanSeat(PlayerKey rider) =>
            _seatRule.CanSit(IsSittable, Occupant, rider);

        internal bool ServerTrySit(PlayerKey rider) {
            if (isServer == false || CanSeat(rider) == false)
                return false;

            ServerWriteOccupant(rider);
            return true;
        }

        internal void ServerStand(PlayerKey rider) {
            if (isServer == false)
                return;

            if (IsHeldBy(rider))
                ServerWriteOccupant(default);
        }

        internal void ServerClearOccupant() {
            if (isServer == false || HasOccupant == false)
                return;

            ServerWriteOccupant(default);
        }

        // Only the seat's occupant sends it (ShipSeats routes by the sender's key); a station that takes none ignores it.
        internal void ServerReceiveStationInput(float value) {
            if (isServer && _stationOwnerInput != null)
                _stationOwnerInput.ServerReceiveOwnerInput(value);
        }

        internal void ServerClearInstall() {
            if (isServer == false)
                return;

            _ship.Modules.ServerOnModuleUninstalled(this);

            ServerWriteState(default);
        }

        internal void ServerResetInstall() {
            if (isServer == false)
                return;

            _ship.Modules.ServerOnModuleUninstalled(this);

            ServerWriteState(Rule.CreateResetState());
            if (_startingModule != null)
                _ship.Modules.ServerOnModuleInstalled(this, _startingModule);
        }

        internal Vector3 ResolveSitLocalOffset(Transform ship) {
            Transform point = _seat != null ? _seat.SitPoint : transform;
            return Quaternion.Inverse(ship.rotation) * (point.position - ship.position);
        }

        private void ServerWriteOccupant(PlayerKey rider) {
            ShipSocketState state = State;
            state.OccupantKey = rider.IsEmpty ? string.Empty : rider.Id;
            ServerWriteState(state);
        }

        private void ServerWriteState(ShipSocketState state) =>
            _ship.SocketStates.ServerSetStates(_socketId, state);

        private void RefreshView() {
            ClearSpawnedView();
            _seat = null;
            _station = null;
            _stationOwnerInput = null;
            if (IsOccupied == false)
                return;

            SetHovered(false);
            // A synced id this peer's catalog does not know: report it, but keep the client running.
            if (TryGetInstalledModule(out ModuleDefinition module) == false) {
                UnityEngine.Debug.LogError(string.Format(UNKNOWN_MODULE_VIEW_FORMAT, name, State.ModuleId, nameof(ModuleCatalog)));
                return;
            }

            GameObject prefab = module.SlotViewPrefab;
            Transform point = ResolveInstallPoint(module);
            _spawnedView = _instantiator.InstantiatePrefab(prefab, point);
            _spawnedView.transform.localPosition = Vector3.zero;
            _spawnedView.transform.localRotation = Quaternion.identity;
            ApplyWorldScale(_spawnedView.transform, prefab.transform.localScale);
            _seat = _spawnedView.GetComponentInChildren<ShipSeat>();
            _station = _spawnedView.GetComponentInChildren<ISeatStation>();
            _stationOwnerInput = _spawnedView.GetComponentInChildren<ISeatStationOwnerInput>();
            SyncStationOccupant();
        }

        private Transform ResolveInstallPoint(ModuleDefinition module) {
            if (_viewInstallPoints != null) {
                for (int i = 0; i < _viewInstallPoints.Length; i++) {
                    ItemViewInstallPoint entry = _viewInstallPoints[i];
                    if (entry == null || entry.Module != module || entry.Point == null)
                        continue;

                    return entry.Point;
                }
            }

            return _defaultInstallPoint != null ? _defaultInstallPoint : transform;
        }

        private static void ApplyWorldScale(Transform target, Vector3 worldScale) {
            Transform parent = target.parent;
            if (parent == null) {
                target.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                parentScale.x == 0f ? worldScale.x : worldScale.x / parentScale.x,
                parentScale.y == 0f ? worldScale.y : worldScale.y / parentScale.y,
                parentScale.z == 0f ? worldScale.z : worldScale.z / parentScale.z);
        }

        private void ClearSpawnedView() {
            if (_spawnedView == null)
                return;

            LeaveStation();
            Destroy(_spawnedView);
            _spawnedView = null;
        }

        // A remote client's bridge clears the model on stop, after IsAvailable turned false: the modules and seats shown
        // then stay as they were, as before the model.
        private void HandleSocketsChanged() {
            if (_sockets.IsAvailable == false)
                return;

            ShipSocketState shown = _shownState;
            _shownState = State;
            PlayerKey shownOccupant = new PlayerKey(shown.OccupantKey);
            if (shownOccupant.Equals(Occupant) == false)
                HandleOccupantChanged(shownOccupant, Occupant);

            if (shown.Occupied != _shownState.Occupied || shown.ModuleId != _shownState.ModuleId)
                RefreshView();
        }

        private void HandleOccupantChanged(PlayerKey previous, PlayerKey current) {
            SetHovered(false);
            _ship.Seats.ClientSyncSeat(this, previous, current);
            SyncStationOccupant();
        }

        // A station (a turret, the helm) hears who sits in it on every peer; a view without one only seats.
        private void SyncStationOccupant() {
            PlayerKey occupant = Occupant;
            if (_station == null || occupant.Equals(_stationOccupant))
                return;

            LeaveStation();
            _stationOccupant = occupant;
            if (occupant.IsEmpty == false)
                _station.Enter(occupant);
        }

        private void LeaveStation() {
            if (_station != null && _stationOccupant.IsEmpty == false)
                _station.Exit();

            _stationOccupant = default;
        }
    }
}
