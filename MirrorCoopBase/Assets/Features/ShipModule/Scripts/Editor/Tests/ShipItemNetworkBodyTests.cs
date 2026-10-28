using System.Collections.Generic;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipItemNetworkBodyTests {
        // A network transform syncs its target: one aimed at another prefab's transform (a copied prefab kept the
        // original's reference) never syncs its own object, and clients see it frozen where it spawned or was let go.
        private const string PREFAB_FILTER = "t:Prefab";
        private const string FEATURES_FOLDER = "Assets/Features";

        private List<NetworkTransformBase> _networkTransforms;

        [SetUp]
        public void SetUp() {
            _networkTransforms = new List<NetworkTransformBase>();
            string[] guids = AssetDatabase.FindAssets(PREFAB_FILTER, new[] { FEATURES_FOLDER });
            for (int i = 0; i < guids.Length; i++) {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (prefab != null)
                    _networkTransforms.AddRange(prefab.GetComponentsInChildren<NetworkTransformBase>(true));
            }
        }

        [Test]
        public void WhenPrefabsLoaded_ThenSomePrefabHasANetworkTransform() =>
            Assert.IsNotEmpty(_networkTransforms);

        [Test]
        public void WhenPrefabsLoaded_ThenEveryNetworkTransformTargetsItsOwnPrefab() {
            for (int i = 0; i < _networkTransforms.Count; i++) {
                NetworkTransformBase networkTransform = _networkTransforms[i];
                Transform root = networkTransform.transform.root;
                Assert.IsNotNull(networkTransform.target, $"{root.name}: {networkTransform.GetType().Name} has no target.");
                Assert.IsTrue(networkTransform.target.root == root, $"{root.name}: {networkTransform.GetType().Name} targets {networkTransform.target.root.name}, another prefab.");
            }
        }
    }
}
