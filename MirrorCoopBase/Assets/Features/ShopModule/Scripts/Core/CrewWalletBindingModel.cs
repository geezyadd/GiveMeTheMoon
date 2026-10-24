namespace Features.ShopModule.Scripts.Core {
    // The server's crew wallet while the game scene runs; the wallet binds and clears itself, so a server system that
    // pays the crew reaches it without a scene lookup.
    public sealed class CrewWalletBindingModel : ICrewWalletBindingModel {
        public ICrewWallet Wallet { get; private set; }
        public bool IsBound => Wallet != null;

        public void Bind(ICrewWallet wallet) =>
            Wallet = wallet;

        // Only the wallet that is bound may clear: a stopping old scene must not unbind a new one.
        public void Clear(ICrewWallet wallet) {
            if (Wallet == wallet)
                Wallet = null;
        }
    }
}
