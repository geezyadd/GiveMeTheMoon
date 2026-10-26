#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Features.ShopModule.Scripts.Data {
    public sealed class ShopDebugMoneyRequestEventClass {
        public event Action OnMoneyRequested;

        public void InvokeMoneyRequested() =>
            OnMoneyRequested?.Invoke();
    }
}
#endif
