namespace Features.ShopModule.Scripts.Core {
    public interface IShopAccessRule {
        public ShopAccessStatus Evaluate(bool isBuildPhase, bool isAlive, float distance, float maxDistance);
        public bool IsDistanceOrderValid(float openDistance, float keepOpenDistance, float purchaseDistance);
    }
}
