namespace Features.ShopModule.Scripts.Core {
    public interface ICrewWallet {
        public long Balance { get; }
        public bool ServerTrySpend(long amount);
        // Server: adds income (a popped target balloon's reward) to the crew balance.
        public void ServerEarn(long amount);
    }
}
