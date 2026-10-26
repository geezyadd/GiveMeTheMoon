using System;
using Features.FlightFieldModule.Contracts;
using Features.ShopModule.Scripts.Core;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Server: a popped target balloon pays its reward into the crew wallet. The pop event fires once per balloon, so
    // the crew is paid once; what a pop gives lives here, not in the balloon (it may become loot later).
    public sealed class FieldTargetRewardSystem : IInitializable, IDisposable {
        private readonly IFieldTargetEvents _fieldTargetEvents;
        private readonly ICrewWalletBindingModel _crewWalletBindingModel;

        public FieldTargetRewardSystem(IFieldTargetEvents fieldTargetEvents, ICrewWalletBindingModel crewWalletBindingModel) {
            _fieldTargetEvents = fieldTargetEvents;
            _crewWalletBindingModel = crewWalletBindingModel;
        }

        public void Initialize() =>
            _fieldTargetEvents.TargetPopped += OnTargetPopped;

        public void Dispose() =>
            _fieldTargetEvents.TargetPopped -= OnTargetPopped;

        // A pop needs a flying ship in the game scene, where the wallet is bound.
        private void OnTargetPopped(FieldTargetPopped popped) {
            if (!_crewWalletBindingModel.IsBound)
                throw new InvalidOperationException($"Target balloon {popped.Id} popped with no crew wallet bound on the server.");

            _crewWalletBindingModel.Wallet.ServerEarn(popped.Reward);
        }
    }
}
