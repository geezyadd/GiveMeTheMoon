#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.GrabModule.Scripts;

namespace Features.ShipModule.Scripts {
    internal interface IShipDeckCargoDebug {
        public void DebugAttach(Grabbable grabbable);
    }
}
#endif
