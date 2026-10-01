using System;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using Zenject;

namespace Features.ShopModule.Scripts.Network {
    public sealed class CrewWallet : WalletBridge, ICrewWallet {
        private WalletConfiguration _walletConfiguration;
        private ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private IShopPurchaseSystem _shopPurchaseSystem;

        public long Balance =>
            Model.Balance;

        [Inject]
        private void InjectDependencies(
            WalletConfiguration walletConfiguration,
            ShopPurchaseRequestEventClass shopPurchaseRequestEventClass,
            IShopPurchaseSystem shopPurchaseSystem) {
            _walletConfiguration = walletConfiguration;
            _shopPurchaseRequestEventClass = shopPurchaseRequestEventClass;
            _shopPurchaseSystem = shopPurchaseSystem;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            ServerSetBalance(_walletConfiguration.StartingBalance);
        }

        public override void OnStartClient() {
            base.OnStartClient();
            _shopPurchaseRequestEventClass.OnPurchaseRequested += OnPurchaseRequested;
        }

        public override void OnStopClient() {
            _shopPurchaseRequestEventClass.OnPurchaseRequested -= OnPurchaseRequested;
            base.OnStopClient();
        }

        [Server]
        public bool ServerTrySpend(long amount) {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, null);

            if (Balance < amount)
                return false;

            ServerSetBalance(Balance - amount);
            return true;
        }

        [Command(requiresAuthority = false)]
        private void CmdPurchase(int entryIndex, NetworkConnectionToClient sender = null) {
            if (sender.identity == null)
                return;

            _shopPurchaseSystem.ServerPurchase(this, entryIndex, sender.identity.transform);
        }

        private void OnPurchaseRequested(int entryIndex) =>
            CmdPurchase(entryIndex);
    }
}
