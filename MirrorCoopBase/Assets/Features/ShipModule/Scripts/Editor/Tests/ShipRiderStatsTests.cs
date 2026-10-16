using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipRiderStatsTests {
        // The ride reads jump height, coyote time, jump buffer and air gravity from these stats; unset, it throws in flight.
        private const string STATS_FIELD = "_stats";
        private const string PREFAB_FILTER = "t:Prefab";
        private const string FEATURES_FOLDER = "Assets/Features";

        private List<ShipRider> _riders;

        [SetUp]
        public void SetUp() {
            _riders = new List<ShipRider>();
            string[] guids = AssetDatabase.FindAssets(PREFAB_FILTER, new[] { FEATURES_FOLDER });
            for (int i = 0; i < guids.Length; i++) {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (prefab != null)
                    _riders.AddRange(prefab.GetComponentsInChildren<ShipRider>(true));
            }
        }

        [Test]
        public void WhenPrefabsLoaded_ThenSomePrefabHasAShipRider() =>
            Assert.IsNotEmpty(_riders);

        [Test]
        public void WhenPrefabsLoaded_ThenEveryShipRiderHasStatsOfItsOwnPrefab() {
            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                Object stats = new SerializedObject(rider).FindProperty(STATS_FIELD).objectReferenceValue;
                Assert.IsInstanceOf<PlayerStatEntity>(stats, rider.transform.root.name + " has no ShipRider stats.");
                Assert.AreSame(rider.transform.root, ((PlayerStatEntity)stats).transform.root,
                    rider.transform.root.name + " ShipRider stats belong to another prefab.");
            }
        }
    }
}
