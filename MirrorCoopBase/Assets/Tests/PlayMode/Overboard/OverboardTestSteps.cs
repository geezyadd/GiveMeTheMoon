#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts.Overboard;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Overboard {
    // The steps the overboard tests share, on the field agent tests' host flight: a cruise, the debug window's
    // "Throw me overboard" clicked as a player does, the drift watched (the server put the host overboard, its height
    // barely changes, the ship flies on, the body tumbles) and the screenshots in the workspace's overboard folder.
    public sealed class OverboardTestSteps {
        public const string LOG_PREFIX = "[Overboard] ";

        private const string THROW_LABEL = "Throw me overboard";
        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/overboard";
        private const string TUMBLING_VISUAL = "RotatablePart/Visual";
        private const float CRUISE_THROTTLE = 0.6f;
        private const float SETTLE_SECONDS = 1f;
        private const float OVERBOARD_TIMEOUT_SECONDS = 3f;
        private const float DRIFT_WATCH_SECONDS = 4f;
        private const float MAX_HEIGHT_CHANGE_METERS = 1.5f;
        private const float MIN_SHIP_GAP_GROWTH_METERS = 5f;

        private readonly FieldAgentTestFlight _flight;

        public OverboardTestSteps(FieldAgentTestFlight flight) =>
            _flight = flight;

        public static ShipOverboardRider LocalOverboard =>
            SessionQueries.LocalPlayer.GetComponent<ShipOverboardRider>();

        public static CharacterDrift LocalDrift =>
            SessionQueries.LocalPlayer.GetComponent<CharacterDrift>();

        public IEnumerator ThrowOverboardAndWatchDriftCoroutine() {
            // The flight's own error log fails the run on an error; the missing Steam client logs one at the start.
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            _flight.SetThrottle(CRUISE_THROTTLE);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Assert.IsTrue(_flight.Ship.Riders.IsLocalPlayerRiding(), "The host does not ride the deck in cruise.");

            yield return ClickCoroutine(DebugSections.PLAYER, THROW_LABEL);
            ShipOverboardRider overboard = LocalOverboard;
            yield return _flight.Session.WaitForCoroutine(() => overboard.IsOverboard, OVERBOARD_TIMEOUT_SECONDS, "the server to put the thrown host overboard");
            Assert.IsTrue(LocalDrift.IsDrifting, "The host overboard does not drift on its own peer.");
            yield return CaptureCoroutine("overboard-1-thrown.png");

            float startHeight = overboard.Position.y;
            float startGap = Vector3.Distance(overboard.Position, _flight.Ship.transform.position);
            yield return new WaitForSeconds(DRIFT_WATCH_SECONDS);
            float heightChange = overboard.Position.y - startHeight;
            float gapGrowth = Vector3.Distance(overboard.Position, _flight.Ship.transform.position) - startGap;
            Debug.Log($"{LOG_PREFIX}drift: height {heightChange:+0.00;-0.00} m in {DRIFT_WATCH_SECONDS} s, the ship {gapGrowth:0.0} m farther");
            Assert.Less(Mathf.Abs(heightChange), MAX_HEIGHT_CHANGE_METERS, "The drifting host fell or rose instead of drifting level.");
            Assert.IsTrue(overboard.IsOverboard, "The host stopped drifting by itself.");
            Assert.IsFalse(_flight.Ship.Riders.IsLocalPlayerRiding(), "The drifting host still rides the deck.");
            Assert.Greater(gapGrowth, MIN_SHIP_GAP_GROWTH_METERS, "The ship did not fly on away from the drifting host.");
            Assert.AreNotEqual(Quaternion.identity, overboard.transform.Find(TUMBLING_VISUAL).localRotation, "The drifting body does not tumble.");
            yield return CaptureCoroutine("overboard-2-tumble.png");
        }

        public static IEnumerator ClickCoroutine(string section, string label) {
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(section, label);
            Assert.IsNull(clicking.Failure, $"Debug window \"{label}\": {clicking.Failure}.");
        }

        public static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }
    }
}
#endif
