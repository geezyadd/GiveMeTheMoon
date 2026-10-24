namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipRockImpactFeelSource {
        public ShipRockImpactFeel GetFeel(ShipRockImpactKind kind);
    }
}
