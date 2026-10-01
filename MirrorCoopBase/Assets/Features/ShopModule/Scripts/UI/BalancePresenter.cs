using Features.MvpModule;
using Features.ShopModule.Scripts.Data;

namespace Features.ShopModule.Scripts.UI {
    public sealed class BalancePresenter : PresenterBehaviour<BalanceViewBase> {
        private readonly IWalletModel _walletModel;

        public BalancePresenter(IWalletModel walletModel) =>
            _walletModel = walletModel;

        protected override void OnViewSet() {
            _walletModel.OnBalanceChanged += OnBalanceChanged;
            OnBalanceChanged();
        }

        protected override void OnDisposed() =>
            _walletModel.OnBalanceChanged -= OnBalanceChanged;

        private void OnBalanceChanged() =>
            View.SetBalance(_walletModel.Balance);
    }
}
