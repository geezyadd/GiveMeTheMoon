namespace Features.ShopModule.Scripts.Core {
    public interface ICrewWallet {
        public long Balance { get; }
        public bool ServerTrySpend(long amount);
    }
}
