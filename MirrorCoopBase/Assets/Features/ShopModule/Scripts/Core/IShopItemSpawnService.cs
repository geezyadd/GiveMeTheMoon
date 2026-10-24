using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopItemSpawnService {
        public void SpawnAtDelivery(GameObject itemPrefab, Transform deliveryPoint);
    }
}
