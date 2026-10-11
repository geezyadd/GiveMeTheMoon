using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Mirror;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    // Spawns a bought item at the kiosk's delivery point, above the items already lying there, so it never
    // spawns inside one of them.
    public sealed class ShopItemSpawnService : IShopItemSpawnService {
        private const int MAX_DELIVERY_COLLIDERS = 32;
        private const float GOLDEN_ANGLE_DEGREES = 137.5f;

        private readonly Collider[] _deliveryColliders = new Collider[MAX_DELIVERY_COLLIDERS];
        private readonly ShopKioskConfiguration _shopKioskConfiguration;
        private readonly GrabConfiguration _grabConfiguration;

        public ShopItemSpawnService(ShopKioskConfiguration shopKioskConfiguration, GrabConfiguration grabConfiguration) {
            _shopKioskConfiguration = shopKioskConfiguration;
            _grabConfiguration = grabConfiguration;
        }

        public void SpawnAtDelivery(ShipItem itemPrefab, Transform deliveryPoint) {
            int itemsThere = CountItemsAt(deliveryPoint.position);
            Quaternion scatterTurn = Quaternion.AngleAxis(itemsThere * GOLDEN_ANGLE_DEGREES, Vector3.up);
            Vector3 scatter = scatterTurn * deliveryPoint.forward * (itemsThere > 0 ? _shopKioskConfiguration.DeliveryScatter : 0f);
            Vector3 position = deliveryPoint.position
                + scatter
                + Vector3.up * (itemsThere * _shopKioskConfiguration.DeliveryStackStep);
            GameObject instance = Object.Instantiate(itemPrefab.gameObject, position, deliveryPoint.rotation);
            NetworkServer.Spawn(instance);
        }

        private int CountItemsAt(Vector3 point) {
            int count = Physics.OverlapSphereNonAlloc(
                point,
                _shopKioskConfiguration.DeliveryOccupancyRadius,
                _deliveryColliders,
                _grabConfiguration.InteractableMask,
                QueryTriggerInteraction.Ignore);
            int items = 0;
            for (int i = 0; i < count; i++) {
                Grabbable item = _deliveryColliders[i].GetComponentInParent<Grabbable>();
                if (item != null && IsFirstColliderOf(item, i))
                    items++;
            }

            return items;
        }

        // An item can have several colliders: only its first one in the buffer counts.
        private bool IsFirstColliderOf(Grabbable item, int index) {
            for (int i = 0; i < index; i++)
                if (_deliveryColliders[i].GetComponentInParent<Grabbable>() == item)
                    return false;

            return true;
        }
    }
}
