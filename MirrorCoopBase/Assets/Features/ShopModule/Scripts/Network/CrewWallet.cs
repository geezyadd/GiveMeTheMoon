using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Systems;
using Mirror;
using Zenject;

namespace Features.ShopModule.Scripts.Network {
    public sealed class CrewWallet : NetworkBehaviour {
        private WalletConfiguration _walletConfiguration;
        private WalletModel _walletModel;
        private ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private IShopPurchaseSystem _shopPurchaseSystem;

        [SyncVar(hook = nameof(OnBalanceSynced))]
        private long _balance;

        public long Balance => _balance;

        [Inject]
        private void InjectDependencies(
            WalletConfiguration walletConfiguration,
            WalletModel walletModel,
            ShopPurchaseRequestEventClass shopPurchaseRequestEventClass,
            IShopPurchaseSystem shopPurchaseSystem) {
            _walletConfiguration = walletConfiguration;
            _walletModel = walletModel;
            _shopPurchaseRequestEventClass = shopPurchaseRequestEventClass;
            _shopPurchaseSystem = shopPurchaseSystem;
        }

        public override void OnStartServer() =>
            ServerSetBalance(_walletConfiguration.StartingBalance);

        public override void OnStartClient() {
            _walletModel.Connect(_balance);
            _shopPurchaseRequestEventClass.OnPurchaseRequested += OnPurchaseRequested;
        }

        public override void OnStopClient() {
            _shopPurchaseRequestEventClass.OnPurchaseRequested -= OnPurchaseRequested;
            _walletModel.Disconnect();
        }

        [Server]
        public void ServerSetBalance(long balance) =>
            _balance = balance;

        [Server]
        public bool ServerTrySpend(long amount) {
            if (amount < 0 || _balance < amount)
                return false;

            _balance -= amount;
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

        private void OnBalanceSynced(long previous, long current) =>
            _walletModel.UpdateBalance(current);
    }
}
