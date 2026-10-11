using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopItemSpawnService {
        public void SpawnAtDelivery(ShipItem itemPrefab, Transform deliveryPoint);
    }
}
