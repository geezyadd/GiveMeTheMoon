using System;
using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.Data {
    public sealed class ShopReviveRequestEventClass {
        public event Action<PlayerKey> OnReviveRequested;

        public void InvokeReviveRequested(PlayerKey target) =>
            OnReviveRequested?.Invoke(target);
    }
}
