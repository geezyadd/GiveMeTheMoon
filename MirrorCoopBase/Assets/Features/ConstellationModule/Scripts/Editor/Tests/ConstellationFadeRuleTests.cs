using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ConstellationModule.Editor.Tests {
    public sealed class ConstellationFadeRuleTests {
        private const float FADE_OUT_SECONDS = 0.8f;
        private const float SWITCH_REVEAL_DELAY_SECONDS = 0.3f;
        private const float TOLERANCE = 0.001f;

        private ConstellationHighlightConfiguration _configuration;
        private ConstellationFadeRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ConstellationHighlightConfiguration>();
            SerializedObject configuration = new SerializedObject(_configuration);
            configuration.FindProperty("_fadeOutSeconds").floatValue = FADE_OUT_SECONDS;
            configuration.FindProperty("_switchRevealDelaySeconds").floatValue = SWITCH_REVEAL_DELAY_SECONDS;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ConstellationFadeRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenNothingIsShown_ThenTheRevealStartsAtOnce() =>
            Assert.AreEqual(0f, _rule.GetRevealStartDelaySeconds(hasShown: false, isReplay: false), TOLERANCE);

        [Test]
        public void WhenAnotherConstellationIsShown_ThenTheRevealStartsAfterTheSwitchDelay() =>
            Assert.AreEqual(SWITCH_REVEAL_DELAY_SECONDS, _rule.GetRevealStartDelaySeconds(hasShown: true, isReplay: false), TOLERANCE);

        [Test]
        public void WhenTheShownConstellationIsPickedAgain_ThenTheRevealWaitsForTheWholeFadeOut() =>
            Assert.AreEqual(FADE_OUT_SECONDS, _rule.GetRevealStartDelaySeconds(hasShown: true, isReplay: true), TOLERANCE);

        [Test]
        public void WhenAFigureFades_ThenItGoesFromFullToNothingOverTheFadeOut() {
            Assert.AreEqual(1f, _rule.GetFadeOutAlpha(0f), TOLERANCE);
            Assert.AreEqual(0.5f, _rule.GetFadeOutAlpha(FADE_OUT_SECONDS * 0.5f), TOLERANCE);
            Assert.AreEqual(0f, _rule.GetFadeOutAlpha(FADE_OUT_SECONDS), TOLERANCE);
            Assert.IsFalse(_rule.IsFadeOutOver(FADE_OUT_SECONDS * 0.5f));
            Assert.IsTrue(_rule.IsFadeOutOver(FADE_OUT_SECONDS));
        }
    }
}
