using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Decor;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Noise;
using Features.FlightFieldModule.Scripts.Generation.Obstacles;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Generation.Steps;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Ore;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Structures;
using Features.MatrixModule.Scripts.MatrixFactory;
using Zenject;

namespace Features.FlightFieldModule.Scripts {
    public sealed class FlightFieldModuleInstaller : Installer<FlightFieldModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<FieldStructureCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<FlightFieldConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<FieldDifficultyProfileValidator>().AsSingle();
            Container.BindInterfacesTo<FlightFieldDecorConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<FieldVolumeConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<FieldRockDurabilityConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<FieldOreCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<FieldOreConfigurationValidator>().AsSingle();
            Container.Bind<IMatrixFactory>().To<MatrixFactory>().AsSingle();
            Container.Bind<IFieldRandomFactory>().To<FieldRandomFactory>().AsSingle();
            Container.Bind<IFieldFootprintReader>().To<FieldFootprintReader>().AsSingle();
            Container.Bind<IFieldStructurePlacer>().To<FieldStructurePlacer>().AsSingle();
            Container.Bind<IFieldDebrisPicker>().To<FieldDebrisPicker>().AsSingle();
            Container.Bind<IFieldAreaReserver>().To<FieldAreaReserver>().AsSingle();
            Container.Bind<IFieldNoise>().To<FieldValueNoise>().AsSingle();
            Container.Bind<IFieldRidgeNoise>().To<FieldRidgeNoise>().AsSingle();
            Container.Bind<IFieldDebrisLayout>().To<FieldDebrisLayout>().AsSingle();
            Container.Bind<IFieldObstacleDensity>().To<FieldObstacleDensity>().AsSingle();
            InstallGenerationSteps();
            Container.Bind<IFlightFieldGenerator>().To<FlightFieldGenerator>().AsSingle();
            Container.BindInterfacesTo<FlightFieldGenerationWarmup>().AsSingle();
            Container.BindInterfacesTo<FieldProfileSelector>().AsSingle();
            Container.Bind<IFieldRequestFactory>().To<FieldRequestFactory>().AsSingle();
            Container.Bind<IFieldProfileLookup>().To<FieldProfileLookup>().AsSingle();
            InstallLiveField();
            InstallRocks();
            InstallOre();
            InstallDecor();
        }

        // The rocks of the live field one by one: the server finds, damages and breaks them from the field data, every
        // peer makes its built rocks weapon targets, shows their hits, hides a broken one and plays its break.
        private void InstallRocks() {
            Container.BindInterfacesTo<LiveFieldBrokenRocks>().AsSingle();
            Container.Bind<IFieldRockClassifier>().To<FieldRockClassifier>().AsSingle();
            Container.Bind<IFieldRockReader>().To<FieldRockReader>().AsSingle();
            Container.Bind<IFieldRockFieldSource>().To<ServerFieldRockFieldSource>().AsSingle();
            Container.Bind<IFieldRockQuery>().To<FieldRockQuery>().AsSingle();
            Container.Bind<IPeerFieldRockQuery>().To<PeerFieldRockQuery>().AsSingle();
            Container.BindInterfacesTo<FieldRockBreaker>().AsSingle();
            Container.Bind<IFieldRockTargetIds>().To<FieldRockTargetIds>().AsSingle();
            Container.Bind<IFieldRockHitPoints>().To<FieldRockHitPoints>().AsSingle();
            Container.BindInterfacesTo<FieldRockDamageService>().AsSingle();
            Container.Bind<FieldRockWeaponTarget>().AsSingle();
            Container.BindInterfacesTo<FieldRockWeaponTargetSystem>().AsSingle();
            Container.BindInterfacesTo<FieldRockHitFeedbackSystem>().AsSingle();
            Container.Bind<IFieldRockTargetBinder>().To<FieldRockTargetBinder>().AsSingle();
            Container.Bind<IFieldRockBreakEffectPool>().To<FieldRockBreakEffectPool>().AsSingle();
            Container.BindInterfacesTo<FieldRockBreakViewSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.Bind<IFieldRockBreakDebug>().To<FieldRockBreakDebug>().AsSingle();
            Container.BindInterfacesTo<FieldRockBreakDebugSystem>().AsSingle();
            Container.BindInterfacesTo<FieldRockDestroyedLogSystem>().AsSingle();
#endif
        }

        // The ore of broken rocks in the live field: the server drops it, hooks it on a rope, pulls it in, takes it in
        // at the ship (once per piece, one event) and lets it go at its end of life; every peer places and shows it
        // from the synced list with its own pool and draws the ropes.
        private void InstallOre() {
            Container.BindInterfacesTo<LiveFieldOres>().AsSingle();
            Container.Bind<IFieldOreClock>().To<NetworkFieldOreClock>().AsSingle();
            Container.Bind<IFieldOreMotion>().To<FieldOreMotion>().AsSingle();
            Container.Bind<IFieldOreLifetimeRule>().To<FieldOreLifetimeRule>().AsSingle();
            Container.Bind<IFieldOreQuery>().To<FieldOreQuery>().AsSingle();
            Container.Bind<IFieldOreSpawner>().To<FieldOreSpawner>().AsSingle();
            Container.Bind<IFieldOreDropRule>().To<FieldOreDropRule>().AsSingle();
            Container.Bind<IFieldOreDropService>().To<FieldOreDropService>().AsSingle();
            Container.BindInterfacesTo<FieldOreDropSystem>().AsSingle();
            Container.Bind<IFieldOreHooks>().To<FieldOreHookService>().AsSingle();
            Container.BindInterfacesTo<FieldOreCollector>().AsSingle();
            Container.Bind<IFieldOreRopeService>().To<FieldOreRopeService>().AsSingle();
            Container.Bind<IFieldOreExpiryService>().To<FieldOreExpiryService>().AsSingle();
            Container.BindInterfacesTo<FieldOreServerSystem>().AsSingle();
            Container.Bind<IFieldOreWeaponIds>().To<FieldOreWeaponIds>().AsSingle();
            Container.BindInterfacesTo<FieldOreWeaponTargetSystem>().AsSingle();
            Container.Bind<IFieldOreSceneBuilder>().To<FieldOreSceneBuilder>().AsSingle().WithArguments(new FieldStructurePool());
            Container.BindInterfacesTo<FieldOreBuildSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<FieldOreDropDebugSystem>().AsSingle();
#endif
        }

        // The visual-only decor every peer builds around its ship; its own pool, because the field's pool is emptied
        // whenever the field changes.
        private void InstallDecor() {
            Container.Bind<IFieldVolumeSourceReader>().To<LiveFieldVolumeSourceReader>().AsSingle();
            Container.Bind<IFieldVolumeCandidates>().To<FieldVolumeCandidates>().AsSingle();
            Container.Bind<IFieldDecorChunkLayout>().To<FieldDecorChunkLayout>().AsSingle();
            Container.Bind<IFieldDecorPlanetPicker>().To<FieldDecorPlanetPicker>().AsSingle();
            Container.Bind<IFieldDecorLightSource>().To<SceneSunLightSource>().AsSingle();
            Container.Bind<IFieldDecorSceneBuilder>().To<FieldDecorSceneBuilder>().AsSingle().WithArguments(new FieldStructurePool());
            Container.BindInterfacesTo<FieldDecorBuildSystem>().AsSingle();
        }

        // The field in the world: the server publishes it at launch, every peer builds it from the live field model.
        // The debris has its own pool: the structures' pool is emptied as a whole whenever the field changes.
        private void InstallLiveField() {
            Container.Bind<IFieldStructureSpawnRule>().To<FieldStructureSpawnRule>().AsSingle();
            Container.Bind<IFieldStructureLayout>().To<FieldStructureLayout>().AsSingle();
            Container.Bind<IFieldClusterLayout>().To<FieldClusterLayout>().AsSingle();
            Container.Bind<IFieldPlacementRockLayout>().To<FieldPlacementRockLayout>().AsSingle();
            Container.BindInterfacesTo<FlightFieldPublisher>().AsSingle();
            Container.Bind<IFieldNetworkedStructures>().To<FieldNetworkedStructures>().AsSingle();
            Container.Bind<IFieldNetworkedPrefabRegistration>().To<FieldNetworkedPrefabRegistration>().AsSingle();
            Container.BindInterfacesTo<FlightFieldService>().AsSingle();
            Container.BindInterfacesTo<FlightFieldLaunchSystem>().AsSingle();
            Container.Bind<IFieldPointOfInterestSource>().To<ServerFieldPointOfInterestSource>().AsSingle();
            Container.Bind<IFieldStructurePool>().To<FieldStructurePool>().AsSingle();
            Container.Bind<IFieldStructureSceneBuilder>().To<FieldStructureSceneBuilder>().AsSingle();
            Container.Bind<IFieldDebrisSceneBuilder>().To<FieldDebrisSceneBuilder>().AsSingle().WithArguments(new FieldStructurePool());
            Container.Bind<IFieldDebrisStreamer>().To<FieldDebrisStreamer>().AsSingle();
            Container.Bind<IFieldStreamingRule>().To<FieldStreamingRule>().AsSingle();
            Container.BindInterfacesTo<LiveFieldBuildSystem>().AsSingle();
        }

        // The only place that orders generation: the steps run in the order they are bound here. A new kind of field
        // content is a new step class and one line here.
        private void InstallGenerationSteps() {
            Container.Bind<IFieldGenerationStep>().To<FieldFrameStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldStationZoneStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldTakeoffCorridorStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldObstacleRouteStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldDensityMapStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldPointOfInterestStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldObstacleGiantStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldObstacleDebrisStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldBalanceMetricStep>().AsSingle();
        }
    }
}
