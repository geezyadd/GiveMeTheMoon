using System;
using Features.ShopModule.Scripts.Core;

namespace Features.ShopModule.Editor.Tests {
    // A crew wallet without a network bridge: counts what it earned.
    public sealed class WalletStub : ICrewWallet {
        public long Balance { get; private set; }
        public int Earnings { get; private set; }

        public WalletStub(long balance) =>
            Balance = balance;

        public bool ServerTrySpend(long amount) =>
            throw new NotSupportedException();

        public void ServerEarn(long amount) {
            Balance += amount;
            Earnings++;
        }
    }
}
