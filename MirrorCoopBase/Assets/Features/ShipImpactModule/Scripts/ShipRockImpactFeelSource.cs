using System;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public sealed class ShipRockImpactFeelSource : IShipRockImpactFeelSource {
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        public ShipRockImpactFeelSource(ShipRockImpactConfiguration shipRockImpactConfiguration) =>
            _shipRockImpactConfiguration = shipRockImpactConfiguration;

        public ShipRockImpactFeel GetFeel(ShipRockImpactKind kind) =>
            kind switch {
                ShipRockImpactKind.Small => _shipRockImpactConfiguration.SmallFeel,
                ShipRockImpactKind.Large => _shipRockImpactConfiguration.LargeFeel,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A rock impact has no feel for this kind."),
            };
    }
}
