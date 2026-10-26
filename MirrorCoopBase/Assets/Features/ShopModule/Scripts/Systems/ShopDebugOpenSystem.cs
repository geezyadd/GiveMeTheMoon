#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Debug only: the debug window's "Open the shop here" action opens the shop wherever the player stands; in a
    // release build the shop opens only at a station's kiosk (ShopKioskInteractable).
    public sealed class ShopDebugOpenSystem : IInitializable, IDisposable {
        private const string OPEN_LABEL = "Open the shop here";
        private const string OPEN_DESCRIPTION = "Opens the shop window without walking to a station's kiosk; bought items still appear on the kiosk's delivery pad.";
        private const string NOT_IN_SESSION_REASON = "Only in a running session.";
        private const string NO_WALLET_REASON = "The crew wallet is not ready yet.";
        private const string DEAD_REASON = "Dead players cannot shop.";
        private const string ALREADY_OPEN_REASON = "The shop is already open.";

        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly IPlayerLifeQuery _playerLifeQuery;
        private readonly ShopModel _shopModel;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public ShopDebugOpenSystem(
            IGameFlowStateMachineService gameFlowStateMachineService,
            IReadOnlyWalletModel walletModel,
            IPlayerLifeQuery playerLifeQuery,
            ShopModel shopModel,
            IDebugActionRegistry debugActionRegistry) {
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _walletModel = walletModel;
            _playerLifeQuery = playerLifeQuery;
            _shopModel = shopModel;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHOP, OPEN_LABEL, OPEN_DESCRIPTION, ReadUnavailableReason, OpenShop));

        public void Dispose() =>
            _registration.Dispose();

        private string ReadUnavailableReason() {
            bool isInSession = _gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState) && _gameFlowStateMachineService.IsTransitioning == false;
            if (isInSession == false)
                return NOT_IN_SESSION_REASON;

            if (_walletModel.IsAvailable == false)
                return NO_WALLET_REASON;

            if (_playerLifeQuery.LocalState != PlayerLifeState.Alive)
                return DEAD_REASON;

            return _shopModel.IsOpen ? ALREADY_OPEN_REASON : null;
        }

        private void OpenShop() =>
            _shopModel.Open(null);
    }
}
#endif
