using MiniMapModular;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRadarScreen : MonoBehaviour {
        private const string CameraName = "Cam_Map_Module";

        [SerializeField] private RectTransform _slot;

        private ShipRadarCatalog _catalog;
        private ShipRunModel _model;
        private ShipBase _ship;
        private RectTransform _slotLive;
        private GameObject _map;
        private Custom_MP _custom;
        private GameObject _cameraObject;
        private Transform _follow;
        private MP _minimap;
        private RectTransform _playerIcon;
        private RectTransform _sweep;
        private RectTransform _arrow;
        private Image _arrowImage;
        private float _sweepZ;

        public void Bind(ShipRadarCatalog catalog, ShipRunModel model, ShipBase ship) {
            Unbind();
            _catalog = catalog;
            _model = model;
            _ship = ship;
            _slotLive = _slot != null ? _slot : transform as RectTransform;
            EnsureInstances();
        }

        public void Unbind() {
            if (_arrow != null)
                Destroy(_arrow.gameObject);
            if (_map != null)
                Destroy(_map);
            if (_cameraObject != null)
                Destroy(_cameraObject);
            if (_follow != null)
                Destroy(_follow.gameObject);

            _arrow = null;
            _arrowImage = null;
            _playerIcon = null;
            _sweep = null;
            _map = null;
            _custom = null;
            _cameraObject = null;
            _follow = null;
            _minimap = null;
            _catalog = null;
            _model = null;
            _ship = null;
            _slotLive = null;
        }

        private void OnDestroy() {
            Unbind();
        }

        private void LateUpdate() {
            if (_map == null || _model == null)
                return;

            bool visible = _ship != null && IsFlightPhase(_model.Phase);
            if (_map.activeSelf != visible)
                _map.SetActive(visible);

            if (visible == false)
                return;

            _follow.SetPositionAndRotation(_ship.transform.position, _ship.transform.rotation);
            UpdateBearings();
        }

        private void EnsureInstances() {
            if (_catalog == null || _slotLive == null || _catalog.CamMapPrefab == null || _catalog.MapStatusPrefab == null)
                return;

            _follow = new GameObject("ModuleRadarFollow").transform;

            var holder = new GameObject("ModuleRadarCamHolder");
            holder.SetActive(false);
            _cameraObject = Instantiate(_catalog.CamMapPrefab, holder.transform);
            _cameraObject.name = CameraName;
            _minimap = _cameraObject.GetComponent<MP>();

            _map = Instantiate(_catalog.MapStatusPrefab, _slotLive, false);
            _custom = _map.GetComponent<Custom_MP>();
            RectTransform mapRect = _map.GetComponent<RectTransform>();
            if (mapRect != null) {
                mapRect.anchorMin = Vector2.zero;
                mapRect.anchorMax = Vector2.one;
                mapRect.pivot = new Vector2(0.5f, 0.5f);
                mapRect.offsetMin = Vector2.zero;
                mapRect.offsetMax = Vector2.zero;
                mapRect.localScale = new Vector3(-1f, 1f, 1f);
                mapRect.localRotation = Quaternion.identity;
            }

            if (_minimap != null) {
                _minimap.MS = _custom;
                _minimap.Player_RY = _follow;
                _minimap.Cam_MapT = _cameraObject.transform;
                _minimap.CamFixed = 1;
                _minimap.Radar_On = 1;
                _minimap.MM_Posy = Vector2.zero;
                if (_custom != null) {
                    _minimap.MPT = _custom.MPT;
                    _minimap.MPT1 = _custom.MPT1;
                    _minimap.MPT2 = _custom.MPT2;
                }
            }

            if (_custom != null) {
                _playerIcon = _custom.IconPlayerR;
                _sweep = _custom.RadarT;
            }

            holder.SetActive(true);
            _cameraObject.transform.SetParent(null);
            Destroy(holder);
            CreateHeadingArrow();
            _map.SetActive(false);
        }

        private void CreateHeadingArrow() {
            if (_catalog.IconArrowPrefab == null || _custom == null || _custom.MPT == null)
                return;

            GameObject arrowObject = Instantiate(_catalog.IconArrowPrefab, _custom.MPT);
            arrowObject.SetActive(true);
            _arrow = arrowObject.GetComponent<RectTransform>();
            _arrow.anchoredPosition = Vector2.zero;
            _arrow.localPosition = Vector3.zero;
            _arrow.localScale = Vector3.one;
            _arrow.localRotation = Quaternion.identity;

            Aux_Icon aux = arrowObject.GetComponent<Aux_Icon>();
            _arrowImage = aux != null ? aux.Local_Icon : null;
            if (_arrowImage != null) {
                Sprite sprite = ReadPrefabSprite(_catalog.IconArrowPrefab);
                if (sprite != null)
                    _arrowImage.sprite = sprite;
                _arrowImage.color = new Color(0.2f, 1f, 0.35f, 1f);
            }
        }

        private void UpdateBearings() {
            float yaw = _ship.transform.eulerAngles.y;
            if (_playerIcon != null)
                _playerIcon.localEulerAngles = new Vector3(0f, 0f, -yaw);

            if (_sweep != null) {
                float speed = _minimap != null ? _minimap.SpeedRad1 : 150f;
                _sweepZ -= speed * Time.deltaTime;
                _sweep.localEulerAngles = new Vector3(0f, 0f, _sweepZ);
            }

            if (_arrow == null)
                return;

            Vector3 destDir = _model.TransitDestination - _ship.transform.position;
            destDir.y = 0f;
            if (destDir.sqrMagnitude < 0.0001f)
                return;

            float destAngle = -Mathf.Atan2(destDir.x, destDir.z) * Mathf.Rad2Deg;
            _arrow.localEulerAngles = new Vector3(0f, 0f, destAngle);
        }

        private static Sprite ReadPrefabSprite(GameObject prefab) {
            if (prefab == null)
                return null;

            Aux_Icon aux = prefab.GetComponent<Aux_Icon>();
            if (aux == null || aux.Local_Icon == null)
                return null;

            return aux.Local_Icon.sprite;
        }

        private static bool IsFlightPhase(ShipRunPhase phase) {
            return phase == ShipRunPhase.Takeoff
                || phase == ShipRunPhase.Cruise
                || phase == ShipRunPhase.Landing;
        }
    }
}
