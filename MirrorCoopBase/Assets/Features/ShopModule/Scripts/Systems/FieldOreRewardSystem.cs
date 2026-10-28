using System;
using Features.FlightFieldModule.Contracts;
using Features.ShopModule.Scripts.Core;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Server: a piece of ore that reached the ship pays its price into the crew wallet. The event fires once per
    // piece, so the crew is paid once; what ore gives lives here, not in the ore.
    public sealed class FieldOreRewardSystem : IInitializable, IDisposable {
        private readonly IFieldOreEvents _fieldOreEvents;
        private readonly ICrewWalletBindingModel _crewWalletBindingModel;

        public FieldOreRewardSystem(IFieldOreEvents fieldOreEvents, ICrewWalletBindingModel crewWalletBindingModel) {
            _fieldOreEvents = fieldOreEvents;
            _crewWalletBindingModel = crewWalletBindingModel;
        }

        public void Initialize() =>
            _fieldOreEvents.OreCollected += OnOreCollected;

        public void Dispose() =>
            _fieldOreEvents.OreCollected -= OnOreCollected;

        // Ore reaches only a flying ship in the game scene, where the wallet is bound.
        private void OnOreCollected(FieldOreCollected collected) {
            if (!_crewWalletBindingModel.IsBound)
                throw new InvalidOperationException($"{collected.Id} reached the ship with no crew wallet bound on the server.");

            _crewWalletBindingModel.Wallet.ServerEarn(collected.Price);
        }
    }
}
