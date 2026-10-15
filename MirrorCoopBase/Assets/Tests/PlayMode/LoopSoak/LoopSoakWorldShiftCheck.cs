#if UNITY_INCLUDE_TESTS
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using UnityEngine;

namespace Tests.PlayMode.LoopSoak {
    // The floating origin over many loops: a landed ship never stays beyond the recenter distance (no drift to large
    // coordinates), and after a shift the player still stands on the deck.
    public sealed class LoopSoakWorldShiftCheck {
        private readonly IShipWorldShiftDebug _worldShift = LoopSmokeShopCheck.Resolve<IShipWorldShiftDebug>();
        private readonly float _recenterDistance;
        private int _checkedShifts;

        public int Count =>
            _worldShift.WorldShiftCount;

        public LoopSoakWorldShiftCheck(ShipRunConfig config) =>
            _recenterDistance = config.OriginRecenterDistance;

        public static float DistanceFromOrigin(ShipBase ship) {
            Vector3 position = ship.transform.position;
            return new Vector2(position.x, position.z).magnitude;
        }

        public void AssertAfterLanding(int loop, ShipBase ship, LoopSmokeFlight flight) {
            float distance = DistanceFromOrigin(ship);
            Assert.Less(distance, _recenterDistance, $"Loop {loop}: the landed ship is {distance:F0} m from the origin, beyond the recenter distance {_recenterDistance} m.");
            if (Count == _checkedShifts)
                return;

            _checkedShifts = Count;
            flight.AssertPlayerOnDeck(loop);
        }
    }
}
#endif
