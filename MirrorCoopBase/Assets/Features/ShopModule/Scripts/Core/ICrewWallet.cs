namespace Features.ShopModule.Scripts.Core {
    public interface ICrewWallet {
        public long Balance { get; }
        public bool ServerTrySpend(long amount);
        // Server: adds income (the price of ore that reached the ship) to the crew balance.
        public void ServerEarn(long amount);
    }
}
