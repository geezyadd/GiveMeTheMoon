using Features.GameCoreModule.Scripts;
using MiniMapModular;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRadarService : ITickable, IGameplaySession {
        private const string CameraName = "Cam_Map";

        private readonly ShipRadarCatalog _catalog;
        private readonly ShipRunModel _model;

        private ShipBase _ship;
        private RectTransform _slot;
        private GameObject _map;
        private GameObject _cameraObject;
        private Transform _follow;
        private GameObject _beacon;
        private MP _minimap;

        public ShipRadarService(ShipRadarCatalog catalog, ShipRunModel model) {
            _catalog = catalog;
            _model = model;
        }

        public void CleanupGameplay() {
            UnbindShip();
            TearDown();
        }

        public void RestartGameplay() {
            UnbindShip();
            TearDown();
        }

        public void BindShip(ShipBase ship) {
            _ship = ship;
        }

        public void UnbindShip() {
            _ship = null;
        }

        public void Attach(RectTransform slot) {
            _slot = slot;
            EnsureInstances();
        }

        public void Detach() {
            _slot = null;
            TearDown();
        }

        public void Tick() {
            if (_slot != null)
                EnsureInstances();

            if (_map == null)
                return;

            bool visible = _slot != null && _ship != null && IsFlightPhase(_model.Phase);
            if (_map.activeSelf != visible)
                _map.SetActive(visible);

            if (visible == false)
                return;

            _follow.SetPositionAndRotation(_ship.transform.position, _ship.transform.rotation);
            if (_beacon != null)
                _beacon.transform.position = _model.TransitDestination;
        }

        private void EnsureInstances() {
            if (_catalog == null || _slot == null)
                return;

            if (_follow == null) {
                var followObject = new GameObject("RadarFollow");
                _follow = followObject.transform;
            }

            if (_cameraObject == null && _catalog.CamMapPrefab != null && _catalog.MapStatusPrefab != null) {
                var holder = new GameObject("RadarCamHolder");
                holder.SetActive(false);
                _cameraObject = Object.Instantiate(_catalog.CamMapPrefab, holder.transform);
                _cameraObject.name = CameraName;
                _minimap = _cameraObject.GetComponent<MP>();

                _map = Object.Instantiate(_catalog.MapStatusPrefab, _slot, false);
                RectTransform mapRect = _map.GetComponent<RectTransform>();
                if (mapRect != null) {
                    mapRect.anchorMin = Vector2.zero;
                    mapRect.anchorMax = Vector2.one;
                    mapRect.pivot = new Vector2(0.5f, 0.5f);
                    mapRect.offsetMin = Vector2.zero;
                    mapRect.offsetMax = Vector2.zero;
                    mapRect.localScale = Vector3.one;
                    mapRect.localRotation = Quaternion.identity;
                }

                Custom_MP custom = _map.GetComponent<Custom_MP>();
                if (_minimap != null) {
                    _minimap.MS = custom;
                    _minimap.Player_RY = _follow;
                    _minimap.Cam_MapT = _cameraObject.transform;
                    _minimap.CamFixed = 1;
                    _minimap.Radar_On = 1;
                    _minimap.MM_Posy = Vector2.zero;
                    if (custom != null) {
                        _minimap.MPT = custom.MPT;
                        _minimap.MPT1 = custom.MPT1;
                        _minimap.MPT2 = custom.MPT2;
                    }
                }

                holder.SetActive(true);
                _cameraObject.transform.SetParent(null);
                Object.Destroy(holder);
                _map.SetActive(false);
            }

            if (_beacon == null && _cameraObject != null)
                _beacon = CreateBeacon();
        }

        private GameObject CreateBeacon() {
            var beacon = new GameObject("RadarDestination");
            beacon.SetActive(false);
            Aux_MP icon = beacon.AddComponent<Aux_MP>();
            icon.Icon = _catalog.IconPrefab;
            icon.Icon_Arrow = _catalog.IconArrowPrefab;
            icon.Arrow_Icon = _catalog.ArrowIconPrefab;
            icon.Sprite_Me = _catalog.DestinationSprite;
            icon.Sprite_Arrow = ReadPrefabSprite(_catalog.IconArrowPrefab);
            icon.Icon_C = new Color(0.2f, 1f, 0.35f, 1f);
            icon.Arrow_C = new Color(0.2f, 1f, 0.35f, 1f);
            icon.Arrow = 1;
            icon.Icon_Dir = 0;
            icon.HiddenIcon = 0;
            icon.RotIconMe = 0;
            icon.LayerIconView = 0;
            beacon.SetActive(true);
            return beacon;
        }

        private static Sprite ReadPrefabSprite(GameObject prefab) {
            if (prefab == null)
                return null;

            Aux_Icon aux = prefab.GetComponent<Aux_Icon>();
            if (aux == null || aux.Local_Icon == null)
                return null;

            return aux.Local_Icon.sprite;
        }

        private void TearDown() {
            if (_beacon != null)
                Object.Destroy(_beacon);
            if (_map != null)
                Object.Destroy(_map);
            if (_cameraObject != null)
                Object.Destroy(_cameraObject);
            if (_follow != null)
                Object.Destroy(_follow.gameObject);

            _beacon = null;
            _map = null;
            _cameraObject = null;
            _follow = null;
            _minimap = null;
        }

        private static bool IsFlightPhase(ShipRunPhase phase) {
            return phase == ShipRunPhase.Takeoff
                || phase == ShipRunPhase.Cruise
                || phase == ShipRunPhase.Landing;
        }
    }
}
