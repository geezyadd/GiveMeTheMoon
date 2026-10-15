using System.IO;
using System.Linq;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Creates the generation for editor tools from the configuration assets at their Addressables addresses and the
    // module installer, so the debug map and the tests run the same steps in the same order as the game.
    public sealed class FlightFieldEditorCompositionFactory {
        public FlightFieldEditorComposition Create() =>
            Create(LoadConfiguration<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default));

        // A test may pass a changed copy of the configuration; the catalog is always the game's one.
        public FlightFieldEditorComposition Create(FlightFieldConfiguration configuration) {
            FieldStructureCatalog catalog = LoadConfiguration<FieldStructureCatalog>(Address.Configurations.FieldStructureCatalog_Default);
            DiContainer container = new();
            container.Bind<FlightFieldConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<FieldStructureCatalog>().FromInstance(catalog).AsSingle();
            FlightFieldModuleInstaller.Install(container);
            return new FlightFieldEditorComposition(configuration, catalog, container.Resolve<IFlightFieldGenerator>(), container.Resolve<IFieldRequestFactory>(),
                container.Resolve<IFieldFootprintReader>());
        }

        private static T LoadConfiguration<T>(string address) where T : ScriptableObject {
            AddressableAssetGroup group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(Address.Configurations.ADDRESSABLE_GROUP_NAME);
            AddressableAssetEntry entry = group.entries.FirstOrDefault(candidate => candidate.address == address);
            if (entry == null)
                throw new FileNotFoundException($"No entry with address {address} in the {Address.Configurations.ADDRESSABLE_GROUP_NAME} group.");

            return AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath);
        }
    }
}
