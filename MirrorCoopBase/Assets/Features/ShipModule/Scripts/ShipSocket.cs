using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSocket : NetworkBehaviour {
        [SerializeField] private string _socketId;
        [SerializeField] private ShipModuleType _acceptedType = ShipModuleType.Engine;
        [SerializeField] private bool _requiredForLaunch;
        [SerializeField] private Outline _outline;
        [SerializeField] private Transform _defaultInstallPoint;
        [SerializeField] private ItemViewInstallPoint[] _viewInstallPoints;
        [SerializeField] private int _unlockLoop;
        [SerializeField] private ShipBase _ship;

        private ItemViewCatalog _catalog;
        private ShipRadarCatalog _radarCatalog;
        private IReadOnlyShipRunModel _run;
        private IReadOnlyShipSocketsModel _sockets;
        private ShipSeat _seat;
        private ShipSocketRule _rule;
        private ShipSocketState _shownState;
        private GameObject _spawnedView;

        public string SocketId => _socketId;
        public ShipModuleType AcceptedType => _acceptedType;
        public bool RequiredForLaunch => _requiredForLaunch;
        public bool IsOccupied => State.Occupied;
        public ItemViewId InstalledView => IsOccupied ? State.View : ItemViewId.None;
        public uint OccupantNetId => State.OccupantNetId;
        public ShipSeat Seat => _seat;
        public bool IsSittable => _seat != null;

        public bool CanAccept(ShipModuleType type) =>
            Rule.CanInstall(IsOccupied, type, RunState);

        public bool CanUninstall =>
            Rule.CanUninstall(IsOccupied, RunState);

        public bool IsUnlocked => Rule.IsUnlocked(RunState);

        private ShipSocketRule Rule => _rule ??= new ShipSocketRule(_acceptedType, _unlockLoop);

        private ShipSocketRunState RunState =>
            new ShipSocketRunState(_run.LoopIndex, _run.Phase);

        // A socket the server has not written yet is empty.
        private ShipSocketState State =>
            _sockets.States.TryGetValue(_socketId, out ShipSocketState state) ? state : default;

        public bool HasHelmPilot =>
            OccupantNetId != 0 && _seat != null && _seat.Role == ShipSeatRole.Helm;

        [Inject]
        private void InjectDependencies(
            ItemViewCatalog catalog,
            ShipRadarCatalog radarCatalog,
            IReadOnlyShipRunModel run,
            IReadOnlyShipSocketsModel sockets) {
            _catalog = catalog;
            _radarCatalog = radarCatalog;
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
                _outline.enabled = hovered && OccupantNetId == 0;
            else if (IsOccupied)
                _outline.enabled = hovered && CanUninstall;
            else
                _outline.enabled = hovered && IsUnlocked;
        }

        public bool ServerTryInstall(ShipModuleType type, ItemViewId view) {
            if (isServer == false || CanAccept(type) == false || view == ItemViewId.None)
                return false;

            ShipSocketState state = State;
            state.View = view;
            state.Occupied = true;
            ServerWriteState(state);
            _ship.Modules.ServerOnModuleInstalled(this);
            return true;
        }

        internal bool CanSeat(uint riderNetId) =>
            IsSittable && riderNetId != 0 && OccupantNetId == 0;

        internal bool ServerTrySit(uint riderNetId) {
            if (isServer == false || CanSeat(riderNetId) == false)
                return false;

            ServerWriteOccupant(riderNetId);
            return true;
        }

        internal void ServerStand(uint riderNetId) {
            if (isServer == false)
                return;

            if (OccupantNetId == riderNetId)
                ServerWriteOccupant(0);
        }

        internal void ServerClearOccupant() {
            if (isServer == false || OccupantNetId == 0)
                return;

            ServerWriteOccupant(0);
        }

        internal void ServerClearInstall() {
            if (isServer == false)
                return;

            _ship.Modules.ServerOnModuleUninstalled(this);

            ServerWriteState(default);
        }

        internal Vector3 ResolveSitLocalOffset(Transform ship) {
            Transform point = _seat != null ? _seat.SitPoint : transform;
            return Quaternion.Inverse(ship.rotation) * (point.position - ship.position);
        }

        private void ServerWriteOccupant(uint riderNetId) {
            ShipSocketState state = State;
            state.OccupantNetId = riderNetId;
            ServerWriteState(state);
        }

        private void ServerWriteState(ShipSocketState state) =>
            _ship.SocketStates.ServerSetStates(_socketId, state);

        private void RefreshView() {
            ClearSpawnedView();
            _seat = null;
            ItemViewId view = InstalledView;
            if (view == ItemViewId.None)
                return;

            SetHovered(false);
            if (_catalog == null || _catalog.TryGetPrefab(view, out GameObject prefab) == false)
                return;

            Transform point = ResolveInstallPoint(view);
            _spawnedView = Instantiate(prefab, point);
            _spawnedView.transform.localPosition = Vector3.zero;
            _spawnedView.transform.localRotation = Quaternion.identity;
            ApplyWorldScale(_spawnedView.transform, prefab.transform.localScale);
            _seat = _spawnedView.GetComponentInChildren<ShipSeat>();
            BindRadarScreen(_spawnedView);
        }

        private Transform ResolveInstallPoint(ItemViewId view) {
            if (_viewInstallPoints != null) {
                for (int i = 0; i < _viewInstallPoints.Length; i++) {
                    ItemViewInstallPoint entry = _viewInstallPoints[i];
                    if (entry == null || entry.View != view || entry.Point == null)
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

        private void BindRadarScreen(GameObject view) {
            ShipRadarScreen screen = view.GetComponentInChildren<ShipRadarScreen>(true);
            if (screen != null)
                screen.Bind(_radarCatalog, _run, _ship);
        }

        private void ClearSpawnedView() {
            if (_spawnedView == null)
                return;

            ShipRadarScreen screen = _spawnedView.GetComponentInChildren<ShipRadarScreen>(true);
            if (screen != null)
                screen.Unbind();

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
            if (shown.OccupantNetId != _shownState.OccupantNetId)
                HandleOccupantChanged(shown.OccupantNetId, _shownState.OccupantNetId);

            if (shown.Occupied != _shownState.Occupied || shown.View != _shownState.View)
                RefreshView();
        }

        private void HandleOccupantChanged(uint previous, uint current) {
            SetHovered(false);
            _ship.Seats.ClientSyncSeat(this, previous, current);
        }
    }
}
