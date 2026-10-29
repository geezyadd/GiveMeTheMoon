#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using Features.BugModule.Scripts.Brain;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.LobbyModule.Scripts;
using Features.MvpModule;
using Features.PointOfInterestModule.Scripts.Client;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.PointOfInterest {
    // The steps the point of interest tests share on a host in cruise: the debug window's buttons clicked as a player
    // clicks them, the ship flown into a point's zone, the entry cutscene watched (or skipped with Space through the
    // jump input), the bugs counted, the reward container rammed and the synced balance checked; screenshots in the
    // workspace's poi folder.
    public sealed class PointOfInterestTestSteps {
        public const string LOG_PREFIX = "[PointOfInterest] ";

        private const string MOVE_LABEL = "Move the ship to the nearest point of interest";
        private const string TEST_CUTSCENE_LABEL = "Play a test cutscene";
        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/poi";
        private const float POINTS_TIMEOUT_SECONDS = 15f;
        private const float ENTRY_TIMEOUT_SECONDS = 30f;
        private const float CUTSCENE_START_TIMEOUT_SECONDS = 2f;
        private const float CUTSCENE_MAX_SECONDS = 5.5f;
        private const float SKIP_AFTER_SECONDS = 1.2f;
        private const float SKIP_TIMEOUT_SECONDS = 1f;
        private const float NEST_SCREENSHOT_SECONDS = 1f;
        private const float FLOCK_SCREENSHOT_SECONDS = 2.6f;
        private const float RAM_TIMEOUT_SECONDS = 40f;
        private const float PAY_TIMEOUT_SECONDS = 3f;
        private const float APPROACH_TOLERANCE_METERS = 6f;
        private const float RAM_THROTTLE = 1f;
        private const float PAID_SETTLE_SECONDS = 0.5f;
        private const string PHOTO_CAMERA_NAME = "PointOfInterestPhotoCamera";
        private const int PHOTO_WIDTH = 1920;
        private const int PHOTO_HEIGHT = 1080;
        private const int PHOTO_DEPTH = 24;

        private readonly LoopSmokeSession _session;
        private readonly Action<float> _setThrottle;

        public IPointsOfInterest Points => SessionQueries.Resolve<IPointsOfInterest>();
        public PointOfInterestConfiguration Configuration => SessionQueries.Resolve<PointOfInterestConfiguration>();

        public PointOfInterestTestSteps(LoopSmokeSession session, Action<float> setThrottle) {
            _session = session;
            _setThrottle = setThrottle;
        }

        public IEnumerator WaitForPointsCoroutine() {
            yield return _session.WaitForCoroutine(() => Points.Count > 0, POINTS_TIMEOUT_SECONDS, "Points of interest: the field's points in the synced list");
            for (int i = 0; i < Points.Count; i++)
                Assert.AreEqual(PointOfInterestState.Dormant, Points.Get(i).State, $"Points of interest: point {Points.Get(i).Id} is not dormant in a new field.");

            Debug.Log($"{LOG_PREFIX}{Points.Count} point(s) in the field");
        }

        // The debug window's move: just outside the nearest standing point's zone, facing it.
        public IEnumerator MoveToNearestPointCoroutine(Action<int> found) {
            yield return ClickCoroutine(DebugSections.POINTS_OF_INTEREST, MOVE_LABEL);
            yield return null;
            PointOfInterestEntry point = FindNearestStanding();
            float distance = Vector3.Distance(Ship.transform.position, point.Position);
            float expected = Configuration.ZoneRadiusMeters + Configuration.DebugApproachMeters;
            Assert.AreEqual(expected, distance, APPROACH_TOLERANCE_METERS, $"Points of interest: the debug move put the ship {distance:0} m from point {point.Id}.");
            Assert.AreEqual(PointOfInterestState.Dormant, point.State, "Points of interest: the point started before the ship entered its zone.");
            found(point.Id);
        }

        public IEnumerator EnterZoneCoroutine(int pointId, float throttle) {
            _setThrottle(throttle);
            yield return _session.WaitForCoroutine(() => StateOf(pointId) == PointOfInterestState.Triggered, ENTRY_TIMEOUT_SECONDS, $"Points of interest: point {pointId} started by the ship entering its zone");
            Debug.Log($"{LOG_PREFIX}point {pointId} started, ship {Vector3.Distance(Ship.transform.position, PositionOf(pointId)):0} m from it");
        }

        // The cutscene plays on this peer: controls blocked, HUD hidden, the ship's throttle at Stop; it ends by itself
        // within its length, or at once on Space.
        public IEnumerator WatchEntryCutsceneCoroutine(bool isSkipped, string nestScreenshot, string flockScreenshot) {
            CutsceneLocalModel cutscene = SessionQueries.Resolve<CutsceneLocalModel>();
            yield return _session.WaitForCoroutine(() => cutscene.IsPlaying, CUTSCENE_START_TIMEOUT_SECONDS, "Cutscene: the entry cutscene started on the host");
            float startedAt = Time.time;
            Assert.IsTrue(SessionQueries.Resolve<PlayerControlBlockModel>().IsBlocked, "Cutscene: the player's controls are not blocked.");
            Assert.AreEqual(WindowStatus.Hidden, HudStatus(), "Cutscene: the HUD is not hidden.");
            Assert.AreEqual(0f, SessionQueries.Resolve<IReadOnlyShipThrottleModel>().Position, "Cutscene: the throttle is not at Stop.");
            yield return CaptureAtCoroutine(startedAt + NEST_SCREENSHOT_SECONDS, nestScreenshot);
            if (isSkipped) {
                yield return new WaitUntil(() => Time.time >= startedAt + SKIP_AFTER_SECONDS);
                SessionQueries.Resolve<IInputService>().Jump.Performed?.Invoke();
                yield return _session.WaitForCoroutine(() => !cutscene.IsPlaying, SKIP_TIMEOUT_SECONDS, "Cutscene: skipped with Space");
            } else {
                yield return CaptureAtCoroutine(startedAt + FLOCK_SCREENSHOT_SECONDS, flockScreenshot);
                yield return _session.WaitForCoroutine(() => !cutscene.IsPlaying, CUTSCENE_MAX_SECONDS, "Cutscene: the entry cutscene ended by itself");
            }

            Debug.Log($"{LOG_PREFIX}the cutscene played {Time.time - startedAt:0.0} s{(isSkipped ? " (skipped)" : string.Empty)}");
            Assert.IsFalse(SessionQueries.Resolve<PlayerControlBlockModel>().IsBlocked, "Cutscene: the controls are still blocked after it.");
            Assert.AreEqual(WindowStatus.Showed, HudStatus(), "Cutscene: the HUD did not come back.");
        }

        // The debug window's test cutscene, skipped with Space at once.
        public IEnumerator PlayAndSkipTestCutsceneCoroutine() {
            CutsceneLocalModel cutscene = SessionQueries.Resolve<CutsceneLocalModel>();
            yield return ClickCoroutine(DebugSections.VIEW, TEST_CUTSCENE_LABEL);
            yield return _session.WaitForCoroutine(() => cutscene.IsPlaying, CUTSCENE_START_TIMEOUT_SECONDS, "Cutscene: the test cutscene started");
            SessionQueries.Resolve<IInputService>().Jump.Performed?.Invoke();
            yield return _session.WaitForCoroutine(() => !cutscene.IsPlaying, SKIP_TIMEOUT_SECONDS, "Cutscene: the test cutscene skipped with Space");
        }

        public void AssertBugsCameOut() {
            int expected = SessionQueries.Resolve<BugNestConfiguration>().BugCount;
            Assert.AreEqual(expected, SessionQueries.Resolve<BugServerModel>().Count, "Points of interest: the nest's bugs did not come out.");
        }

        // The ship turns to the container and flies through it; the server opens it and pays the crew once.
        public IEnumerator RamContainerCoroutine(int pointId) {
            long balance = SessionQueries.Resolve<IReadOnlyWalletModel>().Balance;
            Ship.FlightControlDebug.DebugFace(PositionOf(pointId) - Ship.transform.position);
            _setThrottle(RAM_THROTTLE);
            yield return _session.WaitForCoroutine(() => StateOf(pointId) == PointOfInterestState.Opened, RAM_TIMEOUT_SECONDS, $"Points of interest: point {pointId}'s container rammed open");
            yield return AssertPaidOnceCoroutine(pointId, balance);
        }

        public IEnumerator AssertPaidOnceCoroutine(int pointId, long balanceBefore) {
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            int reward = SessionQueries.Resolve<IPointOfInterestContentCatalog>().GetRequired(ContentOf(pointId)).Reward;
            yield return _session.WaitForCoroutine(() => wallet.Balance == balanceBefore + reward, PAY_TIMEOUT_SECONDS, $"Points of interest: the crew balance up by the reward {reward}");
            yield return new WaitForSeconds(PAID_SETTLE_SECONDS);
            Assert.AreEqual(balanceBefore + reward, wallet.Balance, "Points of interest: the crew was paid more than once.");
            Assert.IsFalse(SessionQueries.Resolve<PointOfInterestSceneModel>().TryGetView(pointId, out _), "Points of interest: the opened container is still shown.");
            Debug.Log($"{LOG_PREFIX}point {pointId} opened, balance {balanceBefore} -> {wallet.Balance}");
        }

        public IEnumerator ClickCoroutine(string section, string label) {
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(section, label);
            Assert.IsNull(clicking.Failure, $"Debug window \"{label}\": {clicking.Failure}.");
        }

        // A 1920 x 1080 photo with the game camera's settings from a pose of its own (no HUD in it); without a pose, from
        // where the game camera looks now (the cutscene's view).
        public static IEnumerator CapturePhotoCoroutine(string fileName, Vector3? position = null, Vector3? focus = null) {
            yield return new WaitForEndOfFrame();
            Camera game = SessionQueries.Resolve<IGameCameraService>().OutputCamera;
            Camera photo = new GameObject(PHOTO_CAMERA_NAME).AddComponent<Camera>();
            photo.CopyFrom(game);
            if (position.HasValue)
                photo.transform.position = position.Value;

            if (focus.HasValue)
                photo.transform.LookAt(focus.Value);

            RenderTexture target = RenderTexture.GetTemporary(PHOTO_WIDTH, PHOTO_HEIGHT, PHOTO_DEPTH);
            photo.targetTexture = target;
            photo.Render();
            photo.targetTexture = null;
            UnityEngine.Object.Destroy(photo.gameObject);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D picture = new(PHOTO_WIDTH, PHOTO_HEIGHT, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, PHOTO_WIDTH, PHOTO_HEIGHT), 0, 0);
            picture.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, fileName), picture.EncodeToPNG());
            UnityEngine.Object.Destroy(picture);
        }

        public static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }

        public PointOfInterestState StateOf(int pointId) =>
            Points.TryFindIndex(pointId, out int index) ? Points.Get(index).State : PointOfInterestState.None;

        public string ContentOf(int pointId) {
            Assert.IsTrue(Points.TryFindIndex(pointId, out int index), $"Points of interest: point {pointId} is gone.");
            return Points.Get(index).ContentId;
        }

        public Vector3 PositionOf(int pointId) {
            Assert.IsTrue(Points.TryFindIndex(pointId, out int index), $"Points of interest: point {pointId} is gone.");
            return Points.Get(index).Position;
        }

        private ShipBase Ship => _session.Ship;

        private static IEnumerator CaptureAtCoroutine(float time, string fileName) {
            yield return new WaitUntil(() => Time.time >= time);
            yield return CapturePhotoCoroutine(fileName);
        }

        private static WindowStatus HudStatus() =>
            SessionQueries.Resolve<IWindowsService>().GetWindow(typeof(GameHudWindow)).WindowStatus;

        private PointOfInterestEntry FindNearestStanding() {
            PointOfInterestEntry nearest = default;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < Points.Count; i++) {
                PointOfInterestEntry point = Points.Get(i);
                float distance = Vector3.Distance(point.Position, Ship.transform.position);
                if (point.State == PointOfInterestState.Opened || distance >= nearestDistance)
                    continue;

                nearest = point;
                nearestDistance = distance;
            }

            Assert.Less(nearestDistance, float.MaxValue, "Points of interest: no standing point in the field.");
            return nearest;
        }
    }
}
#endif
