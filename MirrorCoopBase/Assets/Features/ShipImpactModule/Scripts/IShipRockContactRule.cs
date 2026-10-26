using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipRockContactRule {
        // The radius around a rock's middle the hull must reach to touch it, from its data radius and hit kind.
        public float CalculateContactRadius(ShipRockImpactKind kind, float dataRadius);
    }
}
