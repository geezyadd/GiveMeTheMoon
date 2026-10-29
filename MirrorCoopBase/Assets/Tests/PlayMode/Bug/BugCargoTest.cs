#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.BugModule.Scripts.Brain;
using Features.FieldAgentModule.Scripts.Cargo;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Bug {
    // A stand-in cargo lies on the deck in a slow cruise when the host sends one bug from the debug window: the bug
    // goes for the cargo instead of gnawing, grabs it and drags it away from the ship; once it is well away the debug
    // window's "Remove all field agents" takes its agent out of the field, and the vanished carrier lets the cargo go.
    // The carrier is slower than the ship, so the test does not wait for its whole way out (400 m, about two minutes:
    // the wait for it ran into its deadline once).
    public sealed class BugCargoTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const float CRUISE_THROTTLE = 0.15f;
        // One bug, so no other bug gnaws the ship down while the test watches the carrier fly off.
        private const int LONE_BUG = 1;
        private const float GRAB_TIMEOUT_SECONDS = 120f;
        private const float CARRY_AWAY_TIMEOUT_SECONDS = 90f;
        private const float WELL_AWAY_METERS = 60f;
        private const float CARGO_ON_CARRIER_METERS = 2f;
        private const float CARRY_SCREENSHOT_SECONDS = 1.5f;

        private static readonly Vector3 _cargoOnDeck = new(0f, 0.4f, -3f);

        private FieldAgentTestFlight _flight;
        private BugTestSteps _steps;
        private BugTestCargo _cargo;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new BugTestSteps(_flight);
        }

        [TearDown]
        public void TearDown() {
            if (_cargo != null) {
                SessionQueries.Resolve<IFieldAgentCargoTargetRegistry>().Remove(_cargo);
                _cargo.Dispose();
            }

            _flight.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenACargoLiesOnBoard_AndBugsCome_ThenOneGrabsItAndCarriesItAway() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            _flight.SetThrottle(CRUISE_THROTTLE);
            _cargo = new BugTestCargo(_flight.Ship.transform, _cargoOnDeck);
            SessionQueries.Resolve<IFieldAgentCargoTargetRegistry>().Add(_cargo);
            yield return _steps.SpawnGroupCoroutine(LONE_BUG);

            int carrier = -1;
            yield return _steps.WaitForStageCoroutine(BugStage.Carry, GRAB_TIMEOUT_SECONDS, id => carrier = id);
            Assert.IsTrue(_cargo.IsTaken, "The bug carries a cargo it never took.");
            Assert.IsTrue(_steps.Bugs.TryGet(carrier, out BugAgent bug) && ReferenceEquals(bug.Cargo, _cargo), "The carrying bug does not hold the cargo on board.");
            // Another bug that went for the cargo sees it taken at its own next step.
            yield return null;
            Assert.IsFalse(_steps.TryFindInStage(BugStage.SeekCargo, out int second), $"Bug {second} still goes for the cargo another bug took.");

            yield return new WaitForSeconds(CARRY_SCREENSHOT_SECONDS);
            Assert.IsTrue(_flight.Control.TryGetWorldPosition(carrier, out Vector3 carrierPosition));
            Assert.Less(Vector3.Distance(_cargo.CrateWorldPosition, carrierPosition), CARGO_ON_CARRIER_METERS, "The cargo does not follow its carrier.");
            yield return BugTestSteps.AimAtShownBugCoroutine(carrier, _ => { });
            yield return BugTestSteps.CaptureCoroutine("bug-carrying-cargo.png");

            yield return WaitForCarrierAwayCoroutine(carrier);
            Assert.IsFalse(_cargo.IsReleased, "The carrier let the cargo go on its way out.");
            yield return _flight.ClickDebugActionCoroutine("Remove all field agents");
            yield return null;
            Assert.IsTrue(_cargo.IsReleased, "The carrier's agent was removed without letting the cargo go.");
            Assert.IsFalse(_steps.Bugs.TryGet(carrier, out BugAgent _), $"Carrier {carrier} is still in the bug model after its agent was removed.");
            Debug.Log($"{BugTestSteps.LOG_PREFIX}bug {carrier} carried the cargo away, let it go {Vector3.Distance(_cargo.CrateWorldPosition, _flight.Ship.transform.position):0} m from the ship");

            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator WaitForCarrierAwayCoroutine(int carrier) {
            float deadline = Time.time + CARRY_AWAY_TIMEOUT_SECONDS;
            while (IsCarrierNear(carrier)) {
                Assert.IsTrue(_steps.Bugs.TryGet(carrier, out BugAgent _), $"Carrier {carrier} was gone before it got {WELL_AWAY_METERS} m away.");
                Assert.IsTrue(Time.time <= deadline, $"Bug {carrier} did not carry the cargo {WELL_AWAY_METERS} m away in {CARRY_AWAY_TIMEOUT_SECONDS} s.");
                yield return null;
            }
        }

        private bool IsCarrierNear(int carrier) =>
            _flight.Control.TryGetWorldPosition(carrier, out Vector3 position)
            && Vector3.Distance(position, _flight.Ship.transform.position) < WELL_AWAY_METERS;
    }
}
#endif
