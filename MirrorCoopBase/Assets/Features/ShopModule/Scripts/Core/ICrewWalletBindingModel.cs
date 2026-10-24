namespace Features.ShopModule.Scripts.Core {
    public interface ICrewWalletBindingModel {
        public ICrewWallet Wallet { get; }
        public bool IsBound { get; }
        public void Bind(ICrewWallet wallet);
        public void Clear(ICrewWallet wallet);
    }
}
