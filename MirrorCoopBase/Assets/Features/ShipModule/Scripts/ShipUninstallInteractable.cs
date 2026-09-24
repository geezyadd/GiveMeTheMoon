using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipUninstallInteractable : InteractableBase {
        [SerializeField] private ShipSocket _socket;

        private ShipStationCatalog _stations;

        [Inject]
        private void Construct(ShipStationCatalog stations) {
            _stations = stations;
        }

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || _socket.CanUninstall == false)
                return false;

            return grab == null || grab.IsHolding == false;
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || grab == null || grab.IsHolding)
                return;

            ItemViewId view = _socket.InstalledView;
            if (_stations == null || _stations.TryGetItemPrefab(view, out GameObject prefab) == false)
                return;

            Transform arm = grab.ArmPoint;
            Vector3 position = arm != null ? arm.position : _socket.transform.position;
            Quaternion rotation = arm != null ? arm.rotation : _socket.transform.rotation;
            GameObject instance = Instantiate(prefab, position, rotation);
            NetworkServer.Spawn(instance);
            Grabbable item = instance.GetComponent<Grabbable>();
            if (item == null || grab.ServerGive(item) == false) {
                NetworkServer.Destroy(instance);
                return;
            }

            _socket.ServerClearInstall();
        }
    }
}
