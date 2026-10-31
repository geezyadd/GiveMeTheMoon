#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Globalization;
using Features.DebugModule.Contracts;
using Features.GrabModule.Scripts;
using Features.MvpModule;
using Features.ShipModule.Scripts.Debug;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Configurations;
using Features.SonarModule.Scripts.Network;
using Features.SonarModule.Scripts.UI;
using NUnit.Framework;
using Tests.PlayMode.PointOfInterest;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // A bug nest the way a crew finds and takes it, in the first cruise of run 1: the navigator's ping shows the
    // nearest point of interest on the scope (bright and pulsing, beyond the range too); the debug window puts the ship
    // just outside its zone and the cruise flies it in; the nest starts once, the throttle goes to Stop and the entry
    // cutscene plays until the host presses Space (it skips it for everyone); the nest's bugs are out; the ship rams the
    // reward container and the synced balance grows by the reward once. The bugs are removed and the ship goes back on
    // its course at full throttle.
    public sealed class LoopSmokePointOfInterestCheck {
        private const string CONTACT_PREFIX = "poi:";
        private const string REMOVE_AGENTS_LABEL = "Remove all field agents";
        private const float TIMEOUT_SECONDS = 4f;
        private const float ENTRY_THROTTLE = 0.6f;
        private const float FULL_THROTTLE = 1f;

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;
        private readonly PointOfInterestTestSteps _steps;

        public LoopSmokePointOfInterestCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
            _steps = new PointOfInterestTestSteps(_session, SetThrottle);
        }

        // Right after the sonar check of this cruise (order 10), while the host still stands at the console.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 15)]
        public IEnumerator PingPointOnSonarCoroutine() {
            yield return _steps.WaitForPointsCoroutine();
            SonarConsoleInteractable console = _session.Ship.GetComponentInChildren<SonarConsoleInteractable>();
            SonarScopeModel scope = SessionQueries.Resolve<SonarScopeModel>();
            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(console);
            yield return _session.WaitForCoroutine(() => scope.IsOpen, TIMEOUT_SECONDS, "Points of interest: the sonar console open for the host");

            Object.FindAnyObjectByType<SonarScopeView>().DebugPingButton.onClick.Invoke();
            string contactId = CONTACT_PREFIX + _steps.Points.Get(0).Id.ToString(CultureInfo.InvariantCulture);
            yield return _session.WaitForCoroutine(() => HasBlip(scope, contactId), TIMEOUT_SECONDS, $"Points of interest: the ping's echo of {contactId}");
            Assert.IsTrue(IsLegendListed(), "Points of interest: the sonar legend has no point of interest row.");
            yield return PointOfInterestTestSteps.CaptureCoroutine("sonar-point-of-interest.png");

            Object.FindAnyObjectByType<SonarScopeView>().DebugLeaveButton.onClick.Invoke();
            yield return _session.WaitForCoroutine(() => !scope.IsOpen && !IsSonarWindowOpen(), TIMEOUT_SECONDS, "Points of interest: the sonar console left");
        }

        // After the repair check of this cruise (order 50) and the rock near pass (45).
        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 60)]
        public IEnumerator VisitBugNestCoroutine() {
            Vector3 start = _session.Ship.transform.position;
            int pointId = 0;
            yield return _steps.MoveToNearestPointCoroutine(id => pointId = id);
            yield return _steps.EnterZoneCoroutine(pointId, ENTRY_THROTTLE);
            yield return _steps.WatchEntryCutsceneCoroutine(true, "loop-cutscene-skipped.png", null);
            _steps.AssertBugsCameOut();
            yield return _steps.RamContainerCoroutine(pointId);

            yield return _steps.ClickCoroutine(DebugSections.AGENTS, REMOVE_AGENTS_LABEL);
            SetThrottle(FULL_THROTTLE);
            yield return _flight.PutShipBackCoroutine(start);
        }

        private static void SetThrottle(float position) =>
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(position);

        private bool IsLegendListed() {
            foreach (SonarContactKind kind in SessionQueries.Resolve<SonarConfiguration>().Kinds) {
                if (kind == _steps.Configuration.SonarKind)
                    return true;
            }

            return false;
        }

        private static bool HasBlip(SonarScopeModel scope, string contactId) {
            foreach (SonarBlip blip in scope.Blips) {
                if (blip.ContactId == contactId)
                    return true;
            }

            return false;
        }

        private static bool IsSonarWindowOpen() =>
            SessionQueries.Resolve<IWindowsService>().GetWindow(typeof(SonarWindow)).WindowStatus != WindowStatus.Closed;
    }
}
#endif
