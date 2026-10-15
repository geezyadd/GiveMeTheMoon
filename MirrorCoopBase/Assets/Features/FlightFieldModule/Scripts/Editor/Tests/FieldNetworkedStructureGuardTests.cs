using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Structures;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Guards networked field structures: the server spawns them through Mirror, so every networked definition in the
    // project needs a NetworkIdentity prefab, and a client registers the networked prefabs of its catalog. The debug
    // networked beacon stays out of the shipped catalog and out of the network manager's spawn prefabs.
    public sealed class FieldNetworkedStructureGuardTests {
        private const string NETWORK_MANAGER_SCENE = "Assets/Features/GameCoreModule/GameResources/Scenes/GlobalScene.unity";
        private const string DEBUG_FOLDER = "Assets/Features/FlightFieldModule/GameResources/Debug/";
        private const string DEFINITION_FILTER = "t:" + nameof(FieldStructureDefinition);
        private const string STRUCTURES_PROPERTY = "_structures";

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenNetworkedDefinitionsAreRead_ThenTheirPrefabsAreMirrorPrefabs() {
            FieldStructureDefinition[] networked = LoadNetworkedDefinitions();
            Assert.IsNotEmpty(networked, "No networked structure definition in the project: the Mirror path of the field is not covered.");

            foreach (FieldStructureDefinition structure in networked)
                Assert.IsTrue(structure.Prefab.TryGetComponent(out NetworkIdentity _), $"{structure.name}: the prefab of a networked structure has no NetworkIdentity.");
        }

        [Test]
        public void WhenAClientStartsWithACatalogOfNetworkedStructures_ThenItRegistersTheirPrefabs() {
            FieldStructureDefinition[] networked = LoadNetworkedDefinitions();
            FieldStructureCatalog catalog = CreateCatalogOf(networked);
            try {
                new FieldNetworkedPrefabRegistration(catalog, new FieldStructureSpawnRule()).RegisterForClient();

                foreach (FieldStructureDefinition structure in networked)
                    Assert.IsTrue(NetworkClient.prefabs.ContainsKey(structure.Prefab.GetComponent<NetworkIdentity>().assetId), $"{structure.name}: its prefab was not registered with the client.");
            } finally {
                foreach (FieldStructureDefinition structure in networked)
                    NetworkClient.UnregisterPrefab(structure.Prefab);

                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void WhenTheGameCatalogIsRead_ThenItHasNoDebugStructure() {
            string[] debugStructures = _composition.Catalog.Structures.Select(AssetDatabase.GetAssetPath).Where(IsDebugAsset).ToArray();

            Assert.IsEmpty(debugStructures, $"Debug structures are in the game catalog: {string.Join(", ", debugStructures)}");
        }

        [Test]
        public void WhenTheNetworkManagerIsRead_ThenItShipsNoDebugStructure() {
            HashSet<GameObject> shipped = ReadRegisteredSpawnPrefabs();

            string[] debugPrefabs = LoadNetworkedDefinitions().Where(structure => shipped.Contains(structure.Prefab)).Select(AssetDatabase.GetAssetPath).Where(IsDebugAsset).ToArray();
            Assert.IsEmpty(debugPrefabs, $"Debug structures are in the network manager's spawn prefabs, a release build would ship them: {string.Join(", ", debugPrefabs)}");
        }

        private static bool IsDebugAsset(string path) =>
            path.StartsWith(DEBUG_FOLDER);

        private static FieldStructureDefinition[] LoadNetworkedDefinitions() =>
            AssetDatabase.FindAssets(DEFINITION_FILTER).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<FieldStructureDefinition>)
                .Where(structure => structure.IsNetworked).ToArray();

        private static FieldStructureCatalog CreateCatalogOf(FieldStructureDefinition[] structures) {
            FieldStructureCatalog catalog = ScriptableObject.CreateInstance<FieldStructureCatalog>();
            SerializedObject serializedCatalog = new(catalog);
            SerializedProperty list = serializedCatalog.FindProperty(STRUCTURES_PROPERTY);
            list.arraySize = structures.Length;
            for (int i = 0; i < structures.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = structures[i];

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static HashSet<GameObject> ReadRegisteredSpawnPrefabs() {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(NETWORK_MANAGER_SCENE, OpenSceneMode.Additive);
            try {
                NetworkManager manager = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<NetworkManager>(true)).Single(found => found != null);
                return new HashSet<GameObject>(manager.spawnPrefabs);
            } finally {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
