#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    public interface IShipOverboardDebug {
        public void DebugThrowLocalPlayer();
        public void DebugBringShipToDrifter();
    }
}
#endif
