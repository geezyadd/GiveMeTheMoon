namespace Features.ShipModule.Scripts {
    public interface IShipFloorReferenceProvider {
        bool TryGetWalkableFloorY(out bool isFlying, out float floorY);
    }
}
