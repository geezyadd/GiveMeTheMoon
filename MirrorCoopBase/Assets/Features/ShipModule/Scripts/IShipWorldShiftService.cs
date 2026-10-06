namespace Features.ShipModule.Scripts {
    public interface IShipWorldShiftService {
        int WorldShiftCount { get; }
        void RecenterIfFar();
        void DebugForceRecenter();
    }
}
