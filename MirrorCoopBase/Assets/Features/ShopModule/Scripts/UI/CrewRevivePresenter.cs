using System.Collections.Generic;
using Features.MvpModule;
using Features.NetworkModelModule.Scripts;
using Features.PlayerProfileModule.Data.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;

namespace Features.ShopModule.Scripts.UI {
    // The window is open only while its player may use the kiosk (ShopKioskRangeSystem closes it otherwise, also on
    // takeoff), so a row only checks the money; the server checks everything again.
    public sealed class CrewRevivePresenter : PresenterBehaviour<CrewReviveViewBase> {
        private const string PRICE_FORMAT = "{0:N0}";
        private const int SHORT_KEY_LENGTH = 6;

        private readonly DeadCrewModel _deadCrewModel;
        private readonly IReviveRule _reviveRule;
        private readonly ReviveConfiguration _reviveConfiguration;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly IReadOnlyPlayerProfileRegistry _playerProfileRegistry;
        private readonly ShopReviveRequestEventClass _shopReviveRequestEventClass;

        public CrewRevivePresenter(
            DeadCrewModel deadCrewModel,
            IReviveRule reviveRule,
            ReviveConfiguration reviveConfiguration,
            IReadOnlyWalletModel walletModel,
            IReadOnlyPlayerProfileRegistry playerProfileRegistry,
            ShopReviveRequestEventClass shopReviveRequestEventClass) {
            _deadCrewModel = deadCrewModel;
            _reviveRule = reviveRule;
            _reviveConfiguration = reviveConfiguration;
            _walletModel = walletModel;
            _playerProfileRegistry = playerProfileRegistry;
            _shopReviveRequestEventClass = shopReviveRequestEventClass;
        }

        protected override void OnViewSet() {
            View.OnReviveClicked += OnReviveClicked;
            _walletModel.OnBalanceChanged += OnCrewChanged;
            _deadCrewModel.OnEntriesChanged += OnCrewChanged;
            OnCrewChanged();
        }

        protected override void OnDisposed() {
            View.OnReviveClicked -= OnReviveClicked;
            _walletModel.OnBalanceChanged -= OnCrewChanged;
            _deadCrewModel.OnEntriesChanged -= OnCrewChanged;
        }

        private void OnCrewChanged() {
            IReadOnlyList<PlayerKey> deadCrew = _deadCrewModel.Entries;
            if (deadCrew.Count == 0) {
                HideView();
                return;
            }

            View.SetCrew(CreateDisplays(deadCrew));
            ShowView();
        }

        private IReadOnlyList<CrewReviveDisplay> CreateDisplays(IReadOnlyList<PlayerKey> deadCrew) {
            long price = _reviveConfiguration.RevivePrice;
            string priceText = string.Format(PRICE_FORMAT, price);
            bool canAfford = _reviveRule.CanAfford(_walletModel.Balance, price);
            List<CrewReviveDisplay> displays = new(deadCrew.Count);
            foreach (PlayerKey player in deadCrew)
                displays.Add(new CrewReviveDisplay(player, ReadName(player), priceText, canAfford));

            return displays;
        }

        // A player whose profile has not arrived yet is shown by the end of their key.
        private string ReadName(PlayerKey player) {
            if (_playerProfileRegistry.TryGet(player, out IReadOnlyPlayerProfileModel profile)
                && string.IsNullOrEmpty(profile.DisplayName) == false)
                return profile.DisplayName;

            string id = player.Id;
            return id.Length <= SHORT_KEY_LENGTH ? id : id.Substring(id.Length - SHORT_KEY_LENGTH);
        }

        private void OnReviveClicked(PlayerKey player) =>
            _shopReviveRequestEventClass.InvokeReviveRequested(player);
    }
}
