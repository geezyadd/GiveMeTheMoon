#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts {
    public interface IShipWorldShiftDebug {
        public int WorldShiftCount { get; }
        public void DebugForceRecenter();
    }
}
#endif
