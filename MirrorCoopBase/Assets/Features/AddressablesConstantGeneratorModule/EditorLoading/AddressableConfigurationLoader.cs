using System.IO;
using System.Linq;
using Features.AddressablesConstantsGenerator.Generated;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Features.AddressablesConstantGeneratorModule.EditorLoading {
    // Editor tools and tests read a configuration asset at its address in the Configurations group, as the game does.
    public static class AddressableConfigurationLoader {
        public static T Load<T>(string address) where T : ScriptableObject {
            AddressableAssetGroup group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(Address.Configurations.ADDRESSABLE_GROUP_NAME);
            AddressableAssetEntry entry = group.entries.FirstOrDefault(candidate => candidate.address == address);
            if (entry == null)
                throw new FileNotFoundException($"No entry with address {address} in the {Address.Configurations.ADDRESSABLE_GROUP_NAME} group.");

            return AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath);
        }
    }
}
