using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSocket : NetworkBehaviour {
        [SerializeField] private ShipModuleType _acceptedType = ShipModuleType.Engine;
        [SerializeField] private bool _requiredForLaunch;
        [SerializeField] private Outline _outline;
        [SerializeField] private Transform _defaultInstallPoint;
        [SerializeField] private ItemViewInstallPoint[] _viewInstallPoints;
        [SerializeField] private int _unlockLoop;
        [SerializeField] private ShipBase _ship;

        private ItemViewCatalog _catalog;
        private ShipRadarCatalog _radarCatalog;
        private ShipSeat _seat;
        private ShipRunModel _run;

        [SyncVar(hook = nameof(OnOccupiedChanged))]
        private bool _occupied;

        [SyncVar(hook = nameof(OnInstalledViewChanged))]
        private ItemViewId _installedViewId;

        [SyncVar(hook = nameof(OnOccupantChanged))]
        private uint _occupantNetId;

        private GameObject _spawnedView;

        public ShipModuleType AcceptedType => _acceptedType;
        public bool RequiredForLaunch => _requiredForLaunch;
        public bool IsOccupied => _occupied;
        public ItemViewId InstalledView => _occupied ? _installedViewId : ItemViewId.None;
        public uint OccupantNetId => _occupantNetId;
        public ShipSeat Seat => _seat;
        public bool IsSittable => _seat != null;

        public bool CanAccept(ShipModuleType type) =>
            _occupied == false && type == _acceptedType && IsUnlocked;

        public bool CanUninstall =>
            _occupied && IsBuildPhase && CanRemoveModule(_acceptedType);

        public bool IsUnlocked => _run == null || _run.LoopIndex >= _unlockLoop;

        private bool IsBuildPhase =>
            _run == null || _run.Phase == ShipRunPhase.Build;

        public bool HasHelmPilot =>
            _occupantNetId != 0 && _seat != null && _seat.Role == ShipSeatRole.Helm;

        [Inject]
        private void Construct(
            ItemViewCatalog catalog,
            ShipRadarCatalog radarCatalog,
            [Inject(Optional = true)] ShipRunModel run) {
            _catalog = catalog;
            _radarCatalog = radarCatalog;
            _run = run;
            RefreshView();
        }

        public override void OnStartClient() {
            RefreshView();
        }

        public override void OnStartServer() {
            RefreshView();
        }

        internal void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            if (_occupied && IsSittable)
                _outline.enabled = hovered && _occupantNetId == 0;
            else if (_occupied)
                _outline.enabled = hovered && CanUninstall;
            else
                _outline.enabled = hovered && IsUnlocked;
        }

        internal bool ServerTryInstall(ShipModuleType type, ItemViewId view) {
            if (isServer == false || CanAccept(type) == false || view == ItemViewId.None)
                return false;

            _installedViewId = view;
            _occupied = true;
            if (_ship != null)
                _ship.ServerOnModuleInstalled(this);
            return true;
        }

        internal bool ServerTrySit(uint riderNetId) {
            if (isServer == false || IsSittable == false || riderNetId == 0 || _occupantNetId != 0)
                return false;

            _occupantNetId = riderNetId;
            return true;
        }

        internal void ServerStand(uint riderNetId) {
            if (isServer == false)
                return;

            if (_occupantNetId == riderNetId)
                _occupantNetId = 0;
        }

        internal void ServerClearOccupant() {
            if (isServer == false)
                return;

            _occupantNetId = 0;
        }

        internal void ServerClearInstall() {
            if (isServer == false)
                return;

            if (_ship != null)
                _ship.ServerOnModuleUninstalled(this);

            _occupantNetId = 0;
            _occupied = false;
            _installedViewId = ItemViewId.None;
        }

        internal Vector3 ResolveSitLocalOffset(Transform ship) {
            Transform point = _seat != null ? _seat.SitPoint : transform;
            return Quaternion.Inverse(ship.rotation) * (point.position - ship.position);
        }

        private void OnOccupiedChanged(bool previous, bool current) {
            RefreshView();
        }

        private void OnInstalledViewChanged(ItemViewId previous, ItemViewId current) {
            RefreshView();
        }

        private void OnOccupantChanged(uint previous, uint current) {
            SetHovered(false);
        }

        private void RefreshView() {
            ClearSpawnedView();
            _seat = null;
            if (_occupied == false || _installedViewId == ItemViewId.None)
                return;

            SetHovered(false);
            if (_catalog == null || _catalog.TryGetPrefab(_installedViewId, out GameObject prefab) == false)
                return;

            Transform point = ResolveInstallPoint(_installedViewId);
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

        private static bool CanRemoveModule(ShipModuleType type) {
            return type == ShipModuleType.Engine || type == ShipModuleType.Radar;
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
    }
}
