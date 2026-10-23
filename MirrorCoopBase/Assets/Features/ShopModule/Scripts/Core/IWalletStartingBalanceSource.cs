namespace Features.ShopModule.Scripts.Core {
    // The crew's balance at the start of a run.
    public interface IWalletStartingBalanceSource {
        public long StartingBalance { get; }
    }
}
