#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CharacterMovableModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.LobbyModule.Scripts;
using Features.MvpModule;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Cutscene {
    // The host in the cruise clicks the debug window's "Play a test cutscene": its camera plays it with its controls
    // blocked and its HUD hidden; Space (the jump input) skips it at once and gives the controls and the HUD back.
    public sealed class CutsceneSkipTest {
        private const int TEST_TIMEOUT_MS = 180000;
        private const string TEST_CUTSCENE_LABEL = "Play a test cutscene";
        private const float START_TIMEOUT_SECONDS = 2f;
        private const float SKIP_TIMEOUT_SECONDS = 1f;
        private const float WATCH_SECONDS = 1f;
        private const string SCREENSHOT = "test-cutscene.png";

        private FieldAgentTestFlight _flight;

        [SetUp]
        public void SetUp() =>
            _flight = new FieldAgentTestFlight();

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTheHostPlaysTheTestCutscene_AndPressesSpace_ThenItEndsAndTheControlsAndHudComeBack() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            CutsceneLocalModel cutscene = SessionQueries.Resolve<CutsceneLocalModel>();
            PlayerControlBlockModel controlBlock = SessionQueries.Resolve<PlayerControlBlockModel>();

            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.VIEW, TEST_CUTSCENE_LABEL);
            Assert.IsNull(clicking.Failure, $"Debug window \"{TEST_CUTSCENE_LABEL}\": {clicking.Failure}.");
            yield return _flight.Session.WaitForCoroutine(() => cutscene.IsPlaying, START_TIMEOUT_SECONDS, "Cutscene: the test cutscene started");
            Assert.IsTrue(controlBlock.IsBlocked, "Cutscene: the controls are not blocked.");
            Assert.AreEqual(WindowStatus.Hidden, HudStatus(), "Cutscene: the HUD is not hidden.");
            yield return new WaitForSeconds(WATCH_SECONDS);
            Assert.IsTrue(cutscene.IsPlaying, "Cutscene: the test cutscene ended before Space.");
            yield return PointOfInterest.PointOfInterestTestSteps.CapturePhotoCoroutine(SCREENSHOT);

            SessionQueries.Resolve<IInputService>().Jump.Performed?.Invoke();
            yield return _flight.Session.WaitForCoroutine(() => !cutscene.IsPlaying, SKIP_TIMEOUT_SECONDS, "Cutscene: the test cutscene skipped with Space");
            Assert.IsFalse(controlBlock.IsBlocked, "Cutscene: the controls are still blocked after the skip.");
            Assert.AreEqual(WindowStatus.Showed, HudStatus(), "Cutscene: the HUD did not come back after the skip.");

            yield return _flight.LandAndLeaveCoroutine();
        }

        private static WindowStatus HudStatus() =>
            SessionQueries.Resolve<IWindowsService>().GetWindow(typeof(GameHudWindow)).WindowStatus;
    }
}
#endif
