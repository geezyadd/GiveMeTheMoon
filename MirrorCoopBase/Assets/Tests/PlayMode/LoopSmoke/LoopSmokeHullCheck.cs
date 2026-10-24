#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using Tests.Shared;

namespace Tests.PlayMode.LoopSmoke {
    // The hull through the debug entry the F7 key uses: a hit in cruise lowers the synced hull by the armor-reduced
    // damage, the landing repairs it.
    public sealed class LoopSmokeHullCheck {
        private const float HIT = 15f;
        private const float TOLERANCE = 0.01f;

        public IEnumerator HitInCruiseCoroutine() {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            Assert.Greater(stats.HullMax, 0f, "Hull: the synced hull max is 0 in cruise.");
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, "Hull: the run did not start with a full hull.");
            float expected = stats.Hull - SessionQueries.Resolve<IShipHullDamageRule>().CalculateDealtDamage(HIT, stats.Armor);

            SessionQueries.Resolve<IShipHullDebug>().DebugDamage(HIT);
            yield return null;

            Assert.Less(stats.Hull, stats.HullMax, "Hull: a debug hit in cruise did not lower the synced hull.");
            Assert.AreEqual(expected, stats.Hull, TOLERANCE, "Hull: the synced hull after the hit is not the armor-reduced damage.");
        }

        public void AssertRepairedAfterLanding(int loop) {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, $"Loop {loop}: the hull was not repaired on landing.");
        }
    }
}
#endif
