using System;

namespace Features.ShopModule.Scripts.Data {
    public sealed class ShopPurchaseRequestEventClass {
        public event Action<int> OnPurchaseRequested;

        public void InvokePurchaseRequested(int entryIndex) =>
            OnPurchaseRequested?.Invoke(entryIndex);
    }
}
