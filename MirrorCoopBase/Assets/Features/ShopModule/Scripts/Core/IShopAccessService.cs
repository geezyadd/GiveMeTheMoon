using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopAccessService {
        public ShopAccessStatus Evaluate(Vector3 playerPosition, bool isAlive, Vector3 kioskPosition, ShopAccessRange range);
    }
}
