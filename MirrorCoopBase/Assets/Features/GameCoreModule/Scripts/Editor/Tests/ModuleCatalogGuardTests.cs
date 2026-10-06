using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Features.AddressablesConstantsGenerator.Generated;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.GameCoreModule.Editor.Tests {
    // Guards modules as data: every module is one definition in the ModuleCatalog, the item of every module can be
    // spawned and taken off a slot again, and ship and shop code never branches on a concrete module.
    public sealed class ModuleCatalogGuardTests {
        private const string GLOBAL_SCENE_PATH = "Assets/Features/GameCoreModule/GameResources/Scenes/GlobalScene.unity";
        private const string SOURCE_PATTERN = "*.cs";
        private const string TESTS_FOLDER = "/Editor/Tests/";
        // ItemViewId, or a category compared with or switched on a concrete value to pick behaviour (the former
        // "Engine and Radar are removable" and "the Control slot steers" rules). Declaring a slot category, passing it
        // on or comparing two categories stays allowed.
        private const string CONCRETE_MODULE_PATTERN =
            @"\bItemViewId\b"
            + @"|[Tt]ype\s*[!=]=\s*ShipModuleType\."
            + @"|ShipModuleType\.\w+\s*[!=]="
            + @"|switch\s*\([^)]*[Tt]ype\)"
            + @"|case\s+ShipModuleType\.";

        private static readonly string[] _moduleCodeFolders = {
            "Assets/Features/ShipModule/Scripts",
            "Assets/Features/ShopModule/Scripts",
        };

        private ModuleCatalog _moduleCatalog;

        [SetUp]
        public void SetUp() =>
            _moduleCatalog = LoadConfiguration<ModuleCatalog>(Address.Configurations.ModuleCatalog_Default);

        [Test]
        public void WhenModuleCatalogIsRead_ThenEveryIdIsSetAndUnique() {
            string[] ids = _moduleCatalog.Modules.Select(module => module.Id).ToArray();

            Assert.IsFalse(ids.Any(string.IsNullOrEmpty), $"A module in {_moduleCatalog.name} has no id.");
            string[] duplicates = ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
            Assert.IsEmpty(duplicates, $"Module ids used twice, uninstall would return the wrong item: {string.Join(", ", duplicates)}");
        }

        [Test]
        public void WhenModuleCatalogIsRead_ThenEveryModuleHasAnItemThatPointsBackToIt() {
            foreach (ModuleDefinition module in _moduleCatalog.Modules) {
                ShipItem item = module.ItemPrefab;
                Assert.IsNotNull(item, $"{module.name} has no item prefab, it could not be taken off a slot.");
                Assert.IsNotNull(item.GetComponent<Grabbable>(), $"The item of {module.name} has no {nameof(Grabbable)}.");
                Assert.IsNotNull(item.GetComponent<NetworkIdentity>(), $"The item of {module.name} has no {nameof(NetworkIdentity)}.");
                Assert.AreSame(module, item.Module, $"The item {item.name} points to another module than {module.name}.");
                Assert.IsNotNull(module.SlotViewPrefab, $"{module.name} has no slot view prefab.");
            }
        }

        [Test]
        public void WhenNetworkManagerIsRead_ThenEveryModuleItemIsSpawnable() {
            HashSet<GameObject> spawnable = ReadSpawnablePrefabs();

            string[] missing = _moduleCatalog.Modules
                .Where(module => spawnable.Contains(module.ItemPrefab.gameObject) == false)
                .Select(module => module.name)
                .ToArray();

            Assert.IsEmpty(missing, $"Module items not in the Mirror spawnable prefabs, a client could not spawn them: {string.Join(", ", missing)}");
        }

        [Test]
        public void WhenShopCatalogIsRead_ThenEveryModuleIsInTheModuleCatalog() {
            ShopCatalog shopCatalog = LoadConfiguration<ShopCatalog>(Address.Configurations.ShopCatalog_Default);

            AssertAllInModuleCatalog(shopCatalog.Modules, shopCatalog.name);
        }

        [Test]
        public void WhenStationDropsAreRead_ThenEveryModuleIsInTheModuleCatalog() {
            ShipStationCatalog stationCatalog = LoadConfiguration<ShipStationCatalog>(Address.Configurations.ShipStationCatalog_Default);

            AssertAllInModuleCatalog(stationCatalog.Drops.Select(drop => drop.Module).ToArray(), stationCatalog.name);
        }

        [Test]
        public void WhenShipAndShopSourcesAreScanned_ThenNoneChecksAConcreteModule() {
            string[] offenders = _moduleCodeFolders
                .SelectMany(folder => Directory.GetFiles(folder, SOURCE_PATTERN, SearchOption.AllDirectories))
                .Select(ToAssetPath)
                .Where(path => path.Contains(TESTS_FOLDER) == false)
                .Where(path => Regex.IsMatch(File.ReadAllText(path), CONCRETE_MODULE_PATTERN))
                .ToArray();

            Assert.IsEmpty(offenders, $"Module behaviour comes from ModuleDefinition, not from checks of a module type: {string.Join(", ", offenders)}");
        }

        private void AssertAllInModuleCatalog(IReadOnlyList<ModuleDefinition> modules, string source) {
            string[] missing = modules
                .Where(module => module == null || _moduleCatalog.Modules.Contains(module) == false)
                .Select(module => module == null ? "<empty>" : module.name)
                .ToArray();

            Assert.IsEmpty(missing, $"{source} uses modules missing from {_moduleCatalog.name}: {string.Join(", ", missing)}");
        }

        private static HashSet<GameObject> ReadSpawnablePrefabs() {
            Scene scene = SceneManager.GetSceneByPath(GLOBAL_SCENE_PATH);
            bool wasOpen = scene.isLoaded;
            if (wasOpen == false)
                scene = EditorSceneManager.OpenScene(GLOBAL_SCENE_PATH, OpenSceneMode.Additive);

            try {
                NetworkManager networkManager = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<NetworkManager>(true))
                    .FirstOrDefault();
                Assert.IsNotNull(networkManager, $"No {nameof(NetworkManager)} in {GLOBAL_SCENE_PATH}.");
                return new HashSet<GameObject>(networkManager.spawnPrefabs);
            } finally {
                if (wasOpen == false)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T LoadConfiguration<T>(string address) where T : ScriptableObject {
            AddressableAssetGroup group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(Address.Configurations.ADDRESSABLE_GROUP_NAME);
            AddressableAssetEntry entry = group.entries.FirstOrDefault(candidate => candidate.address == address);
            Assert.IsNotNull(entry, $"No entry with address {address} in the {Address.Configurations.ADDRESSABLE_GROUP_NAME} group.");
            return AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath);
        }

        private static string ToAssetPath(string path) =>
            path.Replace('\\', '/');
    }
}
