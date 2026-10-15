using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Structures;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Guards networked field structures: the server spawns them through Mirror, so every networked definition in the
    // project needs a NetworkIdentity prefab that the network manager registers for clients; the debug networked
    // beacon stays out of the shipped catalog.
    public sealed class FieldNetworkedStructureGuardTests {
        private const string NETWORK_MANAGER_SCENE = "Assets/Features/GameCoreModule/GameResources/Scenes/GlobalScene.unity";
        private const string DEBUG_FOLDER = "Assets/Features/FlightFieldModule/GameResources/Debug/";
        private const string DEFINITION_FILTER = "t:" + nameof(FieldStructureDefinition);

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenNetworkedDefinitionsAreRead_ThenTheirPrefabsAreRegisteredMirrorPrefabs() {
            FieldStructureDefinition[] networked = LoadAllDefinitions().Where(structure => structure.IsNetworked).ToArray();
            Assert.IsNotEmpty(networked, "No networked structure definition in the project: the Mirror path of the field is not covered.");
            HashSet<GameObject> registered = ReadRegisteredSpawnPrefabs();

            foreach (FieldStructureDefinition structure in networked) {
                Assert.IsTrue(structure.Prefab.TryGetComponent(out NetworkIdentity _), $"{structure.name}: the prefab of a networked structure has no NetworkIdentity.");
                Assert.IsTrue(registered.Contains(structure.Prefab), $"{structure.name}: its prefab is not in the network manager's spawn prefabs, clients could not spawn it.");
            }
        }

        [Test]
        public void WhenTheGameCatalogIsRead_ThenItHasNoDebugStructure() {
            string[] debugStructures = _composition.Catalog.Structures.Select(AssetDatabase.GetAssetPath).Where(path => path.StartsWith(DEBUG_FOLDER)).ToArray();

            Assert.IsEmpty(debugStructures, $"Debug structures are in the game catalog: {string.Join(", ", debugStructures)}");
        }

        private static IEnumerable<FieldStructureDefinition> LoadAllDefinitions() =>
            AssetDatabase.FindAssets(DEFINITION_FILTER).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<FieldStructureDefinition>);

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
