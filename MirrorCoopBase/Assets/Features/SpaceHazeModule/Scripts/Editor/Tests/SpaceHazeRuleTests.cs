using Features.SpaceHazeModule.Data;
using Features.SpaceHazeModule.Scripts.Configurations;
using Features.SpaceHazeModule.Scripts.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.SpaceHazeModule.Editor.Tests {
    public sealed class SpaceHazeRuleTests {
        private const float CLEAR_METERS = 150f;
        private const float HIDDEN_METERS = 250f;
        private const float BACKDROP_START_METERS = 1800f;
        private const float BACKDROP_END_METERS = 2000f;
        private const float TOLERANCE = 0.001f;

        private SpaceHazeConfiguration _configuration;
        private SpaceHazeModel _model;
        private SpaceHazeRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<SpaceHazeConfiguration>();
            SerializedObject configuration = new SerializedObject(_configuration);
            configuration.FindProperty("_clearMeters").floatValue = CLEAR_METERS;
            configuration.FindProperty("_hiddenMeters").floatValue = HIDDEN_METERS;
            configuration.FindProperty("_backdropStartMeters").floatValue = BACKDROP_START_METERS;
            configuration.FindProperty("_backdropEndMeters").floatValue = BACKDROP_END_METERS;
            configuration.FindProperty("_strength").floatValue = 1f;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            _model = new SpaceHazeModel();
            _rule = new SpaceHazeRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenAThingIsWithinTheClearDistance_ThenNothingOfItIsHazed() {
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(0f, _rule.GetHazeShare(shape, 10f), TOLERANCE);
            Assert.AreEqual(0f, _rule.GetHazeShare(shape, CLEAR_METERS), TOLERANCE);
        }

        [Test]
        public void WhenAThingIsBetweenTheClearAndTheHiddenDistance_ThenItFadesHalfwayInTheMiddle() {
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(0.5f, _rule.GetHazeShare(shape, (CLEAR_METERS + HIDDEN_METERS) * 0.5f), TOLERANCE);
            Assert.Less(_rule.GetHazeShare(shape, 180f), _rule.GetHazeShare(shape, 220f));
        }

        [Test]
        public void WhenAThingIsPastTheHiddenDistance_ThenItIsHiddenUntilTheBackdrop() {
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(1f, _rule.GetHazeShare(shape, HIDDEN_METERS), TOLERANCE);
            Assert.AreEqual(1f, _rule.GetHazeShare(shape, 1000f), TOLERANCE);
            Assert.AreEqual(1f, _rule.GetHazeShare(shape, BACKDROP_START_METERS), TOLERANCE);
        }

        [Test]
        public void WhenAThingIsPastTheBackdropEnd_ThenItIsNotHazed() {
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(0.5f, _rule.GetHazeShare(shape, (BACKDROP_START_METERS + BACKDROP_END_METERS) * 0.5f), TOLERANCE);
            Assert.AreEqual(0f, _rule.GetHazeShare(shape, BACKDROP_END_METERS), TOLERANCE);
            Assert.AreEqual(0f, _rule.GetHazeShare(shape, 3000f), TOLERANCE);
        }

        [Test]
        public void WhenTheDebugTurnsTheHazeOff_ThenNothingIsHazed() {
            _model.SetEnabled(false);
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(0f, shape.Strength, TOLERANCE);
            Assert.AreEqual(0f, _rule.GetHazeShare(shape, 1000f), TOLERANCE);
        }

        [Test]
        public void WhenTheDebugSetsAClearDistance_ThenTheFadeMovesAndKeepsItsLength() {
            _model.SetClearMetersOverride(400f);
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.AreEqual(400f, shape.ClearMeters, TOLERANCE);
            Assert.AreEqual(400f + HIDDEN_METERS - CLEAR_METERS, shape.HiddenMeters, TOLERANCE);
            Assert.AreEqual(0f, _rule.GetHazeShare(shape, 300f), TOLERANCE);
        }

        [Test]
        public void WhenTheDebugClearDistanceReachesTheBackdrop_ThenTheBackdropMovesBehindTheFade() {
            _model.SetClearMetersOverride(1900f);
            SpaceHazeShape shape = _rule.CreateShape(_model);

            Assert.GreaterOrEqual(shape.BackdropStartMeters, shape.HiddenMeters);
            Assert.Greater(shape.BackdropEndMeters, shape.BackdropStartMeters);
            Assert.AreEqual(1f, _rule.GetHazeShare(shape, shape.HiddenMeters), TOLERANCE);
        }
    }
}
