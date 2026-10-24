using System;
using Features.NetworkModelModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using Zenject;

namespace Features.ShopModule.Scripts.Network {
    public sealed class CrewWallet : WalletBridge, ICrewWallet {
        private IWalletStartingBalanceSource _walletStartingBalanceSource;
        private ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private IShopPurchaseSystem _shopPurchaseSystem;
        private ShopReviveRequestEventClass _shopReviveRequestEventClass;
        private IReviveShopSystem _reviveShopSystem;
        private ICrewWalletBindingModel _crewWalletBindingModel;

        public long Balance =>
            Model.Balance;

        [Inject]
        private void InjectDependencies(
            IWalletStartingBalanceSource walletStartingBalanceSource,
            ShopPurchaseRequestEventClass shopPurchaseRequestEventClass,
            IShopPurchaseSystem shopPurchaseSystem,
            ShopReviveRequestEventClass shopReviveRequestEventClass,
            IReviveShopSystem reviveShopSystem,
            ICrewWalletBindingModel crewWalletBindingModel) {
            _walletStartingBalanceSource = walletStartingBalanceSource;
            _shopPurchaseRequestEventClass = shopPurchaseRequestEventClass;
            _shopPurchaseSystem = shopPurchaseSystem;
            _shopReviveRequestEventClass = shopReviveRequestEventClass;
            _reviveShopSystem = reviveShopSystem;
            _crewWalletBindingModel = crewWalletBindingModel;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            ServerSetBalance(_walletStartingBalanceSource.StartingBalance);
            _crewWalletBindingModel.Bind(this);
        }

        public override void OnStopServer() {
            _crewWalletBindingModel.Clear(this);
            base.OnStopServer();
        }

        public override void OnStartClient() {
            base.OnStartClient();
            _shopPurchaseRequestEventClass.OnPurchaseRequested += OnPurchaseRequested;
            _shopReviveRequestEventClass.OnReviveRequested += OnReviveRequested;
        }

        public override void OnStopClient() {
            _shopPurchaseRequestEventClass.OnPurchaseRequested -= OnPurchaseRequested;
            _shopReviveRequestEventClass.OnReviveRequested -= OnReviveRequested;
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

        [Server]
        public void ServerEarn(long amount) {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, null);

            ServerSetBalance(Balance + amount);
        }

        [Command(requiresAuthority = false)]
        private void CmdPurchase(int entryIndex, NetworkConnectionToClient sender = null) {
            if (sender.identity == null)
                return;

            _shopPurchaseSystem.ServerPurchase(this, entryIndex, sender.identity.GetComponent<ShopCustomer>());
        }

        // PlayerKey is not a Mirror-serializable type: its id travels as a string.
        [Command(requiresAuthority = false)]
        private void CmdRevive(string targetKeyId, NetworkConnectionToClient sender = null) {
            if (sender.identity == null)
                return;

            _reviveShopSystem.ServerRevive(this, new PlayerKey(targetKeyId), sender.identity.GetComponent<ShopCustomer>());
        }

        private void OnPurchaseRequested(int entryIndex) =>
            CmdPurchase(entryIndex);

        private void OnReviveRequested(PlayerKey target) =>
            CmdRevive(target.Id);
    }
}
