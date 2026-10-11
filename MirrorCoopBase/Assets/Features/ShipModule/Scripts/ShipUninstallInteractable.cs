using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipUninstallInteractable : InteractableBase {
        private const string MISSING_ITEM_ERROR_FORMAT =
            "{0}: module '{1}' has no item prefab in the {2}, it stays in the slot.";

        [SerializeField] private ShipSocket _socket;

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || _socket.CanUninstall == false)
                return false;

            return grab.IsHandFree;
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) {
            if (_socket == null || grab == null || grab.IsHolding)
                return;

            if (TryGetInstalledItemPrefab(out ShipItem prefab) == false) {
                UnityEngine.Debug.LogError(string.Format(MISSING_ITEM_ERROR_FORMAT, _socket.name, _socket.InstalledModuleId, nameof(ModuleCatalog)));
                return;
            }

            Transform arm = grab.ArmPoint;
            Vector3 position = arm != null ? arm.position : _socket.transform.position;
            Quaternion rotation = arm != null ? arm.rotation : _socket.transform.rotation;
            GameObject instance = Instantiate(prefab.gameObject, position, rotation);
            NetworkServer.Spawn(instance);
            Grabbable item = instance.GetComponent<Grabbable>();
            if (item == null || grab.ServerGive(item) == false) {
                NetworkServer.Destroy(instance);
                return;
            }

            _socket.ServerClearInstall();
        }

        private bool TryGetInstalledItemPrefab(out ShipItem prefab) {
            prefab = null;
            if (_socket.TryGetInstalledModule(out ModuleDefinition module) == false)
                return false;

            prefab = module.ItemPrefab;
            return prefab != null;
        }
    }
}
