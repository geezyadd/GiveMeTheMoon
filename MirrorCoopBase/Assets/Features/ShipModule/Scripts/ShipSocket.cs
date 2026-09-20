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

        private ItemViewCatalog _catalog;
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

        public bool IsUnlocked => _run == null || _run.LoopIndex >= _unlockLoop;

        public bool HasHelmPilot =>
            _occupantNetId != 0 && _seat != null && _seat.Role == ShipSeatRole.Helm;

        [Inject]
        private void Construct(ItemViewCatalog catalog, [Inject(Optional = true)] ShipRunModel run) {
            _catalog = catalog;
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
            else
                _outline.enabled = hovered && _occupied == false && IsUnlocked;
        }

        internal bool ServerTryInstall(ShipModuleType type, ItemViewId view) {
            if (isServer == false || CanAccept(type) == false || view == ItemViewId.None)
                return false;

            _installedViewId = view;
            _occupied = true;
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

        private void ClearSpawnedView() {
            if (_spawnedView == null)
                return;

            Destroy(_spawnedView);
            _spawnedView = null;
        }
    }
}
