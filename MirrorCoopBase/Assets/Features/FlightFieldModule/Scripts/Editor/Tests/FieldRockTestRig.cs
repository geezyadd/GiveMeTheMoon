using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // One server's rock side over a generated field, built from scratch: two rigs share nothing but the assets, so
    // they stand for two independent builds of the same field.
    public sealed class FieldRockTestRig {
        public FlightFieldServerModel ServerModel { get; } = new();
        public RecordingBrokenRocks BrokenRocks { get; } = new();
        public FieldRockReader Reader { get; }
        public FieldRockQuery Query { get; }
        public FieldRockBreaker Breaker { get; }
        public FieldRockDamageModel DamageModel { get; } = new();
        public FieldRockHitPoints HitPoints { get; }
        public FieldRockDamageService DamageService { get; }

        public FieldRockTestRig(FlightFieldEditorComposition composition, FieldRockDurabilityConfiguration durabilityConfiguration, FlightField field) {
            FieldRandomFactory randomFactory = new();
            FieldFootprintReader footprintReader = new();
            FieldPlacementRockLayout placementRockLayout = new(new FieldClusterLayout(), randomFactory);
            FieldDebrisLayout debrisLayout = new(composition.Configuration);
            Reader = new FieldRockReader(composition.Catalog, new FieldStructureSpawnRule(), footprintReader, new FieldStructureLayout(footprintReader), placementRockLayout,
                debrisLayout, new FieldRockClassifier(durabilityConfiguration));
            Query = new FieldRockQuery(new ServerFieldRockFieldSource(ServerModel, composition.Catalog, composition.ProfileLookup), composition.Configuration, Reader, BrokenRocks,
                randomFactory);
            Breaker = new FieldRockBreaker(ServerModel, Query, BrokenRocks, BrokenRocks);
            HitPoints = new FieldRockHitPoints(durabilityConfiguration);
            DamageService = new FieldRockDamageService(Query, Breaker, HitPoints, DamageModel, durabilityConfiguration);
            ServerModel.SetField(field);
        }
    }
}
