#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal interface IShipFlightControlDebug {
        public void SetDebugSteer(bool active, float steer);
        public void DebugFace(Vector3 worldForward);
    }
}
#endif
