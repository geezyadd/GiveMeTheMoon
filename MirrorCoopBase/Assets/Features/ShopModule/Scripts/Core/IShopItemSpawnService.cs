using Features.GrabModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopItemSpawnService {
        public void SpawnAtDelivery(Grabbable itemPrefab, Transform deliveryPoint);
    }
}
