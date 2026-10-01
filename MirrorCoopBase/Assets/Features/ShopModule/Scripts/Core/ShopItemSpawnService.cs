using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Mirror;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public sealed class ShopItemSpawnService : IShopItemSpawnService {
        private readonly ShopCatalog _shopCatalog;

        public ShopItemSpawnService(ShopCatalog shopCatalog) =>
            _shopCatalog = shopCatalog;

        public void SpawnNear(ShipItem itemPrefab, Transform buyer) {
            Vector3 forward = Vector3.ProjectOnPlane(buyer.forward, Vector3.up).normalized;
            Vector3 position = buyer.position
                + forward * _shopCatalog.SpawnDistance
                + Vector3.up * _shopCatalog.SpawnHeight;
            GameObject instance = Object.Instantiate(itemPrefab.gameObject, position, Quaternion.LookRotation(forward, Vector3.up));
            NetworkServer.Spawn(instance);
        }
    }
}
