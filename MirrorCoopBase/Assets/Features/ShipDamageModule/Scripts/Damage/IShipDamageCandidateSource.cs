using System.Collections.Generic;
using Features.ShipModule.Scripts;

namespace Features.ShipDamageModule.Scripts.Damage {
    // Server: what on the ship can take damage now, for whatever picks a place to damage (a rock impact, a gnawing
    // bug). The lists are the caller's and are cleared first.
    public interface IShipDamageCandidateSource {
        // Every installed module, breakable only while it can break and works now.
        public void CollectModules(ShipBase ship, List<ShipDamageCandidate> modules);
        // Every hull damage point, damageable only while its breach is closed.
        public void CollectDamagePoints(List<ShipDamageCandidate> points);
    }
}
