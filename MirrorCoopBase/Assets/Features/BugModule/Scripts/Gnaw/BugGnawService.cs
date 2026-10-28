using Features.BugModule.Scripts.Configuration;
using Features.GameCoreModule.Contracts;
using Features.ShipDamageModule.Scripts.Damage;
using UnityEngine;

namespace Features.BugModule.Scripts.Gnaw {
    // A bite goes through the ship's own damage rules: the hull loses the bite's HP (cause Gnaw, the bug as the
    // source), and the bite is a weak impact for ShipDamageModule, whose rules may break the module beside it or open
    // a breach at the nearest hull point.
    public sealed class BugGnawService : IBugGnawService {
        private readonly IShipHull _shipHull;
        private readonly IShipImpactDamageService _shipImpactDamageService;
        private readonly BugConfiguration _bugConfiguration;

        public BugGnawService(IShipHull shipHull, IShipImpactDamageService shipImpactDamageService, BugConfiguration bugConfiguration) {
            _shipHull = shipHull;
            _shipImpactDamageService = shipImpactDamageService;
            _bugConfiguration = bugConfiguration;
        }

        public void ServerBite(int bugId, Vector3 worldPoint) {
            if (_bugConfiguration.BiteHull > 0f)
                _shipHull.ServerApplyDamage(new ShipDamage(_bugConfiguration.BiteHull, ShipDamageCause.Gnaw, worldPoint, bugId));

            _shipImpactDamageService.ServerApplyImpact(worldPoint, _bugConfiguration.BiteStrength);
        }
    }
}
