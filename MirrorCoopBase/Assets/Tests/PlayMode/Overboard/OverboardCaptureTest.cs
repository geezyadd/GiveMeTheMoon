#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.DebugModule.Contracts;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Overboard {
    // The host is thrown overboard in a cruise and drifts; the debug window's "Bring the ship to the drifting player"
    // puts the deck under it: the ship takes it back and it rides the deck again.
    public sealed class OverboardCaptureTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const string BRING_LABEL = "Bring the ship to the drifting player";
        private const float BACK_ON_DECK_TIMEOUT_SECONDS = 5f;
        private const float RIDE_AGAIN_SECONDS = 2f;

        private FieldAgentTestFlight _flight;
        private OverboardTestSteps _steps;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new OverboardTestSteps(_flight);
        }

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostDriftsOverboard_AndShipComesToIt_ThenItRidesTheDeckAgain() {
            yield return _steps.ThrowOverboardAndWatchDriftCoroutine();

            yield return OverboardTestSteps.ClickCoroutine(DebugSections.SHIP, BRING_LABEL);
            yield return _flight.Session.WaitForCoroutine(IsRidingAgain, BACK_ON_DECK_TIMEOUT_SECONDS, "the drifting host back on the deck and riding");
            yield return new WaitForSeconds(RIDE_AGAIN_SECONDS);
            Assert.IsTrue(IsRidingAgain(), "The host taken back by the ship does not ride the deck any more.");
            Assert.IsTrue(SessionQueries.IsOnDeck(_flight.Ship, OverboardTestSteps.LocalOverboard.Position), "The host taken back by the ship is not on its deck.");
            Assert.IsFalse(OverboardTestSteps.LocalDrift.IsDrifting, "The host on the deck still drifts.");
            yield return OverboardTestSteps.CaptureCoroutine("overboard-3-back-on-deck.png");
            yield return _flight.LandAndLeaveCoroutine();
        }

        private bool IsRidingAgain() =>
            _flight.Ship.Riders.IsLocalPlayerRiding() && OverboardTestSteps.LocalOverboard.IsOverboard == false;
    }
}
#endif
