using Features.MvpModule;
using Features.ShopModule.Scripts.Generated;

namespace Features.ShopModule.Scripts.UI {
    public sealed class BalancePresenter : PresenterBehaviour<BalanceViewBase> {
        private readonly IReadOnlyWalletModel _walletModel;

        public BalancePresenter(IReadOnlyWalletModel walletModel) =>
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
