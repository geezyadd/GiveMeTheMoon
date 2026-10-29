using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Content;
using Features.PointOfInterestModule.Scripts.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    // The bug nest's entry with the shipped data: the bugs come out behind the container, seen from the ship; with the
    // cutscene on the ship stops and a 3-5 s cutscene plays for everyone, with it off neither happens.
    public sealed class BugNestContentTests {
        private const float MIN_CUTSCENE_SECONDS = 3f;
        private const float MAX_CUTSCENE_SECONDS = 5f;
        private static readonly Vector3 _ship = Vector3.zero;
        private static readonly Vector3 _nest = new(0f, 0f, 100f);

        private RecordingBugSpawnService _bugSpawnService;
        private RecordingThrottleStop _throttleStop;
        private RecordingCutsceneDirector _cutsceneDirector;
        private BugNestConfiguration _configuration;
        private PointOfInterestEntry _point;

        [SetUp]
        public void SetUp() {
            _bugSpawnService = new RecordingBugSpawnService();
            _throttleStop = new RecordingThrottleStop();
            _cutsceneDirector = new RecordingCutsceneDirector();
            _configuration = AddressableConfigurationLoader.Load<BugNestConfiguration>(Address.Configurations.BugNestConfiguration_Default);
            _point = new PointOfInterestEntry { Id = 2, ContentId = _configuration.Content.Id, Position = _nest, State = PointOfInterestState.Triggered };
        }

        [Test]
        public void WhenTheShipEnters_ThenTheBugsComeOutBehindTheNest() {
            CreateContent(_configuration).ServerEnter(_point);

            Assert.AreEqual(1, _bugSpawnService.Groups);
            Assert.AreEqual(_configuration.BugCount, _bugSpawnService.LastCount);
            Assert.Greater(Vector3.Distance(_ship, _bugSpawnService.LastPoint), Vector3.Distance(_ship, _nest), "The bugs did not appear behind the nest.");
        }

        [Test]
        public void WhenTheCutsceneIsOn_ThenTheShipStopsAndAShortCutscenePlays() {
            Assert.IsTrue(_configuration.PlayEntryCutscene, "The shipped bug nest has its entry cutscene off.");

            CreateContent(_configuration).ServerEnter(_point);

            Assert.AreEqual(1, _throttleStop.Stops);
            Assert.AreEqual(1, _cutsceneDirector.Played);
            Assert.AreEqual(CutsceneShotKind.LookAtPoint, _cutsceneDirector.LastShots[0].Kind);
            Assert.AreEqual(_nest, _cutsceneDirector.LastShots[0].Focus);
            Assert.AreEqual(CutsceneShotKind.FollowTarget, _cutsceneDirector.LastShots[^1].Kind);
            Assert.That(CutsceneSeconds(), Is.InRange(MIN_CUTSCENE_SECONDS, MAX_CUTSCENE_SECONDS));
        }

        [Test]
        public void WhenTheCutsceneIsOff_ThenTheShipKeepsFlyingAndNoCutscenePlays() {
            BugNestConfiguration withoutCutscene = Object.Instantiate(_configuration);
            SerializedObject serialized = new(withoutCutscene);
            serialized.FindProperty("_playEntryCutscene").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateContent(withoutCutscene).ServerEnter(_point);

            Assert.AreEqual(0, _throttleStop.Stops);
            Assert.AreEqual(0, _cutsceneDirector.Played);
            Assert.AreEqual(1, _bugSpawnService.Groups, "The bugs did not come out without the cutscene.");
            Object.DestroyImmediate(withoutCutscene);
        }

        private BugNestContent CreateContent(BugNestConfiguration configuration) =>
            new(_bugSpawnService, _throttleStop, _cutsceneDirector, new FixedShipPosition(_ship), new BugNestCutsceneShots(configuration), new BugNestFlockTargetIds(),
                configuration);

        // The shots plus the blend back to the player.
        private float CutsceneSeconds() {
            float seconds = AddressableConfigurationLoader.Load<CutsceneConfiguration>(Address.Configurations.CutsceneConfiguration_Default).ReturnBlendSeconds;
            foreach (CutsceneShot shot in _cutsceneDirector.LastShots)
                seconds += shot.Seconds;

            return seconds;
        }
    }
}
