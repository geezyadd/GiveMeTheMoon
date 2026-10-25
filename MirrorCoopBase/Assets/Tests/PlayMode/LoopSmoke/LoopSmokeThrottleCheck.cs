#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.CameraModule.Scripts.Models;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The throttle lever as a player uses it: in run 2 flight 4 the host stands behind the handle, holds interact on
    // it and pushes the mouse forward until the ship takes off; in that cruise the debug window's "Throttle = N" puts it
    // at 0 (the ship eases to a stop) and at 1 (it flies again) with the player riding on the deck; after the landing
    // the handle is back at Stop and free.
    public sealed class LoopSmokeThrottleCheck {
        private const int RUN = 2;
        private const int FLIGHT = 4;
        private const float FULL = 1f;
        private const float FULL_TOLERANCE = 0.02f;
        private const float STOP_TIMEOUT_SECONDS = 12f;
        private const float SPEED_SAMPLE_SECONDS = 0.5f;
        private const float STOPPED_SPEED = 0.2f;
        private const float MOVING_SPEED = 5f;

        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;

        public LoopSmokeThrottleCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
        }

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static IReadOnlyShipThrottleModel Throttle => SessionQueries.Resolve<IReadOnlyShipThrottleModel>();

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, RUN, FLIGHT, 10)]
        public void LaunchNextFlightByTheHandle() =>
            _flight.LaunchNextFlightBy(PushHandleToLaunchCoroutine);

        [LoopSmokeStep(LoopSmokePhase.InCruise, RUN, FLIGHT, 10)]
        public IEnumerator StopAndGoInCruiseCoroutine() {
            IShipThrottleDebug throttleDebug = SessionQueries.Resolve<IShipThrottleDebug>();
            throttleDebug.DebugSetThrottle(0f);
            yield return WaitForSpeedCoroutine(speed => speed < STOPPED_SPEED, "the ship to stop at throttle 0");
            AssertRidingOnDeck("stopped at throttle 0");

            throttleDebug.DebugSetThrottle(FULL);
            yield return WaitForSpeedCoroutine(speed => speed > MOVING_SPEED, "the ship to fly again at throttle 1");
            AssertRidingOnDeck("flying again at throttle 1");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, RUN, FLIGHT, 10)]
        public void AssertHandleBackAtStop() {
            Assert.AreEqual(0f, Throttle.Position, "Throttle: the handle is not back at Stop after the landing.");
            Assert.IsTrue(string.IsNullOrEmpty(Throttle.HolderKey), "Throttle: someone still holds the handle after the landing.");
        }

        private IEnumerator PushHandleToLaunchCoroutine(int loop) {
            ShipThrottleHandling handling = new ShipThrottleHandling(_session.Ship);
            yield return handling.PushHandleCoroutine(IsTakenOffAtFull);
            Assert.IsNull(handling.Failure, $"Loop {loop}: pushing the throttle handle failed: {handling.Failure}.");
            Assert.IsTrue(handling.WasViewLockedWhileHeld, $"Loop {loop}: the view was not locked while the handle was held.");
            Assert.AreEqual(FULL, handling.ReleasedPosition, FULL_TOLERANCE, $"Loop {loop}: the handle did not stay at Full after release.");
            Assert.IsFalse(SessionQueries.Resolve<CameraLookLockModel>().IsLocked, $"Loop {loop}: the view is still locked after release.");
            Assert.AreNotEqual(ShipRunPhase.Build, _session.Model.Phase, $"Loop {loop}: the pushed handle did not take off.");
        }

        private bool IsTakenOffAtFull() =>
            _session.Model.Phase != ShipRunPhase.Build && Throttle.Position >= FULL - FULL_TOLERANCE;

        private IEnumerator WaitForSpeedCoroutine(Func<float, bool> isReached, string target) {
            float deadline = Time.realtimeSinceStartup + STOP_TIMEOUT_SECONDS;
            while (true) {
                Vector3 start = _session.Ship.transform.position;
                yield return _speedSample;
                Vector3 moved = _session.Ship.transform.position - start;
                moved.y = 0f;
                float speed = moved.magnitude / SPEED_SAMPLE_SECONDS;
                if (isReached(speed))
                    yield break;

                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Throttle: timed out waiting for {target}; the ship moves at {speed:0.00} m/s.");
            }
        }

        private void AssertRidingOnDeck(string when) {
            Assert.IsTrue(Player.GetComponent<ShipRider>().IsRiding, $"Throttle: the player does not ride the ship {when}.");
            Assert.IsTrue(SessionQueries.IsOnDeck(_session.Ship, Player.transform.position), $"Throttle: the player is not on the deck {when}.");
        }
    }
}
#endif
