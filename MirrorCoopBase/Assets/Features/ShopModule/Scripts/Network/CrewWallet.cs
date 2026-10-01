using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Mirror;
using Zenject;

namespace Features.ShopModule.Scripts.Network {
    public sealed class CrewWallet : NetworkBehaviour {
        private WalletConfiguration _walletConfiguration;
        private WalletModel _walletModel;
        private ShopPurchaseRequestEventClass _shopPurchaseRequestEventClass;
        private IShopPurchaseService _shopPurchaseService;
        private IShopItemSpawnService _shopItemSpawnService;

        [SyncVar(hook = nameof(OnBalanceSynced))]
        private long _balance;

        [Inject]
        private void InjectDependencies(
            WalletConfiguration walletConfiguration,
            WalletModel walletModel,
            ShopPurchaseRequestEventClass shopPurchaseRequestEventClass,
            IShopPurchaseService shopPurchaseService,
            IShopItemSpawnService shopItemSpawnService) {
            _walletConfiguration = walletConfiguration;
            _walletModel = walletModel;
            _shopPurchaseRequestEventClass = shopPurchaseRequestEventClass;
            _shopPurchaseService = shopPurchaseService;
            _shopItemSpawnService = shopItemSpawnService;
        }

        public override void OnStartServer() =>
            _balance = _walletConfiguration.StartingBalance;

        public override void OnStartClient() {
            _walletModel.Connect(_balance);
            _shopPurchaseRequestEventClass.OnPurchaseRequested += OnPurchaseRequested;
        }

        public override void OnStopClient() {
            _shopPurchaseRequestEventClass.OnPurchaseRequested -= OnPurchaseRequested;
            _walletModel.Disconnect();
        }

        [Command(requiresAuthority = false)]
        private void CmdPurchase(int entryIndex, NetworkConnectionToClient sender = null) {
            if (sender == null || sender.identity == null)
                return;

            ShopPurchaseResult result = _shopPurchaseService.Evaluate(_balance, entryIndex);
            if (result.Status != ShopPurchaseStatus.Success)
                return;

            _balance -= result.Entry.Price;
            _shopItemSpawnService.SpawnNear(result.Entry.Item, sender.identity.transform);
        }

        private void OnPurchaseRequested(int entryIndex) =>
            CmdPurchase(entryIndex);

        private void OnBalanceSynced(long previous, long current) =>
            _walletModel.UpdateBalance(current);
    }
}
