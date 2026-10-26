#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Debug only: the debug window's "+1000" action asks the server to add money to the crew wallet; it works from a
    // client too, the server adds the amount from WalletConfiguration.
    public sealed class ShopDebugMoneySystem : IInitializable, IDisposable {
        private const string GRANT_LABEL_FORMAT = "+{0}";
        private const string GRANT_DESCRIPTION = "Adds money to the crew wallet through the server; every crewmate sees the new balance.";
        private const string NOT_IN_SESSION_REASON = "Only in a running session.";
        private const string NO_WALLET_REASON = "The crew wallet is not ready yet.";

        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly WalletConfiguration _walletConfiguration;
        private readonly ShopDebugMoneyRequestEventClass _shopDebugMoneyRequestEventClass;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public ShopDebugMoneySystem(
            IGameFlowStateMachineService gameFlowStateMachineService,
            IReadOnlyWalletModel walletModel,
            WalletConfiguration walletConfiguration,
            ShopDebugMoneyRequestEventClass shopDebugMoneyRequestEventClass,
            IDebugActionRegistry debugActionRegistry) {
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _walletModel = walletModel;
            _walletConfiguration = walletConfiguration;
            _shopDebugMoneyRequestEventClass = shopDebugMoneyRequestEventClass;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(
                DebugSections.SHOP,
                string.Format(GRANT_LABEL_FORMAT, _walletConfiguration.DebugGrantAmount),
                GRANT_DESCRIPTION,
                ReadUnavailableReason,
                _shopDebugMoneyRequestEventClass.InvokeMoneyRequested));

        public void Dispose() =>
            _registration.Dispose();

        private string ReadUnavailableReason() {
            bool isInSession = _gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState) && _gameFlowStateMachineService.IsTransitioning == false;
            if (isInSession == false)
                return NOT_IN_SESSION_REASON;

            return _walletModel.IsAvailable ? null : NO_WALLET_REASON;
        }
    }
}
#endif
