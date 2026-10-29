using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // A station offers 0..2 parcels by the weights, each reward between the bounds on the step; the shipped data pays
    // clearly more than a good flight's ore.
    public sealed class ParcelOfferRuleTests {
        private const int MIN_REWARD = 300;
        private const int MAX_REWARD = 500;
        private const int STEP = 50;
        private const float LOW_ROLL = 0.1f;
        private const float MIDDLE_ROLL = 0.5f;
        private const float HIGH_ROLL = 0.9f;
        private const float TOP_ROLL = 1f;
        // A good flight's ore pays about 150-250 (ShopModule AGENTS.md).
        private const int GOOD_FLIGHT_ORE = 250;

        private ParcelConfiguration _configuration;
        private ParcelOfferRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ParcelConfiguration>();
            SerializedObject serialized = new(_configuration);
            SerializedProperty weights = serialized.FindProperty("_offerCountWeights");
            weights.arraySize = 3;
            weights.GetArrayElementAtIndex(0).floatValue = 0.2f;
            weights.GetArrayElementAtIndex(1).floatValue = 0.4f;
            weights.GetArrayElementAtIndex(2).floatValue = 0.4f;
            serialized.FindProperty("_minReward").intValue = MIN_REWARD;
            serialized.FindProperty("_maxReward").intValue = MAX_REWARD;
            serialized.FindProperty("_rewardStep").intValue = STEP;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _rule = new ParcelOfferRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenTheRollGoesUp_ThenTheStationOffersNoneOneThenTwo() {
            Assert.AreEqual(0, _rule.CalculateOfferCount(LOW_ROLL));
            Assert.AreEqual(1, _rule.CalculateOfferCount(MIDDLE_ROLL));
            Assert.AreEqual(2, _rule.CalculateOfferCount(HIGH_ROLL));
            Assert.AreEqual(ParcelOfferRule.MAX_OFFERS, _rule.CalculateOfferCount(TOP_ROLL));
        }

        [Test]
        public void WhenARewardIsRolled_ThenItLiesBetweenTheBoundsOnTheStep() {
            Assert.AreEqual(MIN_REWARD, _rule.CalculateReward(0f));
            Assert.AreEqual(MAX_REWARD, _rule.CalculateReward(TOP_ROLL));
            long middle = _rule.CalculateReward(MIDDLE_ROLL);
            Assert.That(middle, Is.InRange(MIN_REWARD, MAX_REWARD));
            Assert.AreEqual(0, (middle - MIN_REWARD) % STEP);
        }

        [Test]
        public void WhenTheShippedDataIsRead_ThenAParcelPaysClearlyMoreThanAGoodFlightsOre() {
            ParcelConfiguration defaults = AddressableConfigurationLoader.Load<ParcelConfiguration>(Address.Configurations.ParcelConfiguration_Default);
            Assert.Greater(defaults.MinReward, GOOD_FLIGHT_ORE);
            Assert.IsNotNull(defaults.CratePrefab);
            Assert.IsNotNull(defaults.StolenSonarKind);
            Assert.IsTrue(defaults.StolenSonarKind.IsMarkable, "The navigator cannot mark a stolen parcel for the crew.");
            new ParcelConfigurationValidator(defaults).Initialize();
        }
    }
}
