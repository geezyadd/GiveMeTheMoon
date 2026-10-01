using System;
using System.Collections.Generic;
using Features.MvpModule;

namespace Features.ShopModule.Scripts.UI {
    public abstract class ShopViewBase : ViewBehaviour {
        public event Action<int> OnBuyClicked;
        public event Action OnCloseClicked;

        public abstract void SetItems(IReadOnlyList<ShopItemDisplay> items);
        public abstract void SetItemAffordable(int index, bool isAffordable);

        protected void InvokeBuyClicked(int index) =>
            OnBuyClicked?.Invoke(index);

        protected void InvokeCloseClicked() =>
            OnCloseClicked?.Invoke();
    }
}
