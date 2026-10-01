using System;

namespace Features.ShopModule.Scripts.Data {
    public interface IWalletModel {
        public long Balance { get; }
        public bool IsAvailable { get; }

        public event Action OnBalanceChanged;
    }
}
