namespace Features.ShopModule.Scripts.Core {
    public interface IReviveRule {
        public ReviveStatus Evaluate(ReviveRequest request);
        public bool CanAfford(long balance, long price);
        public bool IsPriceValid(long price);
    }
}
