using System.Collections.Generic;
using System.IO;
using System.Linq;
using Features.AddressablesConstantsGenerator.Generated;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Features.GameCoreModule.Editor.Tests {
    // Guards the rule from AGENTS.md: configurations load only through Addressables and ConfigurationInstaller.
    public sealed class ConfigurationLoadingGuardTests {
        // Split so that this file does not match a repository search for the call itself.
        private const string RESOURCES_LOAD_CALL = "Resources" + ".Load";
        private const string RESOURCES_FOLDER_NAME = "Resources";
        private const string FEATURES_FOLDER = "Assets/Features";
        private const string SOURCE_PATTERN = "*.cs";

        // The Resources backend of the project asset loader; configurations never go through it.
        private static readonly string[] _allowedResourcesLoadFiles = {
            "Assets/Features/AssetLoaderModule/Scripts/ResourceAssetLoaderService.cs",
        };

        // Zenject looks up ProjectContext.prefab in a Resources folder; it is not a configuration.
        private static readonly string[] _allowedResourcesFolders = {
            "Assets/Features/GameCoreModule/GameResources/Resources",
        };

        private static IEnumerable<string> ConfigurationAddresses => Address.Configurations.AllAddressablesInGroup;

        [Test]
        public void WhenFeatureSourcesAreScanned_ThenNoneCallsResourcesLoad() {
            string[] offenders = Directory.GetFiles(FEATURES_FOLDER, SOURCE_PATTERN, SearchOption.AllDirectories)
                .Select(ToAssetPath)
                .Where(path => _allowedResourcesLoadFiles.Contains(path) == false)
                .Where(path => File.ReadAllText(path).Contains(RESOURCES_LOAD_CALL))
                .ToArray();

            Assert.IsEmpty(offenders, $"{RESOURCES_LOAD_CALL} is not allowed in features: {string.Join(", ", offenders)}");
        }

        [Test]
        public void WhenFeatureFoldersAreScanned_ThenOnlyAllowedResourcesFoldersExist() {
            string[] offenders = Directory.GetDirectories(FEATURES_FOLDER, RESOURCES_FOLDER_NAME, SearchOption.AllDirectories)
                .Select(ToAssetPath)
                .Where(path => _allowedResourcesFolders.Contains(path) == false)
                .ToArray();

            Assert.IsEmpty(offenders, $"Configurations go to Addressables, not to Resources folders: {string.Join(", ", offenders)}");
        }

        [TestCaseSource(nameof(ConfigurationAddresses))]
        public void WhenConfigurationAddressIsResolved_ThenItsAssetLoads(string address) {
            AddressableAssetEntry entry = FindConfigurationsGroup().entries.FirstOrDefault(candidate => candidate.address == address);

            Assert.IsNotNull(entry, $"No entry with address {address} in the {Address.Configurations.ADDRESSABLE_GROUP_NAME} group.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath), $"Asset of {address} does not load from {entry.AssetPath}.");
        }

        [Test]
        public void WhenConfigurationsGroupIsRead_ThenEveryEntryHasAGeneratedAddress() {
            string[] missing = FindConfigurationsGroup().entries
                .Select(entry => entry.address)
                .Where(address => Address.Configurations.AllAddressablesInGroup.Contains(address) == false)
                .ToArray();

            Assert.IsEmpty(missing, $"Address_g.cs is stale, regenerate it: {string.Join(", ", missing)}");
        }

        private static AddressableAssetGroup FindConfigurationsGroup() {
            AddressableAssetGroup group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(Address.Configurations.ADDRESSABLE_GROUP_NAME);
            Assert.IsNotNull(group, $"Addressables group {Address.Configurations.ADDRESSABLE_GROUP_NAME} is missing.");
            return group;
        }

        private static string ToAssetPath(string path) =>
            path.Replace('\\', '/');
    }
}
