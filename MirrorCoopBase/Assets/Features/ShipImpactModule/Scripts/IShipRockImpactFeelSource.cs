using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipRockImpactFeelSource {
        public ShipRockImpactFeel GetFeel(ShipRockImpactKind kind);
    }
}
