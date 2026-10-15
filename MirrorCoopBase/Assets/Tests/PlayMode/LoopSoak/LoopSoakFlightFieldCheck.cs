#if UNITY_INCLUDE_TESTS
using System.Collections;

namespace Tests.PlayMode.LoopSoak {
    // The flight field objects of a loop: as many in cruise as the field model has placements, none after landing,
    // and a pool that does not grow from loop to loop. Placements grow with the loop (a longer field): reported only.
    public sealed class LoopSoakFlightFieldCheck {
        public const int UNKNOWN = -1;

        public int Placements { get; private set; } = UNKNOWN;
        public int PooledObjects { get; private set; } = UNKNOWN;

        // TODO(flight field in the world): the field is data only on this branch (Features.FlightFieldModule spawns
        // nothing yet). Once it is built in the world: read the placement count of this loop's field model and assert
        // the field objects in the scene equal it.
        public IEnumerator CheckInCruiseCoroutine(int loop) {
            yield break;
        }

        // TODO(flight field in the world): assert 0 field objects in the scene after landing, record the pool size and
        // fail when it grows past loop 1's by more than a tolerance.
        public void CheckAfterLanding(int loop) {
        }
    }
}
#endif
