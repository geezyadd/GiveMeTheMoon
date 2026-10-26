using System;
using Features.ShipModule.Scripts.Contracts;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Damage {
    // Server: every rock hit may break the module nearest to it and open a hull breach.
    public sealed class ShipImpactDamageSystem : IInitializable, IDisposable {
        private readonly IShipServerImpactEvents _shipServerImpactEvents;
        private readonly IShipImpactDamageService _shipImpactDamageService;

        public ShipImpactDamageSystem(IShipServerImpactEvents shipServerImpactEvents, IShipImpactDamageService shipImpactDamageService) {
            _shipServerImpactEvents = shipServerImpactEvents;
            _shipImpactDamageService = shipImpactDamageService;
        }

        public void Initialize() =>
            _shipServerImpactEvents.OnServerImpact += OnServerImpact;

        public void Dispose() =>
            _shipServerImpactEvents.OnServerImpact -= OnServerImpact;

        private void OnServerImpact(ShipRockImpact impact) =>
            _shipImpactDamageService.ServerApplyImpact(impact.Point, impact.Strength);
    }
}
