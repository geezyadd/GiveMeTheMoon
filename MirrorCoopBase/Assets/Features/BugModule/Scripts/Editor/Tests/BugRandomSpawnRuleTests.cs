using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Spawn;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    public sealed class BugRandomSpawnRuleTests {
        private const float CHANCE = 0.1f;
        private const float CARGO_MULTIPLIER = 3f;
        private const int MIN_SIZE = 3;
        private const int MAX_SIZE = 5;
        private const float SURE_ROLL = 0f;
        private const float ROLL_BETWEEN = 0.2f;
        private const float HIGHEST_SHARE = 0.9999f;
        private const float RARE_CHANCE = 0.5f;
        private const float NO_BOOST = 1f;
        private const string DEFAULT_CONFIGURATION_PATH = "Assets/Features/BugModule/GameResources/Configurations/BugConfiguration_Default.asset";

        private BugConfiguration _configuration;
        private BugRandomSpawnRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<BugConfiguration>();
            SerializedObject serialized = new(_configuration);
            serialized.FindProperty("_randomSpawnChance").floatValue = CHANCE;
            serialized.FindProperty("_cargoSpawnChanceMultiplier").floatValue = CARGO_MULTIPLIER;
            serialized.FindProperty("_groupMinSize").intValue = MIN_SIZE;
            serialized.FindProperty("_groupMaxSize").intValue = MAX_SIZE;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _rule = new BugRandomSpawnRule(_configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenRandomSpawnsAreOff_ThenNoGroupComesWhateverTheRoll() =>
            Assert.IsFalse(_rule.IsGroupComing(false, SURE_ROLL, true));

        [Test]
        public void WhenARollIsAboveTheChance_ThenNoGroupComesWithoutACargo_ButOneComesWithACargoOnBoard() {
            Assert.IsFalse(_rule.IsGroupComing(true, ROLL_BETWEEN, false));
            Assert.IsTrue(_rule.IsGroupComing(true, ROLL_BETWEEN, true));
        }

        [Test]
        public void WhenTheGameRunsWithTheDefaultConfiguration_ThenRandomSpawnsAreOnAndRare() {
            BugConfiguration defaults = AssetDatabase.LoadAssetAtPath<BugConfiguration>(DEFAULT_CONFIGURATION_PATH);

            Assert.IsTrue(defaults.IsRandomSpawnEnabled);
            Assert.Less(defaults.RandomSpawnChance, RARE_CHANCE);
            Assert.Greater(defaults.CargoSpawnChanceMultiplier, NO_BOOST);
        }

        [Test]
        public void WhenAGroupComes_ThenItsSizeIsWithinTheConfiguredRange() {
            Assert.AreEqual(MIN_SIZE, _rule.CalculateGroupSize(SURE_ROLL));
            Assert.AreEqual(MAX_SIZE, _rule.CalculateGroupSize(HIGHEST_SHARE));
        }
    }
}
