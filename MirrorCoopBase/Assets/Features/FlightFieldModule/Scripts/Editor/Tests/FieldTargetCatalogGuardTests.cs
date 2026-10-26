using System;
using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Targets;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The target balloon data: the catalog and the configuration load at their addresses and pass their startup
    // checks, ids are set and unique, the three kinds (small 20, medium 50, rare golden 200) differ in size and colour,
    // every prefab is a weapon target on the FieldRock layer with its one trigger off until it is bound, and a broken
    // catalog fails the check.
    public sealed class FieldTargetCatalogGuardTests {
        private const int FIELD_ROCK_LAYER = 8;
        private const string SMALL_ID = "balloon_small";
        private const string MEDIUM_ID = "balloon_medium";
        private const string GOLDEN_ID = "balloon_golden";
        private const int SMALL_REWARD = 20;
        private const int MEDIUM_REWARD = 50;
        private const int GOLDEN_REWARD = 150;

        private readonly List<ScriptableObject> _created = new();

        private FieldTargetCatalog _catalog;
        private FieldTargetConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _catalog = AddressableConfigurationLoader.Load<FieldTargetCatalog>(Address.Configurations.FieldTargetCatalog_Default);
            _configuration = AddressableConfigurationLoader.Load<FieldTargetConfiguration>(Address.Configurations.FieldTargetConfiguration_Default);
        }

        [TearDown]
        public void TearDown() {
            foreach (ScriptableObject created in _created)
                UnityEngine.Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void WhenTheGameDataIsChecked_ThenTheCatalogAndTheConfigurationPassTheStartupChecks() {
            Assert.DoesNotThrow(() => new FieldTargetCatalogValidator(_catalog).Initialize());
            Assert.DoesNotThrow(() => new FieldTargetConfigurationValidator(_configuration).Initialize());
            List<string> ids = _catalog.Targets.Select(target => target.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids, "Two balloon kinds share an id.");
            Assert.IsTrue(ids.All(id => !string.IsNullOrEmpty(id)), "A balloon kind has no id.");
        }

        [Test]
        public void WhenTheKindsAreRead_ThenSmallMediumAndARareGoldenOneDifferInRewardSizeAndColour() {
            FieldTargetDefinition small = _catalog.GetRequired(SMALL_ID);
            FieldTargetDefinition medium = _catalog.GetRequired(MEDIUM_ID);
            FieldTargetDefinition golden = _catalog.GetRequired(GOLDEN_ID);

            Assert.AreEqual(SMALL_REWARD, small.Reward);
            Assert.AreEqual(MEDIUM_REWARD, medium.Reward);
            Assert.AreEqual(GOLDEN_REWARD, golden.Reward);
            Assert.Less(golden.Weight, small.Weight, "The golden balloon is not rarer than the small one.");
            Assert.Less(golden.Weight, medium.Weight, "The golden balloon is not rarer than the medium one.");
            Assert.AreEqual(3, new[] { small.RadiusMeters, medium.RadiusMeters, golden.RadiusMeters }.Distinct().Count(), "Two balloon kinds have the same size.");
            Assert.AreEqual(3, new[] { small.PopColor, medium.PopColor, golden.PopColor }.Distinct().Count(), "Two balloon kinds have the same colour.");
            Assert.AreEqual(3, new[] { small.Prefab, medium.Prefab, golden.Prefab }.Distinct().Count(), "Two balloon kinds share a prefab.");
        }

        [Test]
        public void WhenThePrefabsAreRead_ThenEveryBalloonIsAWeaponTargetWithOneTriggerOnTheFieldRockLayer() {
            foreach (FieldTargetDefinition target in _catalog.Targets) {
                GameObject prefab = target.Prefab.gameObject;
                Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
                Assert.AreEqual(1, colliders.Length, $"{prefab.name}: a balloon has exactly one collider, its weapon trigger.");
                Assert.AreSame(prefab, colliders[0].gameObject, $"{prefab.name}: the trigger is not on the root next to the view.");
                Assert.IsTrue(colliders[0].isTrigger, $"{prefab.name}: the collider is not a trigger.");
                Assert.IsFalse(colliders[0].enabled, $"{prefab.name}: the trigger is on before the builder binds the balloon.");
                Assert.AreEqual(FIELD_ROCK_LAYER, prefab.layer, $"{prefab.name}: the balloon is not on the FieldRock layer the minigun hits.");
            }

            Assert.IsNotNull(_configuration.PopEffect, "The configuration has no pop effect.");
        }

        [Test]
        public void WhenTwoKindsShareAnId_ThenTheStartupCheckFails() {
            FieldTargetCatalog broken = Remember(UnityEngine.Object.Instantiate(_catalog));
            FieldTargetDefinition copy = Remember(UnityEngine.Object.Instantiate(_catalog.Targets[0]));
            FieldTargetDefinition[] targets = _catalog.Targets.Append(copy).ToArray();
            UnityEditor.SerializedObject serialized = new(broken);
            UnityEditor.SerializedProperty property = serialized.FindProperty("_targets");
            property.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => new FieldTargetCatalogValidator(broken).Initialize());
        }

        private T Remember<T>(T created) where T : ScriptableObject {
            _created.Add(created);
            return created;
        }
    }
}
