using System;

namespace Features.ShopModule.Scripts.Data {
    public sealed class WalletModel : IWalletModel {
        public long Balance { get; private set; }
        public bool IsAvailable { get; private set; }

        public event Action OnBalanceChanged;

        public void Connect(long balance) {
            IsAvailable = true;
            Balance = balance;
            OnBalanceChanged?.Invoke();
        }

        public void UpdateBalance(long balance) {
            if (Balance == balance)
                return;

            Balance = balance;
            OnBalanceChanged?.Invoke();
        }

        public void Disconnect() {
            IsAvailable = false;
            Balance = 0;
            OnBalanceChanged?.Invoke();
        }
    }
}
