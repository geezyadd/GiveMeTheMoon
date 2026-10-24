using Features.MvpModule;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;

namespace Features.ShopModule.Scripts.UI {
    // The crew balance; a rise (income, such as a popped target balloon) flashes it.
    public sealed class BalancePresenter : PresenterBehaviour<BalanceViewBase> {
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly WalletConfiguration _walletConfiguration;

        private bool _isBalanceShown;
        private long _shownBalance;

        public BalancePresenter(IReadOnlyWalletModel walletModel, WalletConfiguration walletConfiguration) {
            _walletModel = walletModel;
            _walletConfiguration = walletConfiguration;
        }

        protected override void OnViewSet() {
            _walletModel.OnBalanceChanged += OnBalanceChanged;
            OnBalanceChanged();
        }

        protected override void OnDisposed() =>
            _walletModel.OnBalanceChanged -= OnBalanceChanged;

        // The first value a player sees (a join included) is no income.
        private void OnBalanceChanged() {
            long balance = _walletModel.Balance;
            if (_isBalanceShown && balance > _shownBalance)
                View.PlayIncomeFlash(_walletConfiguration.IncomeFlashSeconds, _walletConfiguration.IncomeFlashColor, _walletConfiguration.IncomeFlashScale);

            _isBalanceShown = true;
            _shownBalance = balance;
            View.SetBalance(balance);
        }
    }
}
