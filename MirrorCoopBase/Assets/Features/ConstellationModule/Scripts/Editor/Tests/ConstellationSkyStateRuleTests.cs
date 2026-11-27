using Features.ConstellationModule.Data.Generated;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ConstellationModule.Editor.Tests {
    public sealed class ConstellationSkyStateRuleTests {
        private const string ORION = "orion";
        private const string BIG_DIPPER = "big_dipper";
        private const float FADE_OUT_SECONDS = 0.8f;
        private const float SWITCH_REVEAL_DELAY_SECONDS = 0.3f;
        private const double START_TIME = 100d;
        private const double CHANGE_TIME = 104d;
        private const double TOLERANCE = 0.001d;

        private ConstellationHighlightConfiguration _configuration;
        private ConstellationSkyStateRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ConstellationHighlightConfiguration>();
            SerializedObject configuration = new SerializedObject(_configuration);
            configuration.FindProperty("_fadeOutSeconds").floatValue = FADE_OUT_SECONDS;
            configuration.FindProperty("_switchRevealDelaySeconds").floatValue = SWITCH_REVEAL_DELAY_SECONDS;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ConstellationSkyStateRule(new ConstellationFadeRule(_configuration));
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenTheSkyIsNew_ThenNothingIsShown() =>
            Assert.IsFalse(_rule.IsShown(default));

        [Test]
        public void WhenAConstellationIsPickedOnAnEmptySky_ThenItsRevealStartsAtThePick() {
            ConstellationSkyState state = _rule.CreateShown(default, ORION, START_TIME);

            Assert.IsTrue(_rule.IsShown(state));
            Assert.AreEqual(ORION, state.ConstellationId);
            Assert.AreEqual(START_TIME, state.RevealStartTime, TOLERANCE);
            Assert.AreEqual(START_TIME, state.ChangeTime, TOLERANCE);
        }

        [Test]
        public void WhenAnotherConstellationIsPicked_ThenItsRevealStartsAfterTheSwitchDelay() {
            ConstellationSkyState state = _rule.CreateShown(_rule.CreateShown(default, ORION, START_TIME), BIG_DIPPER, CHANGE_TIME);

            Assert.AreEqual(BIG_DIPPER, state.ConstellationId);
            Assert.AreEqual(CHANGE_TIME + SWITCH_REVEAL_DELAY_SECONDS, state.RevealStartTime, TOLERANCE);
            Assert.AreEqual(CHANGE_TIME, state.ChangeTime, TOLERANCE);
        }

        [Test]
        public void WhenTheShownConstellationIsPickedAgain_ThenItsRevealWaitsForTheWholeFadeOut() {
            ConstellationSkyState state = _rule.CreateShown(_rule.CreateShown(default, ORION, START_TIME), ORION, CHANGE_TIME);

            Assert.AreEqual(CHANGE_TIME + FADE_OUT_SECONDS, state.RevealStartTime, TOLERANCE);
        }

        [Test]
        public void WhenTheSkyIsHidden_ThenNothingIsShownFromTheHide() {
            ConstellationSkyState state = _rule.CreateHidden(_rule.CreateShown(default, ORION, START_TIME), CHANGE_TIME);

            Assert.IsFalse(_rule.IsShown(state));
            Assert.AreEqual(CHANGE_TIME, state.ChangeTime, TOLERANCE);
        }
    }
}
