using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    public interface IShipImpactDamageService {
        // Server: something hit the hull at the point with the strength (0..1), a rock or a gnawing bug's bite: it
        // may break a module and open a breach.
        public void ServerApplyImpact(Vector3 point, float strength);
    }
}
