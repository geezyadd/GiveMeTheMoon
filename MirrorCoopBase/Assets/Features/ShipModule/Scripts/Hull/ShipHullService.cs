using Features.GameCoreModule.Contracts;

namespace Features.ShipModule.Scripts.Hull {
    // Server: the one way the run's hull loses or gets back HP. It knows the hit, not what made it.
    public sealed class ShipHullService : IShipHull, IShipHullRepair {
        private readonly IShipHullStatSource _shipHullStatSource;
        private readonly IShipHullDamageRule _shipHullDamageRule;
        private readonly IShipHullEventPublisher _shipHullEventPublisher;
        private readonly ShipHullConfiguration _shipHullConfiguration;

        public ShipHullService(
            IShipHullStatSource shipHullStatSource,
            IShipHullDamageRule shipHullDamageRule,
            IShipHullEventPublisher shipHullEventPublisher,
            ShipHullConfiguration shipHullConfiguration) {
            _shipHullStatSource = shipHullStatSource;
            _shipHullDamageRule = shipHullDamageRule;
            _shipHullEventPublisher = shipHullEventPublisher;
            _shipHullConfiguration = shipHullConfiguration;
        }

        // A destroyed hull takes no more hits until it is repaired, so OnDestroyed fires once per loss.
        public void ServerApplyDamage(ShipDamage damage) {
            if (_shipHullStatSource.TryGetHull(out IShipHullStat hull) == false || hull.Current <= 0f)
                return;

            hull.ServerSubtract(_shipHullDamageRule.CalculateDealtDamage(damage.Amount, hull.Armor));
            _shipHullEventPublisher.PublishDamaged(damage, hull.Current);
            if (hull.Current <= 0f)
                _shipHullEventPublisher.PublishDestroyed();
        }

        public void ServerRepairAfterLanding() {
            if (_shipHullConfiguration.RepairOnLanding == false)
                return;

            if (_shipHullStatSource.TryGetHull(out IShipHullStat hull))
                hull.ServerRepair();
        }
    }
}
