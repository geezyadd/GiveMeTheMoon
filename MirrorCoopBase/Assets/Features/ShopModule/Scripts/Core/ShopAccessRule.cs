namespace Features.ShopModule.Scripts.Core {
    // Whether a player may use the shop kiosk: only at a station, only alive, only close to the kiosk.
    public sealed class ShopAccessRule : IShopAccessRule {
        public ShopAccessStatus Evaluate(bool isBuildPhase, bool isAlive, float distance, float maxDistance) {
            if (isBuildPhase == false)
                return ShopAccessStatus.NotBuildPhase;

            if (isAlive == false)
                return ShopAccessStatus.Dead;

            if (distance > maxDistance)
                return ShopAccessStatus.TooFar;

            return ShopAccessStatus.Allowed;
        }

        // The window must not flicker at the open distance, and a purchase from an open window must not be refused.
        public bool IsDistanceOrderValid(float openDistance, float keepOpenDistance, float purchaseDistance) =>
            openDistance < keepOpenDistance && keepOpenDistance <= purchaseDistance;
    }
}
