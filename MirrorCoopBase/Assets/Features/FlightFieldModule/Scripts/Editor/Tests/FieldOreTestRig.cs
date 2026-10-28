using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Ore;
using Features.FlightFieldModule.Scripts.Server;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The server's ore services on the shipped ore catalog and configuration, over a recorded ore list, a set clock,
    // a set ship and set weapon anchors, so a test drives drops, ropes and deliveries without a network or a scene.
    public sealed class FieldOreTestRig {
        public FieldOreCatalog Catalog { get; }
        public FieldOreConfiguration Configuration { get; }
        public RecordingFieldOres Ores { get; } = new();
        public FixedOreClock Clock { get; } = new();
        public FixedShipPositionSource Ship { get; } = new();
        public RecordingWeaponHolderAnchors Anchors { get; } = new();
        public FlightFieldServerModel ServerModel { get; } = new();
        public FieldOreMotion Motion { get; }
        public FieldOreLifetimeRule Lifetime { get; }
        public FieldOreQuery Query { get; }
        public FieldOreSpawner Spawner { get; }
        public FieldOreHookService Hooks { get; }
        public FieldOreCollector Collector { get; }
        public FieldOreRopeService Ropes { get; }
        public FieldOreExpiryService Expiry { get; }

        public FieldOreTestRig(FlightField field) {
            Catalog = AddressableConfigurationLoader.Load<FieldOreCatalog>(Address.Configurations.FieldOreCatalog_Default);
            Configuration = AddressableConfigurationLoader.Load<FieldOreConfiguration>(Address.Configurations.FieldOreConfiguration_Default);
            if (field != null)
                ServerModel.SetField(field);
            Motion = new FieldOreMotion(Ores, Ship, Configuration);
            Lifetime = new FieldOreLifetimeRule(Configuration);
            Query = new FieldOreQuery(Ores, Motion, Clock, Catalog);
            Spawner = new FieldOreSpawner(Ores, Ores, Motion, Clock, new FieldOreIdModel());
            Hooks = new FieldOreHookService(Ores, Ores, Motion, Clock, Catalog, Configuration);
            Collector = new FieldOreCollector(ServerModel, Ores, Ores, Query);
            Ropes = new FieldOreRopeService(Ores, Motion, Clock, Hooks, Collector, Anchors);
            Expiry = new FieldOreExpiryService(Ores, Ores, Lifetime, Clock);
        }

        public FieldOreDefinition FirstKind => Catalog.Ores[0];

        public FieldOreId Spawn(Vector3 position, Vector3 velocity) {
            Spawner.TrySpawn(FirstKind, position, velocity, out FieldOreId id);
            return id;
        }

        public FieldOreDescription Describe(FieldOreId id) {
            Query.TryGetOre(id, out FieldOreDescription ore);
            return ore;
        }
    }
}
