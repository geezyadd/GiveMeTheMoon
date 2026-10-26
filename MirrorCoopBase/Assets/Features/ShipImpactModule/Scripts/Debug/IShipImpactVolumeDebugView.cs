#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipImpactModule.Scripts.Debug {
    public interface IShipImpactVolumeDebugView {
        public bool IsShown { get; }
        public void SetShown(bool isShown);
    }
}
#endif
