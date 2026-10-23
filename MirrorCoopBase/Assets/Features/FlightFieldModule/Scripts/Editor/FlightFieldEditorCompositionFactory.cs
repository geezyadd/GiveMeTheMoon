using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Creates the generation for editor tools from the configuration assets at their Addressables addresses and the
    // module installer, so the debug map and the tests run the same steps in the same order as the game.
    public sealed class FlightFieldEditorCompositionFactory {
        public FlightFieldEditorComposition Create() =>
            Create(AddressableConfigurationLoader.Load<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default));

        // A test may pass a changed copy of the configuration; the catalog is always the game's one.
        public FlightFieldEditorComposition Create(FlightFieldConfiguration configuration) {
            FieldStructureCatalog catalog = AddressableConfigurationLoader.Load<FieldStructureCatalog>(Address.Configurations.FieldStructureCatalog_Default);
            DiContainer container = new();
            container.Bind<FlightFieldConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<FieldStructureCatalog>().FromInstance(catalog).AsSingle();
            container.Bind<FieldRockDurabilityConfiguration>().FromInstance(AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default)).AsSingle();
            FlightFieldModuleInstaller.Install(container);
            return new FlightFieldEditorComposition(configuration, catalog, container.Resolve<IFlightFieldGenerator>(), container.Resolve<IFieldRequestFactory>(),
                container.Resolve<IFieldFootprintReader>(), container.Resolve<IFieldDebrisLayout>(), container.Resolve<IFieldRandomFactory>(),
                container.Resolve<IFieldProfileLookup>());
        }
    }
}
