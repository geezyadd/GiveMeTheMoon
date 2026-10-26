#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.ShipModule.Scripts;
using Tests.Shared;

namespace Tests.PlayMode.LoopSmoke {
    // The field over the five flights of run 2 (every flight's own field checks run in LoopSmokeFlight): a forced world
    // shift in a cruise keeps the field in place against the ship, and by the last station at least one field had a
    // networked placement to check.
    public sealed class LoopSmokeFieldRunCheck {
        private readonly LoopSmokeContext _context;

        public LoopSmokeFieldRunCheck(LoopSmokeContext context) =>
            _context = context;

        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 3, 10)]
        public IEnumerator ShiftWorldInCruiseCoroutine() =>
            _context.FieldCheck.AssertFieldFollowsWorldShiftCoroutine(_context.Session.Ship.transform, SessionQueries.Resolve<IShipWorldShiftDebug>());

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 5, 10)]
        public void AssertNetworkedPathWasChecked() =>
            _context.FieldCheck.AssertNetworkedPathWasChecked();
    }
}
#endif
