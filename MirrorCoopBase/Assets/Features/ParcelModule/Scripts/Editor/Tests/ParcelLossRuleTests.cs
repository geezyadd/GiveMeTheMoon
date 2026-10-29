using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // A thief takes the parcel for good beyond the distance or the carry time; a drifting parcel is lost the same way
    // after its own time; a parcel on a rope never; a loose crate only when it fell far below the deck.
    public sealed class ParcelLossRuleTests {
        private const float LOSE_METERS = 250f;
        private const float CARRY_SECONDS = 75f;
        private const float DRIFT_SECONDS = 60f;
        private const float FALL_METERS = 30f;
        private const float NEAR = 50f;
        private const float SHORT = 10f;
        private const float MARGIN = 1f;
        private const float ON_DECK = 1f;

        private ParcelConfiguration _configuration;
        private ParcelLossRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ParcelConfiguration>();
            SerializedObject serialized = new(_configuration);
            serialized.FindProperty("_loseDistanceMeters").floatValue = LOSE_METERS;
            serialized.FindProperty("_loseCarrySeconds").floatValue = CARRY_SECONDS;
            serialized.FindProperty("_loseDriftSeconds").floatValue = DRIFT_SECONDS;
            serialized.FindProperty("_fallLoseMeters").floatValue = FALL_METERS;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ParcelLossRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenAThiefIsNearAndQuick_ThenTheParcelIsNotLost() =>
            Assert.IsFalse(_rule.IsLost(ParcelState.Carried, NEAR, SHORT, ON_DECK));

        [Test]
        public void WhenAThiefGetsTooFar_ThenTheParcelIsLost() =>
            Assert.IsTrue(_rule.IsLost(ParcelState.Carried, LOSE_METERS + MARGIN, SHORT, ON_DECK));

        [Test]
        public void WhenAThiefFliesTooLong_ThenTheParcelIsLost() =>
            Assert.IsTrue(_rule.IsLost(ParcelState.Carried, NEAR, CARRY_SECONDS + MARGIN, ON_DECK));

        [Test]
        public void WhenAParcelDriftsPastItsTimeOrTooFar_ThenItIsLost() {
            Assert.IsFalse(_rule.IsLost(ParcelState.Drifting, NEAR, DRIFT_SECONDS - MARGIN, ON_DECK));
            Assert.IsTrue(_rule.IsLost(ParcelState.Drifting, NEAR, DRIFT_SECONDS + MARGIN, ON_DECK));
            Assert.IsTrue(_rule.IsLost(ParcelState.Drifting, LOSE_METERS + MARGIN, SHORT, ON_DECK));
        }

        [Test]
        public void WhenAParcelIsOnARope_ThenItIsNeverLostByItself() =>
            Assert.IsFalse(_rule.IsLost(ParcelState.Pulled, LOSE_METERS + MARGIN, CARRY_SECONDS + DRIFT_SECONDS, -FALL_METERS - MARGIN));

        [Test]
        public void WhenALooseCrateFallsFarBelowTheDeck_ThenItWentOverboard() {
            Assert.IsFalse(_rule.IsLost(ParcelState.Loose, LOSE_METERS + MARGIN, CARRY_SECONDS + MARGIN, -FALL_METERS + MARGIN));
            Assert.IsTrue(_rule.IsLost(ParcelState.Loose, NEAR, SHORT, -FALL_METERS - MARGIN));
        }
    }
}
