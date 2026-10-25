using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipStatsConfigurationValidatorTests {
        private const string STATS_CONFIGURATION_PATH =
            "Assets/Features/ShipModule/GameResources/Configurations/ShipAccumulativeStatsConfiguration_Default.asset";
        private const string DUPLICATE_FLIGHT_SPEED_JSON =
            "{\"_defaults\":[{\"Type\":2},{\"Type\":4},{\"Type\":5},{\"Type\":5},{\"Type\":6},{\"Type\":7}]}";
        private const string MISSING_ARMOR_JSON =
            "{\"_defaults\":[{\"Type\":2},{\"Type\":5},{\"Type\":6},{\"Type\":7}]}";
        private const string NONE_LISTED_JSON =
            "{\"_defaults\":[{\"Type\":0},{\"Type\":2},{\"Type\":4},{\"Type\":5},{\"Type\":6},{\"Type\":7}]}";
        // 1 was Thrust and 3 was Handling: removed stats an old asset could still store.
        private const string REMOVED_STAT_LISTED_JSON =
            "{\"_defaults\":[{\"Type\":1},{\"Type\":2},{\"Type\":4},{\"Type\":5},{\"Type\":6},{\"Type\":7}]}";

        [Test]
        public void WhenDefaultAssetValidated_ThenItPasses() =>
            Assert.DoesNotThrow(
                new ShipStatsConfigurationValidator(
                    AssetDatabase.LoadAssetAtPath<ShipAccumulativeStatsConfiguration>(STATS_CONFIGURATION_PATH)).Initialize);

        [TestCase(DUPLICATE_FLIGHT_SPEED_JSON)]
        [TestCase(MISSING_ARMOR_JSON)]
        [TestCase(NONE_LISTED_JSON)]
        [TestCase(REMOVED_STAT_LISTED_JSON)]
        public void WhenDefaultsAreBroken_ThenValidationThrows(string defaultsJson) {
            ShipAccumulativeStatsConfiguration configuration = ScriptableObject.CreateInstance<ShipAccumulativeStatsConfiguration>();
            try {
                JsonUtility.FromJsonOverwrite(defaultsJson, configuration);
                Assert.Throws<InvalidOperationException>(new ShipStatsConfigurationValidator(configuration).Initialize);
            }
            finally {
                Object.DestroyImmediate(configuration);
            }
        }
    }
}
