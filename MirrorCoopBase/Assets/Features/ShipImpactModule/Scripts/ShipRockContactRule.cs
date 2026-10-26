using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    // A rock's data sphere holds its turned mesh with room to spare, so the hull touches it at a share of that sphere
    // per hit kind, near the mesh's visible surface.
    public sealed class ShipRockContactRule : IShipRockContactRule {
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        public ShipRockContactRule(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public float CalculateContactRadius(ShipRockImpactKind kind, float dataRadius) =>
            dataRadius * (kind == ShipRockImpactKind.Small
                ? _shipRockImpactConfiguration.SmallContactRadiusShare
                : _shipRockImpactConfiguration.LargeContactRadiusShare);
    }
}
