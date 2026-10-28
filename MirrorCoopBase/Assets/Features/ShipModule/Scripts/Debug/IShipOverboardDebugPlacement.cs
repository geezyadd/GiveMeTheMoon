#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    public interface IShipOverboardDebugPlacement {
        public void ThrowOverboard(ShipBase ship, ShipRider rider, Rigidbody body);
        public void BringShipTo(ShipBase ship, Vector3 drifterPosition);
    }
}
#endif
