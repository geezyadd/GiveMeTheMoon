#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Spawn;
using Features.CameraModule.Scripts;
using Features.FieldAgentModule.Scripts.Client;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.Bug {
    // The steps the bug tests share, on the field agent tests' host flight: the random bugs switched off and a group
    // sent through the debug window's buttons as a player clicks them, the wait for a bug to reach a stage, the view
    // turned onto a bug through the look input, and the screenshots in the workspace's basic-bugs folder.
    public sealed class BugTestSteps {
        public const string LOG_PREFIX = "[Bugs] ";

        private const string SPAWN_LABEL = "Spawn bugs (N)";
        private const string RANDOM_ON_LABEL = "Random bugs: on";
        private const string RANDOM_OFF_LABEL = "Random bugs: off";
        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/basic-bugs";
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float PROGRESS_LOG_SECONDS = 5f;
        // The test pitch is not clamped by the camera: past straight up or down the view turns over.
        private const float MAX_PITCH_DEGREES = 85f;

        private readonly FieldAgentTestFlight _flight;

        public BugServerModel Bugs { get; private set; }
        public BugConfiguration Configuration { get; private set; }

        public BugTestSteps(FieldAgentTestFlight flight) =>
            _flight = flight;

        // In the cruise: random bugs off (only the test's group flies), then one group from the debug window, of the
        // default size or of the typed count.
        public IEnumerator SpawnGroupCoroutine(int count = 0) {
            Bugs = SessionQueries.Resolve<BugServerModel>();
            Configuration = SessionQueries.Resolve<BugConfiguration>();
            // The loop session starts with the random bugs off; the toggle switches them on and off again.
            BugRandomSpawnModel random = SessionQueries.Resolve<BugRandomSpawnModel>();
            Assert.IsFalse(random.IsEnabled, "The random bugs are on in a test session.");
            yield return _flight.ClickDebugActionCoroutine(RANDOM_OFF_LABEL);
            Assert.IsTrue(random.IsEnabled, "The debug toggle did not switch the random bugs on.");
            yield return _flight.ClickDebugActionCoroutine(RANDOM_ON_LABEL);
            Assert.IsFalse(random.IsEnabled, "The debug toggle did not switch the random bugs off.");
            int expected = count > 0 ? count : Configuration.DebugGroupSize;
            yield return _flight.ClickDebugActionCoroutine(SPAWN_LABEL, count > 0 ? count.ToString(CultureInfo.InvariantCulture) : null);
            Assert.AreEqual(expected, Bugs.Count, "The debug button did not send the bug group.");
            Debug.Log($"{LOG_PREFIX}{Bugs.Count} bugs spawned, {DescribeDistance()} from the ship");
        }

        public IEnumerator WaitForStageCoroutine(BugStage stage, float timeoutSeconds, Action<int> found, Func<IEnumerator> whileWaiting = null) {
            float deadline = Time.time + timeoutSeconds;
            float nextLog = Time.time;
            while (TryFindInStage(stage, out int id) == false) {
                if (Time.time >= nextLog) {
                    nextLog = Time.time + PROGRESS_LOG_SECONDS;
                    Debug.Log($"{LOG_PREFIX}waiting for {stage}: {DescribeStages()}, {DescribeDistance()} from the ship");
                }

                Assert.Less(Time.time, deadline, $"No bug reached {stage} in {timeoutSeconds} s ({DescribeStages()}, {DescribeDistance()} from the ship).");
                Assert.AreEqual(0, _flight.RockPushOuts, "The steering let a bug into a rock sphere.");
                if (whileWaiting != null)
                    yield return whileWaiting();

                yield return null;
            }

            found(FindInStage(stage));
        }

        public bool TryFindInStage(BugStage stage, out int bugId) {
            bugId = 0;
            foreach (int id in Bugs.Ids) {
                if (Bugs.TryGet(id, out BugAgent bug) == false || bug.Stage != stage)
                    continue;

                bugId = id;
                return true;
            }

            return false;
        }

        // Turns the view through the look input onto where this peer shows the bug; false once the bug is not shown.
        public static IEnumerator AimAtShownBugCoroutine(int bugId, Action<bool> isOnTarget) {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            CameraLookDriver.DebugPitchOverride = true;
            while (shown.TryGetShownWorldPosition(bugId, out Vector3 position)) {
                if (KeepAiming(position)) {
                    isOnTarget(true);
                    yield break;
                }

                if (Time.realtimeSinceStartup > deadline)
                    break;

                yield return null;
            }

            isOnTarget(false);
        }

        // One step of the turn onto the point, the pitch kept short of straight up or down; true once on it.
        public static bool KeepAiming(Vector3 point) {
            bool isOnTarget = HandWeaponAiming.KeepAiming(point);
            CameraLookDriver.DebugPitch = Mathf.Clamp(CameraLookDriver.DebugPitch, -MAX_PITCH_DEGREES, MAX_PITCH_DEGREES);
            return isOnTarget;
        }

        public static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }

        public float FindNearestDistance() {
            float nearest = float.MaxValue;
            foreach (int id in Bugs.Ids) {
                if (_flight.Control.TryGetWorldPosition(id, out Vector3 position))
                    nearest = Mathf.Min(nearest, Vector3.Distance(position, _flight.Ship.transform.position));
            }

            return nearest;
        }

        public int FindNearestBug() {
            int nearestId = -1;
            float nearest = float.MaxValue;
            foreach (int id in Bugs.Ids) {
                if (_flight.Control.TryGetWorldPosition(id, out Vector3 position) == false)
                    continue;

                float distance = Vector3.Distance(position, _flight.Ship.transform.position);
                if (distance >= nearest)
                    continue;

                nearest = distance;
                nearestId = id;
            }

            return nearestId;
        }

        private int FindInStage(BugStage stage) {
            TryFindInStage(stage, out int id);
            return id;
        }

        private string DescribeStages() {
            string stages = string.Empty;
            foreach (int id in Bugs.Ids) {
                if (Bugs.TryGet(id, out BugAgent bug))
                    stages += $"{id}:{bug.Stage} ";
            }

            return stages.Length == 0 ? "no bugs" : stages.TrimEnd();
        }

        private string DescribeDistance() {
            float nearest = FindNearestDistance();
            return nearest == float.MaxValue ? "-" : $"nearest {nearest:0} m";
        }
    }
}
#endif
