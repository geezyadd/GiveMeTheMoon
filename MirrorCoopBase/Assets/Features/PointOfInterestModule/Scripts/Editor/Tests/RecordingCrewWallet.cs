using Features.ShopModule.Scripts.Core;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    // A bound crew wallet without a network bridge: counts what it earned.
    public sealed class RecordingCrewWallet : ICrewWallet, ICrewWalletBindingModel {
        public long Balance { get; private set; }
        public int Earnings { get; private set; }
        public ICrewWallet Wallet => this;
        public bool IsBound => true;

        public bool ServerTrySpend(long amount) =>
            throw new System.NotSupportedException();

        public void ServerEarn(long amount) {
            Balance += amount;
            Earnings++;
        }

        public void Bind(ICrewWallet wallet) { }

        public void Clear(ICrewWallet wallet) { }
    }
}
